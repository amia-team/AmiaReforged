using System;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

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

        bool isOwned = _snapshot.OwnerCharacterId.HasValue || !string.IsNullOrWhiteSpace(_snapshot.OwnerPersonaId);
        if (!isOwned && !force)
        {
            return PlayerStallDomainResult<Action<PlayerStall>>.Fail(
                PlayerStallError.NotOwned,
                "Stall is not currently owned.");
        }

        bool isOwner = string.Equals(
            _snapshot.OwnerPersonaId,
            requestorPersonaId,
            StringComparison.OrdinalIgnoreCase);

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

    private readonly record struct PlayerStallSnapshot(
        long Id,
        Guid? OwnerCharacterId,
        string? OwnerPersonaId,
        bool IsActive)
    {
        public static PlayerStallSnapshot From(PlayerStall stall)
        {
            return new PlayerStallSnapshot(
                stall.Id,
                stall.OwnerCharacterId,
                stall.OwnerPersonaId,
                stall.IsActive);
        }
    }
}
