using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Gateways;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;

/// <summary>
/// Concrete implementation of the Economy subsystem.
/// Provides access to specialized gateways for different economic domains.
/// </summary>
[ServiceBinding(typeof(IEconomySubsystem))]
public sealed class EconomySubsystem : IEconomySubsystem
{
    public IBankingGateway Banking { get; }
    public IStorageGateway Storage { get; }
    public IShopGateway Shops { get; }

    public EconomySubsystem(
        IBankingGateway banking,
        IStorageGateway storage,
        IShopGateway shops)
    {
        Banking = banking;
        Storage = storage;
        Shops = shops;
    }
}

