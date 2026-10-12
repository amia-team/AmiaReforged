using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Arms the load callback after Anvil injection, then starts native sailing work on module load.</summary>
[ServiceBinding(typeof(SailingModuleLifecycleService))]
public sealed class SailingModuleLifecycleService : IInitializable, IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly EventService _events;
    private readonly SailingFileStore _files;
    private readonly MaritimeLedgerService _ledger;
    private readonly PhysicalShipService _physical;
    private readonly HelmService _helm;
    private readonly ShipRetrievalNuiService _retrieval;
    private readonly ShipTradeNuiService _trade;
    private readonly ShipSpellService _spells;
    private readonly ShipNativeSpellDispatcher _dispatcher;
    private readonly ShipCombatNuiService _combat;
    private readonly SailingBootstrapService _bootstrap;
    private readonly ShipNavigationLoopService _navigation;
    private NwModule? _module;
    private bool _subscribed;
    private bool _started;
    private bool _disposed;

    public SailingModuleLifecycleService(EventService events, SailingFileStore files, PhysicalShipService physical,
        HelmService helm, ShipRetrievalNuiService retrieval, ShipTradeNuiService trade, ShipSpellService spells,
        ShipNativeSpellDispatcher dispatcher, ShipCombatNuiService combat, SailingBootstrapService bootstrap,
        ShipNavigationLoopService navigation, MaritimeLedgerService ledger)
    {
        _ledger = ledger;
        _events = events; _files = files; _physical = physical; _helm = helm;
        _retrieval = retrieval; _trade = trade; _spells = spells; _dispatcher = dispatcher;
        _combat = combat; _bootstrap = bootstrap; _navigation = navigation;
    }

    public void Init()
    {
        if (_disposed || _subscribed) return;
        // IInitializable runs after service construction/static injection. Use
        // the injected EventService to arm the same GameEventFactory hook used
        // by NwModule.OnModuleLoad; no NwObject.EventService access in a ctor.
        NwModule module = NwModule.Instance;
        try
        {
            _events.SubscribeAll<ModuleEvents.OnModuleLoad, GameEventFactory, GameEventFactory.RegistrationData>(
                new GameEventFactory.RegistrationData(module, true), HandleModuleLoad);
        }
        catch
        {
            _events.UnsubscribeAll<ModuleEvents.OnModuleLoad, GameEventFactory>(HandleModuleLoad);
            throw;
        }
        _module = module;
        _subscribed = true;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad evt)
    {
        if (_disposed || _started || _module == null) return;
        _started = true;
        try
        {
            _files.InitializeModule();
            _ledger.InitializeModule();
            _physical.InitializeModule(_module);
            _retrieval.InitializeModule(_module);
            _trade.InitializeModule(_module);
            _helm.InitializeModule(_module);
            _spells.InitializeModule(_module);
            _dispatcher.InitializeModule();
            _combat.InitializeModule(_module);
            _bootstrap.InitializeModule(_module);
            _ = _retrieval.RefreshAfterInitializationAsync(_bootstrap.Initialization);
            _navigation.Start(_bootstrap.Initialization);
            Log.Info("Sailing module-load registration completed; fleet restoration gates navigation.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Sailing module-load initialization failed; navigation was not started.");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_subscribed)
            _events.UnsubscribeAll<ModuleEvents.OnModuleLoad, GameEventFactory>(HandleModuleLoad);
        _subscribed = false;
        _module = null;
    }
}
