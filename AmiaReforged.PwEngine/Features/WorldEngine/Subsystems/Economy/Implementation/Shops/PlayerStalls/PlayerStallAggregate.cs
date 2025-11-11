using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Domain aggregate responsible for enforcing player stall invariants when mutating state.
/// </summary>
public sealed class PlayerStallAggregate
{
    private readonly PlayerStallSnapshot _snapshot;

    private PlayerStallAggregate(PlayerStallSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public static PlayerStallAggregate FromEntity(PlayerStall stall)
    {
        ArgumentNullException.ThrowIfNull(stall);
        return new PlayerStallAggregate(PlayerStallSnapshot.From(stall));
    }

    public PlayerStallDomainResult<Action<PlayerStall>> TryClaim(Guid ownerCharacterId, PlayerStallClaimOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_snapshot.OwnerCharacterId.HasValue && _snapshot.OwnerCharacterId.Value != ownerCharacterId)
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.AlreadyOwned,
                "Stall is already claimed by another persona.");
        }

        Action<PlayerStall> mutation = stall =>
        {
            stall.OwnerCharacterId = ownerCharacterId;
            stall.OwnerPersonaId = options.OwnerPersonaId;
            stall.OwnerPlayerPersonaId = options.OwnerPlayerPersonaId;
            stall.OwnerDisplayName = options.OwnerDisplayName;
            stall.CoinHouseAccountId = options.CoinHouseAccountId;
            stall.HoldEarningsInStall = options.HoldEarningsInStall;
            stall.LeaseStartUtc = options.LeaseStartUtc;
            stall.NextRentDueUtc = options.NextRentDueUtc;
            stall.LastRentPaidUtc ??= options.LeaseStartUtc;
            stall.SuspendedUtc = null;
            stall.DeactivatedUtc = null;
            stall.IsActive = true;
        };

        return PlayerStallDomainResult<Action<PlayerStall>>.Ok(mutation);
    }

    public PlayerStallDomainResult<Action<PlayerStall>> TryRelease(string requestorPersonaId, bool force, DateTime releasedUtc)
    {
        if (string.IsNullOrWhiteSpace(requestorPersonaId) && !force)
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.NotOwner,
                "Requestor persona is required to release a stall.");
        }

    bool isOwned = _snapshot.OwnerCharacterId.HasValue ||
               !string.IsNullOrWhiteSpace(_snapshot.OwnerPersonaId) ||
               !string.IsNullOrWhiteSpace(_snapshot.OwnerPlayerPersonaId);
        if (!isOwned && !force)
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.NotOwned,
                "Stall is not currently owned.");
        }

        bool isOwner = (!string.IsNullOrWhiteSpace(_snapshot.OwnerPersonaId) &&
                         string.Equals(
                             _snapshot.OwnerPersonaId,
                             requestorPersonaId,
                             StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(_snapshot.OwnerPlayerPersonaId) &&
                         string.Equals(
                             _snapshot.OwnerPlayerPersonaId,
                             requestorPersonaId,
                             StringComparison.OrdinalIgnoreCase));

        if (!isOwner && !force)
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.NotOwner,
                "Only the owner may release this stall.");
        }

        Action<PlayerStall> mutation = stall =>
        {
            stall.OwnerCharacterId = null;
            stall.OwnerPersonaId = null;
            stall.OwnerPlayerPersonaId = null;
            stall.OwnerDisplayName = null;
            stall.CoinHouseAccountId = null;
            stall.HoldEarningsInStall = false;
            stall.SuspendedUtc = releasedUtc;
            stall.DeactivatedUtc = releasedUtc;
            stall.IsActive = false;
        };

        return PlayerStallDomainResult<Action<PlayerStall>>.Ok(mutation);
    }

    public PlayerStallDomainResult<StallProduct> CreateProduct(PlayerStallProductDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (descriptor.StallId != _snapshot.Id)
        {
            return PlayerStallDomainResult<StallProduct>.Fail(
                PlayerStallError.DescriptorMismatch,
                "Product descriptor does not match stall identifier.");
        }

        if (!_snapshot.IsActive)
        {
            return PlayerStallDomainResult<StallProduct>.Fail(
                PlayerStallError.StallInactive,
                "Cannot list products on an inactive stall.");
        }

        StallProduct product = new()
        {
            StallId = descriptor.StallId,
            ResRef = descriptor.ResRef,
            Name = descriptor.Name,
            OriginalName = descriptor.OriginalName,
            Description = descriptor.Description,
            Price = descriptor.Price,
            Quantity = descriptor.Quantity,
            BaseItemType = descriptor.BaseItemType,
            ItemData = (byte[])descriptor.ItemData.Clone(),
            ConsignedByPersonaId = descriptor.ConsignorPersonaId,
            ConsignedByDisplayName = descriptor.ConsignorDisplayName,
            Notes = descriptor.Notes,
            SortOrder = descriptor.SortOrder,
            IsActive = descriptor.IsActive,
            ListedUtc = descriptor.ListedUtc,
            UpdatedUtc = descriptor.UpdatedUtc
        };

        return PlayerStallDomainResult<StallProduct>.Ok(product);
    }

    public PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>> TryUpdateProductPrice(
        string requestorPersonaId,
        StallProduct product,
        int newPrice)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(requestorPersonaId))
        {
            return PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>>.Fail(
                PlayerStallError.Unauthorized,
                "A persona is required to update stall inventory.");
        }

        if (product.StallId != _snapshot.Id)
        {
            return PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>>.Fail(
                PlayerStallError.ProductNotFound,
                "That product is not registered to this stall.");
        }

        if (!HasInventoryPrivileges(requestorPersonaId))
        {
            return PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>>.Fail(
                PlayerStallError.Unauthorized,
                "You do not have permission to manage this stall's inventory.");
        }

        if (newPrice < 0)
        {
            return PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>>.Fail(
                PlayerStallError.PriceOutOfRange,
                "Price must be zero or greater.");
        }

        long productId = product.Id;
        int sanitizedPrice = newPrice;

        return PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>>.Ok((stall, persistedProduct) =>
        {
            if (persistedProduct.Id != productId)
            {
                return false;
            }

            if (persistedProduct.StallId != _snapshot.Id)
            {
                return false;
            }

            persistedProduct.Price = sanitizedPrice;
            return true;
        });
    }

    public PlayerStallDomainResult<bool> TryReclaimProduct(string requestorPersonaId, StallProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(requestorPersonaId))
        {
            return PlayerStallDomainResult<bool>.Fail(
                PlayerStallError.Unauthorized,
                "A persona is required to manage stall inventory.");
        }

        if (!_snapshot.IsActive)
        {
            return PlayerStallDomainResult<bool>.Fail(
                PlayerStallError.StallInactive,
                "This stall is not currently active.");
        }

        if (product.StallId != _snapshot.Id)
        {
            return PlayerStallDomainResult<bool>.Fail(
                PlayerStallError.ProductNotFound,
                "That listing is not registered to this stall.");
        }

        if (!HasInventoryPrivileges(requestorPersonaId))
        {
            return PlayerStallDomainResult<bool>.Fail(
                PlayerStallError.Unauthorized,
                "You do not have permission to manage this stall's inventory.");
        }

        return PlayerStallDomainResult<bool>.Ok(true);
    }

    public PlayerStallDomainResult<Action<PlayerStall>> TryConfigureRentSettings(
        string requestorPersonaId,
        Guid? coinHouseAccountId,
        bool holdEarningsInStall)
    {
        if (string.IsNullOrWhiteSpace(requestorPersonaId))
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.Unauthorized,
                "A persona is required to update stall rent settings.");
        }

        if (!HasSettingsPrivileges(requestorPersonaId))
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.Unauthorized,
                "You do not have permission to change how this stall handles rent.");
        }

        return PlayerStallDomainResult<Action<PlayerStall>>.Ok(stall =>
        {
            stall.CoinHouseAccountId = coinHouseAccountId;
            stall.HoldEarningsInStall = holdEarningsInStall;
        });
    }

    public PlayerStallDomainResult<PlayerStallWithdrawal> TryWithdrawEarnings(
        string requestorPersonaId,
        int? requestedAmount)
    {
        if (string.IsNullOrWhiteSpace(requestorPersonaId))
        {
            return PlayerStallDomainResult<PlayerStallWithdrawal>.Fail(
                PlayerStallError.Unauthorized,
                "A persona is required to withdraw stall earnings.");
        }

        if (!_snapshot.IsActive)
        {
            return PlayerStallDomainResult<PlayerStallWithdrawal>.Fail(
                PlayerStallError.StallInactive,
                "This stall is not currently active.");
        }

        if (!HasCollectionPrivileges(requestorPersonaId))
        {
            return PlayerStallDomainResult<PlayerStallWithdrawal>.Fail(
                PlayerStallError.Unauthorized,
                "You do not have permission to withdraw earnings from this stall.");
        }

        int available = Math.Max(0, _snapshot.EscrowBalance);
        if (available <= 0)
        {
            return PlayerStallDomainResult<PlayerStallWithdrawal>.Fail(
                PlayerStallError.InsufficientEscrow,
                "There are no earnings available to withdraw.");
        }

        int amount;
        bool requestedPartial = false;

        if (requestedAmount is null)
        {
            amount = available;
        }
        else
        {
            if (requestedAmount.Value <= 0)
            {
                return PlayerStallDomainResult<PlayerStallWithdrawal>.Fail(
                    PlayerStallError.InvalidWithdrawalAmount,
                    "Withdrawal amount must be greater than zero.");
            }

            amount = Math.Min(requestedAmount.Value, available);
            requestedPartial = amount < requestedAmount.Value;
        }

        if (amount <= 0)
        {
            return PlayerStallDomainResult<PlayerStallWithdrawal>.Fail(
                PlayerStallError.InvalidWithdrawalAmount,
                "Withdrawal amount must be greater than zero.");
        }

        int sanitizedAmount = amount;
        bool wasPartial = requestedAmount.HasValue && requestedPartial;

        return PlayerStallDomainResult<PlayerStallWithdrawal>.Ok(new PlayerStallWithdrawal(stall =>
        {
            stall.EscrowBalance = Math.Max(0, stall.EscrowBalance - sanitizedAmount);
        }, sanitizedAmount, wasPartial));
    }

    private bool HasInventoryPrivileges(string personaId)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            return false;
        }

        if (IsOwner(personaId))
        {
            return true;
        }

        return _snapshot.InventoryPermissions.TryGetValue(personaId, out bool canManage) && canManage;
    }

    private bool HasCollectionPrivileges(string personaId)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            return false;
        }

        if (IsOwner(personaId))
        {
            return true;
        }

        return _snapshot.CollectionPermissions.TryGetValue(personaId, out bool canCollect) && canCollect;
    }

    private bool HasSettingsPrivileges(string personaId)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            return false;
        }

        if (IsOwner(personaId))
        {
            return true;
        }

        return _snapshot.SettingsPermissions.TryGetValue(personaId, out bool canConfigure) && canConfigure;
    }

    private bool IsOwner(string personaId)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_snapshot.OwnerPersonaId) &&
            string.Equals(_snapshot.OwnerPersonaId, personaId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(_snapshot.OwnerPlayerPersonaId) &&
            string.Equals(_snapshot.OwnerPlayerPersonaId, personaId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private readonly record struct PlayerStallSnapshot(
        long Id,
        Guid? OwnerCharacterId,
    string? OwnerPersonaId,
    string? OwnerPlayerPersonaId,
        bool IsActive,
        int EscrowBalance,
        bool HoldEarningsInStall,
        IReadOnlyDictionary<string, bool> InventoryPermissions,
        IReadOnlyDictionary<string, bool> CollectionPermissions,
        IReadOnlyDictionary<string, bool> SettingsPermissions)
    {
        public static PlayerStallSnapshot From(PlayerStall stall)
        {
            (IReadOnlyDictionary<string, bool> inventory,
                IReadOnlyDictionary<string, bool> collection,
                IReadOnlyDictionary<string, bool> settings) = BuildMemberPermissions(stall);

            return new PlayerStallSnapshot(
                stall.Id,
                stall.OwnerCharacterId,
                stall.OwnerPersonaId,
                stall.OwnerPlayerPersonaId,
                stall.IsActive,
                Math.Max(0, stall.EscrowBalance),
                stall.HoldEarningsInStall,
                inventory,
                collection,
                settings);
        }

        private static (IReadOnlyDictionary<string, bool> Inventory,
            IReadOnlyDictionary<string, bool> Collection,
            IReadOnlyDictionary<string, bool> Settings) BuildMemberPermissions(PlayerStall stall)
        {
            Dictionary<string, bool> inventory = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, bool> collection = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, bool> settings = new(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(stall.OwnerPersonaId))
            {
                string owner = stall.OwnerPersonaId;
                inventory[owner] = true;
                collection[owner] = true;
                settings[owner] = true;
            }

            if (!string.IsNullOrWhiteSpace(stall.OwnerPlayerPersonaId))
            {
                string ownerPlayer = stall.OwnerPlayerPersonaId;
                inventory[ownerPlayer] = true;
                collection[ownerPlayer] = true;
                settings[ownerPlayer] = true;
            }

            if (stall.Members is not null && stall.Members.Count > 0)
            {
                foreach (PlayerStallMember member in stall.Members.Where(m => m is not null))
                {
                    if (member.RevokedUtc.HasValue)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(member.PersonaId))
                    {
                        continue;
                    }

                    string persona = member.PersonaId;

                    if (member.CanManageInventory)
                    {
                        inventory[persona] = true;
                    }

                    if (member.CanCollectEarnings)
                    {
                        collection[persona] = true;
                    }

                    if (member.CanConfigureSettings)
                    {
                        settings[persona] = true;
                    }
                }
            }

            return (inventory, collection, settings);
        }
    }
}

public sealed record PlayerStallWithdrawal(Action<PlayerStall> Apply, int Amount, bool WasPartial);
