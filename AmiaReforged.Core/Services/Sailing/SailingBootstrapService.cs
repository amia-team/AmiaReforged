using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(SailingBootstrapService))]
public class SailingBootstrapService : IDisposable
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly HelmService _helmService;
    private readonly MaritimeLedgerService _ledger;
    private readonly ShipRentalService _rentals;
    private readonly ShipRegistryService _registry;
    private readonly IslandService _islands;
    private readonly ShipCombatNuiService _combat;
    private readonly MaritimeOptions _options;
    private bool _disposed;
    private readonly TaskCompletionSource _initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private NwModule? _module;
    public Task Initialization => _initialization.Task;

    private readonly PhysicalShipService _physicalShipService;
private readonly MerchantTradeRouteService
    _merchantTradeRouteService;
    // ---------------------------------------------------------------------
    // Starting Fleet
    // ---------------------------------------------------------------------

    private static readonly ShipDefinition[] StartingFleet =
    {
 new(
    "Sea Sprite",
    "sloop",
    "sailing_helm",
    "sea_sprite",
    "sea_sprite_exit",
    "sea_sprite_d2",
    "sea_sprite_c",
    "ocean_01",
    76f,
    79f,
    0f,
    Heading.East,
    ShipType.Player,
    100) { InitialStoredPortId = "southport" },
new(
    "Black Pearl",
    "galleon",
    "sailing_helm_black_pearl",
    "black_pearl",
    "black_pearl_exit",
    "black_pearl_d2",
    "black_pearl_c",
    "ocean_01",
    10f,
    10f,
    0f,
    Heading.East,
    ShipType.Player,
    140),

new(
    "Stormrunner",
    "brig",
    "sailing_helm_stormrunner",
    "stormrunner",
    "stormrunner_exit",
    "stormrunner_d2",
    "stormrunner_c",
    "ocean_01",
    70f,
    40f,
    0f,
    Heading.East,
    ShipType.Player,
    90),

    };

    // ---------------------------------------------------------------------
    // Constructor
    // ---------------------------------------------------------------------

public SailingBootstrapService(
    HelmService helmService,
    PhysicalShipService physicalShipService,
    MerchantTradeRouteService merchantTradeRouteService,
    ShipCombatNuiService combat, MaritimeLedgerService ledger, ShipRentalService rentals, ShipRegistryService registry, IslandService islands, MaritimeOptions options)
{
    _helmService = helmService; _ledger = ledger; _rentals = rentals; _registry = registry; _islands = islands;
    _combat = combat; _options = options;
    _physicalShipService = physicalShipService;
    _merchantTradeRouteService = merchantTradeRouteService;

}

internal void InitializeModule(NwModule module)
{
    if (_disposed || _module != null) return;
    module.OnClientEnter += HandleClientEnter;
    _module = module;

    try { RegisterFleet(); }
    catch (Exception ex)
    {
        // Release reconnect/restoration waiters if native fleet setup fails
        // before the asynchronous initialization task can be started.
        _initialization.TrySetException(ex);
        throw;
    }
}

private void RegisterFleet()
{

    // ---------------------------------------------------------------------
    // Starting fleet
    // ---------------------------------------------------------------------

    foreach (ShipDefinition ship in StartingFleet)
    {
        _helmService.CreateShip(ship);
        _physicalShipService.RegisterPhysicalShip(ship);
    }

    // ---------------------------------------------------------------------
    // Merchant ships
    // ---------------------------------------------------------------------

    ShipDefinition goldenGullDefinition =
        new(
            "Golden Gull",
            "cog",
            "sailing_helm_golden_gull",
            "golden_gull",
            "golden_gull_exit",
            "golden_gull_d2",
            "golden_gull_c",
            "ocean_01",
            120f,
            90f,
            0f,
            Heading.West,
            ShipType.Merchant,
            140);
goldenGullDefinition.CargoCapacity = 120;
    ShipState goldenGull =
        _helmService.CreateShip(
            goldenGullDefinition);
goldenGull.MerchantGold = 1000;
goldenGull.Cargo.Add(
    new MerchantCargo
    {
        ItemId = "timber",
        Quantity = 50
    });            

        _merchantTradeRouteService
        .AssignDriftwoodSouthportRoute(
            goldenGull);

    _physicalShipService.RegisterPhysicalShip(
        goldenGullDefinition);

    CutterRentalFleet.ValidateIsolation(_options.RentalVessels, StartingFleet.Append(goldenGullDefinition));
    foreach (var slot in _options.RentalVessels)
    {
        if (!slot.IsConfigured)
        {
            Log.Warn("Rental cutter {0} unavailable: configure its distinct deck ResRef, helm, exit and ocean representation tags. No module resources were fabricated.", slot.ShipId);
            continue;
        }
        IslandLocation port = _islands.GetIslands().Single(i => i.Id == slot.PortId);
        ShipDefinition definition = CutterRentalFleet.CreateDefinition(slot, port);
        _helmService.CreateShip(definition);
        _physicalShipService.RegisterPhysicalShip(definition);
    }

    // ---------------------------------------------------------------------
    // Physical ship interactions
    // ---------------------------------------------------------------------

    _physicalShipService.RegisterPhysicalShipInteractions();
    _ = CompleteInitialization();
    _ = RefreshWorld();

    foreach (ShipDefinition ship in StartingFleet)
    {
        _physicalShipService.SpawnPhysicalShip(
            ship.ShipName);
    }

    //Log.Info(
        //$"Sailing system initialized. " +
        //$"Registered {StartingFleet.Length} starting ship(s) " +
        //$"and 1 merchant ship.");
}

    private async Task CompleteInitialization()
    {
        try
        {
            await _helmService.InitializeFleet();
            await _ledger.InitializeAsync(_registry, _islands);
            await NwTask.SwitchToMainThread();
            await _rentals.EnrollAsync();
            await NwTask.SwitchToMainThread();
            _initialization.TrySetResult();
        }
        catch (Exception ex) { _initialization.TrySetException(ex); }
    }
    private void HandleClientEnter(ModuleEvents.OnClientEnter evt) => _ = RecoverPlayer(evt.Player);

    private async Task RefreshWorld()
    {
        try
        {
            await Initialization;
            await NwTask.SwitchToMainThread();
            if (_disposed) return;
            _helmService.SynchronizeWorld();
            _rentals.LogResourceReadiness();
            _combat.RegisterInteractions();
        }
        catch (Exception ex) { Log.Error(ex, "Sailing world initialization failed; saved state was not replaced with defaults."); }
    }

    private async Task RecoverPlayer(NwPlayer player)
    {
        try
        {
            await Initialization;
            await NwTask.NextFrame();
            if (!_disposed && player.IsValid) await _helmService.RecoverPlayerContextAsync(player);
        }
        catch (Exception ex) { Log.Error(ex, "Sailing reconnect recovery failed."); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_module != null) _module.OnClientEnter -= HandleClientEnter;
        _module = null;
        _initialization.TrySetCanceled();
    }
}

