using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;

/// <summary>
/// Concrete implementation of the Economy subsystem.
/// Provides access to specialized gateways for different economic domains.
/// </summary>
[ServiceBinding(typeof(IEconomySubsystem))]
public sealed class EconomySubsystem : IEconomySubsystem
{
    public IBankingFacade Banking { get; }
    public IStorageFacade Storage { get; }
    public IShopFacade Shops { get; }

    public EconomySubsystem(
        IBankingFacade banking,
        IStorageFacade storage,
        IShopFacade shops)
    {
        Banking = banking;
        Storage = storage;
        Shops = shops;
    }
}
