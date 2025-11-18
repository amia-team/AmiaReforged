using System.Globalization;
using System.Text.Json;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Application service coordinating player stall domain operations with persistence.
/// </summary>
[ServiceBinding(typeof(IPlayerStallService))]
public sealed class PlayerStallService : IPlayerStallService
{
    private readonly IPlayerShopRepository _shops;

    public PlayerStallService(IPlayerShopRepository shops)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
    }

    public Task<PlayerStallServiceResult> ClaimAsync(ClaimPlayerStallRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(request.StallId);
        if (stall is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(PlayerStallError.StallNotFound, $"Stall {request.StallId} was not found."));
        }

        if (!string.Equals(stall.AreaResRef, request.AreaResRef, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(stall.Tag, request.PlaceableTag, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PlaceableMismatch,
                "Stall record does not match provided placeable context."));
        }

        if (!TryResolvePersonaGuid(request.OwnerPersona, out Guid ownerGuid))
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PersonaNotGuidBacked,
                "Owner persona must resolve to a GUID-backed actor."));
        }

        if (_shops.HasActiveOwnershipInArea(ownerGuid, stall.AreaResRef, stall.Id))
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.OwnershipRuleViolation,
                "Owner already controls a stall within this area."));
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        PlayerStallClaimOptions options = new(
            request.OwnerPersona.ToString(),
            request.OwnerPlayerPersona.ToString(),
            request.OwnerDisplayName,
            request.CoinHouseAccountId,
            request.HoldEarningsInStall,
            request.LeaseStartUtc,
            request.NextRentDueUtc);

        PlayerStallDomainResult<Action<PlayerStall>> domainResult = aggregate.TryClaim(ownerGuid, options);
        if (!domainResult.Success)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(domainResult.Error, domainResult.ErrorMessage ?? "Failed to update stall rent settings."));
        }

        IEnumerable<PlayerStallMember> members = BuildMembers(request);
        bool updated = _shops.UpdateShopWithMembers(request.StallId, domainResult.Payload!, members);
        if (!updated)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PersistenceFailure,
                "Failed to persist stall ownership changes."));
        }

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["stallId"] = request.StallId,
            ["nextRentDueUtc"] = request.NextRentDueUtc
        };

        return Task.FromResult(PlayerStallServiceResult.Ok(data));
    }

    private static IEnumerable<PlayerStallMember> BuildMembers(ClaimPlayerStallRequest request)
    {
        List<PlayerStallMember> members = new()
        {
            new PlayerStallMember
            {
                PersonaId = request.OwnerPersona.ToString(),
                DisplayName = request.OwnerDisplayName,
                CanManageInventory = true,
                CanConfigureSettings = true,
                CanCollectEarnings = true,
                AddedByPersonaId = request.OwnerPersona.ToString()
            }
        };

        if (request.CoOwners is null || request.CoOwners.Count == 0)
        {
            return members;
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase)
        {
            request.OwnerPersona.ToString()
        };

        foreach (PlayerStallCoOwnerRequest coOwner in request.CoOwners)
        {
            string personaId = coOwner.Persona.ToString();
            if (!seen.Add(personaId))
            {
                continue;
            }

            members.Add(new PlayerStallMember
            {
                PersonaId = personaId,
                DisplayName = string.IsNullOrWhiteSpace(coOwner.DisplayName) ? personaId : coOwner.DisplayName,
                CanManageInventory = coOwner.CanManageInventory,
                CanConfigureSettings = coOwner.CanConfigureSettings,
                CanCollectEarnings = coOwner.CanCollectEarnings,
                AddedByPersonaId = request.OwnerPersona.ToString()
            });
        }

        return members;
    }

    public Task<PlayerStallServiceResult> ReleaseAsync(ReleasePlayerStallRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(request.StallId);
        if (stall is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(PlayerStallError.StallNotFound, $"Stall {request.StallId} was not found."));
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        DateTime releasedUtc = (request.ReleasedUtc ?? DateTime.UtcNow).ToUniversalTime();
        string personaId = request.Requestor.ToString();

        PlayerStallDomainResult<Action<PlayerStall>> domainResult = aggregate.TryRelease(personaId, request.Force, releasedUtc);
        if (!domainResult.Success)
        {
            PlayerStallError error = domainResult.Error;
            string message = domainResult.ErrorMessage ?? "Failed to update stall rent settings.";

            if (error == PlayerStallError.Unauthorized &&
                (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId) || !string.IsNullOrWhiteSpace(stall.OwnerPlayerPersonaId)))
            {
                bool isOwner = MatchesOwnerPersona(stall, personaId);
                bool isActiveMember = stall.Members?.Any(member =>
                    member is not null &&
                    !member.RevokedUtc.HasValue &&
                    string.Equals(member.PersonaId, personaId, StringComparison.OrdinalIgnoreCase)) == true;

                if (!isOwner && !isActiveMember)
                {
                    error = PlayerStallError.NotOwner;
                    message = "Only the stall owner may change rent settings.";
                }
            }

            return Task.FromResult(PlayerStallServiceResult.Fail(error, message));
        }

        bool updated = _shops.UpdateShop(request.StallId, domainResult.Payload!);
        if (!updated)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PersistenceFailure,
                "Failed to release stall ownership."));
        }

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["stallId"] = request.StallId,
            ["releasedUtc"] = releasedUtc
        };

        return Task.FromResult(PlayerStallServiceResult.Ok(data));
    }

    public Task<PlayerStallServiceResult> ListProductAsync(ListStallProductRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(request.StallId);
        if (stall is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(PlayerStallError.StallNotFound, $"Stall {request.StallId} was not found."));
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        PlayerStallProductDescriptor descriptor = new(
            request.StallId,
            request.ResRef,
            request.Name,
            request.Description,
            request.Price,
            request.Quantity,
            request.BaseItemType,
            request.ItemData,
            request.ConsignorPersona?.ToString(),
            string.IsNullOrWhiteSpace(request.ConsignorDisplayName) ? null : request.ConsignorDisplayName,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes,
            request.SortOrder,
            request.IsActive,
            request.ListedUtc,
            request.UpdatedUtc);

        PlayerStallDomainResult<StallProduct> domainResult = aggregate.CreateProduct(descriptor);
        if (!domainResult.Success)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(domainResult.Error, domainResult.ErrorMessage!));
        }

        StallProduct product = domainResult.Payload!;
        _shops.AddProductToShop(request.StallId, product);

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["stallId"] = request.StallId,
            ["productId"] = product.Id
        };

        return Task.FromResult(PlayerStallServiceResult.Ok(data));
    }

    public Task<PlayerStallServiceResult> UpdateProductPriceAsync(UpdateStallProductPriceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopWithMembers(request.StallId);
        if (stall is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.StallNotFound,
                $"Stall {request.StallId} was not found."));
        }

        StallProduct? product = _shops.GetProductById(request.StallId, request.ProductId);
        if (product is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.ProductNotFound,
                $"Product {request.ProductId} was not found on stall {request.StallId}."));
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        string personaId = request.Requestor.ToString();

        PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>> domainResult = aggregate.TryUpdateProductPrice(
            personaId,
            product,
            request.NewPrice);

        if (!domainResult.Success)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(domainResult.Error, domainResult.ErrorMessage!));
        }

        bool updated = _shops.UpdateStallAndProduct(request.StallId, product.Id, domainResult.Payload!);
        if (!updated)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PersistenceFailure,
                "Failed to update stall product price."));
        }

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["stallId"] = request.StallId,
            ["productId"] = product.Id,
            ["price"] = Math.Max(0, request.NewPrice)
        };

        return Task.FromResult(PlayerStallServiceResult.Ok(data));
    }

    public Task<PlayerStallServiceResult> UpdateRentSettingsAsync(UpdateStallRentSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(request.StallId);
        if (stall is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.StallNotFound,
                $"Stall {request.StallId} was not found."));
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        string personaId = request.Requestor.ToString();

        PlayerStallDomainResult<Action<PlayerStall>> domainResult = aggregate.TryConfigureRentSettings(
            personaId,
            request.CoinHouseAccountId,
            request.HoldEarningsInStall);

        if (!domainResult.Success)
        {
            PlayerStallError error = domainResult.Error;
            string message = domainResult.ErrorMessage ?? "Failed to update stall rent settings.";

            if (error == PlayerStallError.Unauthorized &&
                (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId) || !string.IsNullOrWhiteSpace(stall.OwnerPlayerPersonaId)))
            {
                bool isOwner = MatchesOwnerPersona(stall, personaId);
                bool isActiveMember = stall.Members?.Any(member =>
                    member is not null &&
                    !member.RevokedUtc.HasValue &&
                    string.Equals(member.PersonaId, personaId, StringComparison.OrdinalIgnoreCase)) == true;

                if (!isOwner && !isActiveMember)
                {
                    error = PlayerStallError.NotOwner;
                    message = "Only the stall owner may change rent settings.";
                }
            }

            return Task.FromResult(PlayerStallServiceResult.Fail(error, message));
        }

        bool updated = _shops.UpdateShop(request.StallId, domainResult.Payload!);
        if (!updated)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PersistenceFailure,
                "Failed to persist stall rent configuration."));
        }

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["stallId"] = request.StallId,
            ["rentFromCoinhouse"] = request.CoinHouseAccountId.HasValue
        };

        return Task.FromResult(PlayerStallServiceResult.Ok(data));
    }

    public Task<PlayerStallServiceResult> WithdrawEarningsAsync(WithdrawStallEarningsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(request.StallId);
        if (stall is null)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.StallNotFound,
                $"Stall {request.StallId} was not found."));
        }

        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        string personaId = request.Requestor.ToString();

        PlayerStallDomainResult<PlayerStallWithdrawal> domainResult = aggregate.TryWithdrawEarnings(
            personaId,
            request.RequestedAmount);

        if (!domainResult.Success)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(domainResult.Error, domainResult.ErrorMessage!));
        }

        PlayerStallWithdrawal withdrawal = domainResult.Payload!;

        bool updated = _shops.UpdateShop(request.StallId, withdrawal.Apply);
        if (!updated)
        {
            return Task.FromResult(PlayerStallServiceResult.Fail(
                PlayerStallError.PersistenceFailure,
                "Failed to persist stall earnings withdrawal."));
        }

        RecordWithdrawalLedgerEntry(stall, request, withdrawal);

        IReadOnlyDictionary<string, object> data = new Dictionary<string, object>
        {
            ["stallId"] = request.StallId,
            ["amount"] = withdrawal.Amount,
            ["partial"] = withdrawal.WasPartial
        };

        return Task.FromResult(PlayerStallServiceResult.Ok(data));
    }

    private static bool TryResolvePersonaGuid(PersonaId persona, out Guid guid)
    {
        try
        {
            guid = PersonaId.ToGuid(persona);
            return true;
        }
        catch (Exception) when (persona.Type != PersonaType.Character)
        {
            guid = Guid.Empty;
            return false;
        }
        catch (FormatException)
        {
            guid = Guid.Empty;
            return false;
        }
        catch (InvalidOperationException)
        {
            guid = Guid.Empty;
            return false;
        }
        catch (ArgumentException)
        {
            guid = Guid.Empty;
            return false;
        }
    }

    private static bool MatchesOwnerPersona(PlayerStall stall, string personaId)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId) &&
            string.Equals(stall.OwnerPersonaId, personaId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(stall.OwnerPlayerPersonaId) &&
            string.Equals(stall.OwnerPlayerPersonaId, personaId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private void RecordWithdrawalLedgerEntry(PlayerStall stall, WithdrawStallEarningsRequest request, PlayerStallWithdrawal withdrawal)
    {
        PlayerStallLedgerEntry entry = new()
        {
            StallId = request.StallId,
            OwnerCharacterId = stall.OwnerCharacterId,
            OwnerPersonaId = stall.OwnerPersonaId,
            EntryType = PlayerStallLedgerEntryType.Withdrawal,
            Amount = -Math.Max(0, withdrawal.Amount),
            Currency = "gp",
            Description = BuildWithdrawalDescription(withdrawal.Amount, request.Requestor, withdrawal.WasPartial),
            OccurredUtc = DateTime.UtcNow,
            MetadataJson = BuildWithdrawalMetadata(request, withdrawal)
        };

        _shops.AddLedgerEntry(entry);
    }

    private static string BuildWithdrawalDescription(int amount, PersonaId persona, bool wasPartial)
    {
        string personaLabel = persona.ToString();
        string basis = wasPartial ? "Partial withdrawal" : "Withdrawal";

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} of {1:n0} gp by {2}",
            basis,
            Math.Max(0, amount),
            string.IsNullOrWhiteSpace(personaLabel) ? "unknown persona" : personaLabel);
    }

    private static string BuildWithdrawalMetadata(WithdrawStallEarningsRequest request, PlayerStallWithdrawal withdrawal)
    {
        var metadata = new
        {
            persona = request.Requestor.ToString(),
            withdrawnAmount = Math.Max(0, withdrawal.Amount),
            requestedAmount = request.RequestedAmount,
            partial = withdrawal.WasPartial
        };

        return JsonSerializer.Serialize(metadata);
    }
}
