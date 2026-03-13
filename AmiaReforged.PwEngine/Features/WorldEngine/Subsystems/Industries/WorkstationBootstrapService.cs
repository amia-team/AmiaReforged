using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Bootstraps workstation placeables in the game world.
/// For each workstation definition with a matching placeable tag, subscribes to OnUsed
/// to open the workstation crafting NUI window.
///
/// Convention: the workstation's Tag value is used as the NWN placeable tag.
/// Example: Workstation with Tag "forge" → all placeables with tag "forge" will open the crafting UI.
///
/// Supports both static placeables (present at module load) and dynamically spawned placeables
/// via <see cref="RegisterPlaceable(NwPlaceable, WorkstationTag)"/> or
/// <see cref="RegisterPlaceable(NwPlaceable, Workstation)"/>.
/// </summary>
[ServiceBinding(typeof(WorkstationBootstrapService))]
public sealed class WorkstationBootstrapService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IWorkstationRepository _workstationRepository;
    private readonly WindowDirector _windowDirector;
    private readonly RecipeTemplateExpander _recipeTemplateExpander;

    /// <summary>
    /// Tracks which placeable object IDs have already been wired to prevent duplicate subscriptions.
    /// </summary>
    private readonly HashSet<uint> _wiredPlaceables = new();

    /// <summary>
    /// Maps placeable object IDs to their workstation definition for quick lookup on use.
    /// </summary>
    private readonly Dictionary<uint, Workstation> _placeableWorkstations = new();

    public WorkstationBootstrapService(
        IWorkstationRepository workstationRepository,
        WindowDirector windowDirector,
        RecipeTemplateExpander recipeTemplateExpander)
    {
        _workstationRepository = workstationRepository;
        _windowDirector = windowDirector;
        _recipeTemplateExpander = recipeTemplateExpander;

        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        try
        {
            RegisterWorkstations();
            _recipeTemplateExpander.ExpandAll();
            Log.Info("Recipe templates expanded at module load.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to register workstation placeables during module load.");
        }
    }

    // === Public API for dynamic registration ===

    /// <summary>
    /// Registers a dynamically spawned placeable as a workstation, looking up the
    /// workstation definition by tag from the repository.
    /// Call this after spawning a workstation placeable at runtime.
    /// </summary>
    /// <param name="placeable">The spawned NWN placeable object.</param>
    /// <param name="workstationTag">The workstation tag identifying which workstation definition to use.</param>
    /// <returns>True if successfully registered; false if the placeable was already wired,
    /// invalid, or the workstation tag was not found.</returns>
    public bool RegisterPlaceable(NwPlaceable placeable, WorkstationTag workstationTag)
    {
        Workstation? workstation = _workstationRepository.GetByTag(workstationTag);
        if (workstation == null)
        {
            Log.Warn("Cannot register placeable — workstation definition '{Tag}' not found.", workstationTag.Value);
            return false;
        }

        return RegisterPlaceable(placeable, workstation);
    }

    /// <summary>
    /// Registers a dynamically spawned placeable as a workstation using an already-resolved
    /// workstation definition. Useful when the caller already has the definition in hand.
    /// </summary>
    /// <param name="placeable">The spawned NWN placeable object.</param>
    /// <param name="workstation">The workstation definition to bind to this placeable.</param>
    /// <returns>True if successfully registered; false if the placeable was already wired or invalid.</returns>
    public bool RegisterPlaceable(NwPlaceable placeable, Workstation workstation)
    {
        if (placeable is not { IsValid: true })
        {
            Log.Warn("Cannot register workstation — placeable is null or invalid.");
            return false;
        }

        if (!_wiredPlaceables.Add(placeable.ObjectId))
        {
            Log.Debug("Placeable {Id} already registered as a workstation, skipping.", placeable.ObjectId);
            return false;
        }

        _placeableWorkstations[placeable.ObjectId] = workstation;
        placeable.OnUsed += HandleWorkstationUsed;

        Log.Info("Dynamically registered workstation placeable {Id} as '{Tag}' ({Name}).",
            placeable.ObjectId, workstation.Tag.Value, workstation.Name);
        return true;
    }

    /// <summary>
    /// Unregisters a workstation placeable, removing its OnUsed handler and internal tracking.
    /// Call this before destroying a dynamically spawned workstation placeable.
    /// </summary>
    /// <param name="placeable">The placeable to unregister.</param>
    /// <returns>True if the placeable was previously registered and has been removed.</returns>
    public bool UnregisterPlaceable(NwPlaceable placeable)
    {
        if (placeable == null) return false;

        if (!_wiredPlaceables.Remove(placeable.ObjectId))
            return false;

        _placeableWorkstations.Remove(placeable.ObjectId);
        placeable.OnUsed -= HandleWorkstationUsed;

        Log.Debug("Unregistered workstation placeable {Id}.", placeable.ObjectId);
        return true;
    }

    // === Bulk registration at module load ===

    /// <summary>
    /// Finds all workstation definitions and wires their matching placeables.
    /// </summary>
    private void RegisterWorkstations()
    {
        List<Workstation> workstations = _workstationRepository.All();
        int totalWired = 0;

        foreach (Workstation workstation in workstations)
        {
            string placeableTag = workstation.Tag.Value;
            IEnumerable<NwPlaceable> placeables = NwObject.FindObjectsWithTag<NwPlaceable>(placeableTag);

            foreach (NwPlaceable placeable in placeables)
            {
                if (RegisterPlaceable(placeable, workstation))
                    totalWired++;
            }
        }

        Log.Info("WorkstationBootstrap: wired {Count} workstation placeables across {Definitions} definitions.",
            totalWired, workstations.Count);
    }

    /// <summary>
    /// Called when a player uses a workstation placeable. Opens or toggles the crafting UI.
    /// </summary>
    private void HandleWorkstationUsed(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;
        if (player == null) return;

        if (!_placeableWorkstations.TryGetValue(obj.Placeable.ObjectId, out Workstation? workstation))
        {
            Log.Warn("Workstation placeable {Id} used but no mapping found.", obj.Placeable.ObjectId);
            return;
        }

        // Toggle: if window is already open, close it
        if (_windowDirector.IsWindowOpen(player, typeof(WorkstationCraftingPresenter)))
        {
            _windowDirector.CloseWindow(player, typeof(WorkstationCraftingPresenter));
            return;
        }

        OpenCraftingWindow(player, workstation);
    }

    /// <summary>
    /// Creates and opens the workstation crafting NUI window for a player.
    /// </summary>
    private void OpenCraftingWindow(NwPlayer player, Workstation workstation)
    {
        WorkstationCraftingView view = new(player, workstation.Tag, workstation.Name);
        _windowDirector.OpenWindow(view.Presenter);
    }
}
