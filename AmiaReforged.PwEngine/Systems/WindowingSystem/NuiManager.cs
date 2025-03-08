using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem;

[ServiceBinding(typeof(NuiManager))]
public sealed class NuiManager : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly InjectionService _injectionService;

    private readonly Dictionary<NwPlayer, List<INuiController>> _nuiControllers = new();

    private readonly List<INuiView> _nuiViews;
    private readonly WindowAutoCloseService _windowAutoCloseService;

    public NuiManager(InjectionService injectionService, WindowAutoCloseService windowAutoCloseService,
        IEnumerable<INuiView> nuiViews)
    {
        _injectionService = injectionService;
        _windowAutoCloseService = windowAutoCloseService;
        _nuiViews = nuiViews.OrderBy(view => view.Title).ToList();

        NwModule.Instance.OnNuiEvent += OnNuiEvent;
        NwModule.Instance.OnClientLeave += OnClientLeave;
    }

    void IDisposable.Dispose()
    {
        foreach (List<INuiController> controllers in _nuiControllers.Values)
        {
            foreach (INuiController controller in controllers)
            {
                controller.Close();
            }
        }

        _nuiControllers.Clear();
    }

    /// <summary>
    ///     Opens a window view using the specified controller.
    /// </summary>
    /// <param name="player">The player to show the window.</param>
    /// <param name="configure">
    ///     Additional configuration for the controller before it is initialized by the
    ///     <see cref="NuiManager" />.
    /// </param>
    /// <typeparam name="TView">The type of view to open.</typeparam>
    /// <typeparam name="TController">The type of controller for the view.</typeparam>
    /// <returns>The created controller. Null if the client cannot render windows.</returns>
    public TController? OpenWindow<TView, TController>(NwPlayer player, Action<TController?> configure = null)
        where TView : NuiView<TView>, new()
        where TController : NuiController<TView>, new()
    {
        TView view = (TView)GetWindowFromType(typeof(TView));
        if (view != null && player.TryCreateNuiWindow(view.WindowTemplate, out NuiWindowToken token))
        {
            TController? controller = _injectionService.Inject(new TController
            {
                View = view,
                Token = token
            });

            configure?.Invoke(controller);

            InitController(controller, player);
            return controller;
        }

        return null;
    }

    /// <summary>
    ///     Opens a window view using the view's default controller.
    /// </summary>
    /// <param name="player">The player opening the window.</param>
    /// <typeparam name="T">The type of view to open.</typeparam>
    public void OpenWindow<T>(NwPlayer player) where T : NuiView<T>, new()
    {
        INuiView view = GetWindowFromType(typeof(T));
        if (view != null) OpenWindow(player, view);
    }

    /// <summary>
    ///     Opens a window view using the view's default controller.
    /// </summary>
    /// <param name="player">The player opening the window.</param>
    /// <param name="view">The view to open.</param>
    public void OpenWindow(NwPlayer player, INuiView view)
    {
        INuiController? controller = view.CreateDefaultController(player);
        if (controller == null) return;

        _injectionService.Inject(controller);
        InitController(controller, player);
    }

    private INuiView GetWindowFromType(Type windowType)
    {
        foreach (INuiView view in _nuiViews)
        {
            if (view.GetType() == windowType) return view;
        }

        Log.Error(message: "Failed to find window of type {Type}", windowType.FullName);
        return null;
    }

    private void InitController(INuiController? controller, NwPlayer player)
    {
        if (controller.AutoClose) _windowAutoCloseService.RegisterWindowForAutoClose(controller);

        controller.Init();
        _nuiControllers.AddElement(player, controller);
    }

    private void OnNuiEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (_nuiControllers.TryGetValue(eventData.Player, out List<INuiController> playerControllers))
        {
            INuiController controller = null;
            int index;

            for (index = 0; index < playerControllers.Count; index++)
            {
                INuiController playerController = playerControllers[index];
                if (eventData.Token == playerController.Token)
                {
                    controller = playerController;
                    break;
                }
            }

            if (controller == null) return;

            controller.ProcessEvent(eventData);
            if (eventData.EventType == NuiEventType.Close)
            {
                controller.Close(false);
                playerControllers.RemoveAt(index);
            }
        }
    }

    private void OnClientLeave(ModuleEvents.OnClientLeave eventData)
    {
        if (_nuiControllers.TryGetValue(eventData.Player, out List<INuiController> playerControllers))
        {
            foreach (INuiController controller in playerControllers)
            {
                controller.Close();
            }

            _nuiControllers.Remove(eventData.Player);
        }
    }

    public bool WindowIsOpen(NwPlayer player, Type type)
    {
        _nuiControllers.TryGetValue(player, out List<INuiController>? playerControllers);

        return playerControllers != null && playerControllers.Any(controller => controller.GetType() == type);
    }
}