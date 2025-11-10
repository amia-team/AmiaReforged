using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to economy-related operations through specialized gateways.
/// Each gateway focuses on a specific economic domain (banking, storage, shops, etc.).
/// </summary>
public interface IEconomySubsystem
{
    /// <summary>
    /// Gets the banking gateway for coinhouse and gold transaction operations.
    /// </summary>
    IBankingGateway Banking { get; }

    /// <summary>
    /// Gets the storage gateway for item storage and capacity management.
    /// </summary>
    IStorageGateway Storage { get; }

    /// <summary>
    /// Gets the shop gateway for merchant and player stall operations.
    /// </summary>
    IShopGateway Shops { get; }
}

