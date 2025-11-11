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
    /// <inheritdoc />
    public IBankingFacade Banking { get; }

    /// <inheritdoc />
    public IStorageFacade Storage { get; }

    /// <inheritdoc />
    public IShopFacade Shops { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EconomySubsystem"/> class.
    /// </summary>
    /// <param name="banking">The banking facade for coinhouse operations.</param>
    /// <param name="storage">The storage facade for item storage operations.</param>
    /// <param name="shops">The shop facade for marketplace operations.</param>
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

