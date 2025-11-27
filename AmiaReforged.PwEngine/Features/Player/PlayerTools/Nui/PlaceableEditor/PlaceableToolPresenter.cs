using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaPersistence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

public sealed class PlaceableToolPresenter : ScryPresenter<PlaceableToolView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const bool TraceEnabled = false;
    private const string PersistPlcLocalInt = "persist_plc";
    private const string CharacterIdLocalString = "character_id";
    private const string SourceItemDataLocalString = "source_item_data";

    private readonly NwPlayer _player;
    private readonly PlaceableToolModel _model;
    private List<PlaceableBlueprint> _blueprints = [];
    private List<PlaceableBlueprint> _filteredBlueprints = [];

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private PlaceableBlueprint? _pendingSpawn;
    private NwPlaceable? _lastSelection;
    private PlaceableData? _savedSnapshot;
    private PlaceableData? _pendingSnapshot;
    private float? _savedOrientation;
    private float? _pendingOrientation;
    private bool _hasUnsavedChanges;
    private bool _selectionEditable;

    private readonly HashSet<string> _watchBlacklist = new(StringComparer.OrdinalIgnoreCase);
    private bool _watchersEnabled;
    private static readonly TimeSpan LiveApplyThrottle = TimeSpan.FromMilliseconds(50);
    private DateTime _lastApplyAt = DateTime.MinValue;

    private bool _recoverAllConfirmationPending;
    private DateTime _recoverAllConfirmationExpiry;

    public PlaceableToolPresenter(PlaceableToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new PlaceableToolModel(player);
    }

    public override PlaceableToolView View { get; }

    [Inject] private Lazy<PlaceablePersistenceService> PersistenceService { get; init; } = null!;
    [Inject] private Lazy<IPersistentObjectRepository> ObjectRepository { get; init; } = null!;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(320f, 80f, PlaceableToolView.WindowWidth, PlaceableToolView.WindowHeight)
        };

        Trace("InitBefore executed; window stub prepared.");
    }

    public override void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            _player.SendServerMessage("Failed to create the Placeable Tool window.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        Trace("Create invoked; TryCreateNuiWindow completed.");

        InitializeBinds();
        RefreshBlueprints();
        UpdateSelection(null);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        Trace(
            $"ProcessEvent type={eventData.EventType} element={eventData.ElementId ?? "<null>"} index={eventData.ArrayIndex} watchers={_watchersEnabled} selectionValid={_lastSelection?.IsValid ?? false}");

        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClick(eventData);
                break;
            case NuiEventType.Watch:
                HandleWatch(eventData);
                break;
        }
    }

    public override void Close()
    {
        if (_lastSelection != null && _lastSelection.IsValid)
        {
            Effect? selectionEffect = _lastSelection.ActiveEffects.FirstOrDefault(e => e.Tag == PlaceableToolModel.SelectionVfxTag);
            if (selectionEffect != null)
            {
                _lastSelection.RemoveEffect(selectionEffect);
            }
        }


        _blueprints.Clear();
        _pendingSpawn = null;
        _lastSelection = null;
        _savedSnapshot = null;
        _pendingSnapshot = null;
        _token.Close();
    }

    private void InitializeBinds()
    {
        ToggleBindWatch(false);

        Trace("InitializeBinds called; reset message and blueprint binds.");

        Token().SetBindValue(View.StatusMessage,
            "Select a blueprint to spawn or pick an existing placeable. Use the sliders to preview changes, then Save or Discard.");
        Token().SetBindValue(View.SelectionAvailable, false);
        Token().SetBindValue(View.SelectedName, "No placeable selected");
        Token().SetBindValue(View.SelectedLocation, string.Empty);
        Token().SetBindValue(View.BlueprintCount, 0);
        Token().SetBindValues(View.BlueprintNames, []);
        Token().SetBindValues(View.BlueprintResRefs, []);
        Token().SetBindValue(View.BlueprintSearch, string.Empty);
        Token().SetBindWatch(View.BlueprintSearch, true);

        ResetEditFields();

        // Leave watchers disabled until a selection is active to avoid idle watch spam.
    }

    private void HandleClick(ModuleEvents.OnNuiEvent eventData)
    {
        Trace($"HandleClick received element={eventData.ElementId ?? "<null>"} arrayIndex={eventData.ArrayIndex}");

        if (eventData.ElementId == View.RefreshButton.Id)
        {
            Trace("HandleClick dispatching RefreshBlueprints().");
            RefreshBlueprints();
            return;
        }

        if (eventData.ElementId == View.SelectExistingButton.Id)
        {
            Trace("HandleClick dispatching BeginSelectExisting().");
            BeginSelectExisting();
            return;
        }

        if (eventData.ElementId == View.SpawnButton.Id && eventData.ArrayIndex >= 0 &&
            eventData.ArrayIndex < _filteredBlueprints.Count)
        {
            Trace(
                $"HandleClick dispatching BeginSpawn() for index={eventData.ArrayIndex} resref={_filteredBlueprints[eventData.ArrayIndex].ResRef}.");
            BeginSpawn(_filteredBlueprints[eventData.ArrayIndex]);
            return;
        }

        if (eventData.ElementId == View.SaveButton.Id)
        {
            Trace("HandleClick dispatching SaveSelectedPlaceable().");
            SaveSelectedPlaceable();
            return;
        }

        if (eventData.ElementId == View.DiscardButton.Id)
        {
            Trace("HandleClick dispatching DiscardChanges().");
            DiscardChanges();
            return;
        }

        if (eventData.ElementId == View.ApplyTransformButton.Id)
        {
            Trace("HandleClick dispatching ApplyTransformToPosition().");
            ApplyTransformToPosition();
            return;
        }

        if (eventData.ElementId == View.RecoverButton.Id)
        {
            Trace("HandleClick dispatching RecoverSelectedPlaceable().");
            RecoverSelectedPlaceable();
            return;
        }

        if (eventData.ElementId == View.RecoverAllButton.Id)
        {
            Trace("HandleClick dispatching RecoverAllPlaceablesInArea().");
            HandleRecoverAllClick();
        }
    }

    private void RefreshBlueprints()
    {
        _blueprints = _model.CollectBlueprints().ToList();
        ApplyBlueprintFilter();
    }

    private void ApplyBlueprintFilter()
    {
        string search = Token().GetBindValue(View.BlueprintSearch) ?? string.Empty;

        _filteredBlueprints = string.IsNullOrWhiteSpace(search)
            ? _blueprints.ToList()
            : _blueprints.Where(bp =>
                bp.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                bp.ResRef.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Token().SetBindValues(View.BlueprintNames, _filteredBlueprints.Select(bp => bp.DisplayName).ToArray());
        Token().SetBindValues(View.BlueprintResRefs, _filteredBlueprints.Select(bp => bp.ResRef).ToArray());
        Token().SetBindValue(View.BlueprintCount, _filteredBlueprints.Count);

        Token().SetBindValue(View.StatusMessage,
            _blueprints.Count == 0
                ? "No placeable blueprints found in your inventory."
                : _filteredBlueprints.Count == 0
                    ? "No blueprints match your search."
                    : "Use a Target Spawn button to pick where the placeable should appear.");
    }

    private void BeginSpawn(PlaceableBlueprint blueprint)
    {
        Trace($"BeginSpawn starting for blueprint name={blueprint.DisplayName} resref={blueprint.ResRef}.");
        _pendingSpawn = blueprint;
        Token().SetBindValue(View.StatusMessage, $"Target a location to spawn '{blueprint.DisplayName}'.");

        _player.EnterTargetMode(HandleSpawnTarget,
            new TargetModeSettings
            {
                CursorType = MouseCursor.Action,
                ValidTargets = ObjectTypes.Tile | ObjectTypes.Placeable | ObjectTypes.Creature
            });
    }

    private void HandleSpawnTarget(ModuleEvents.OnPlayerTarget targetData)
    {
        Trace("HandleSpawnTarget invoked.");

        if (_pendingSpawn is null)
        {
            Trace("HandleSpawnTarget exiting; pending spawn is null.");
            return;
        }

        PlaceableBlueprint blueprint = _pendingSpawn;
        _pendingSpawn = null;

        Trace($"HandleSpawnTarget using pending blueprint name={blueprint.DisplayName} resref={blueprint.ResRef}.");

        NwArea? area = targetData.TargetObject switch
        {
            NwGameObject gameObject when gameObject.Area != null => gameObject.Area,
            _ => _player.ControlledCreature?.Area
        };

        if (area == null)
        {
            Trace("HandleSpawnTarget failed; resolved area null.");
            Token().SetBindValue(View.StatusMessage, "Unable to determine a valid area for spawn.");
            return;
        }

        Location? location = targetData.TargetObject switch
        {
            NwGameObject gameObject => gameObject.Location,
            _ => Location.Create(area, targetData.TargetPosition, 0f)
        };

        Trace($"HandleSpawnTarget resolved location area={area.Name} position={location?.Position ?? Vector3.Zero}.");

        NwPlaceable? placeable = NwPlaceable.Create(blueprint.ResRef, location);
        if (placeable == null)
        {
            Trace("HandleSpawnTarget failed; NwPlaceable.Create returned null.");
            Token().SetBindValue(View.StatusMessage, $"Failed to create placeable '{blueprint.DisplayName}'.");
            return;
        }

        placeable.HP = blueprint.HealthOverride > 0 ? blueprint.HealthOverride : placeable.HP;
        placeable.IsStatic = blueprint.IsStatic;
        placeable.PlotFlag = blueprint.IsPlot;
        placeable.Name = blueprint.DisplayName;
        EnsureCharacterAssociation(placeable);

        // Store serialized source item data on the placeable for later persistence
        if (blueprint.SourceItem.IsValid)
        {
            byte[]? itemSerialized = blueprint.SourceItem.Serialize();
            if (itemSerialized is not null && itemSerialized.Length > 0)
            {
                // Store as base64 string in local variable so it persists with the placeable
                string base64Data = Convert.ToBase64String(itemSerialized);
                placeable.GetObjectVariable<LocalVariableString>(SourceItemDataLocalString).Value = base64Data;
                Trace($"Stored source item data ({itemSerialized.Length} bytes) for placeable {placeable.Name}");
            }

            // Remove the source item from the player's inventory now that it's placed
            blueprint.SourceItem.Destroy();
        }

        try
        {
            PlaceableTableEntry appearanceRow = NwGameTables.PlaceableTable.GetRow(blueprint.Appearance);
            placeable.Appearance = appearanceRow;
        }
        catch
        {
            // Ignore invalid appearance rows and keep the default.
        }

        MarkPersistent(placeable);
        _ = PersistSpawnedPlaceable(placeable);

        _player.SendServerMessage($"Spawned placeable '{placeable.Name}'.", ColorConstants.Green);
        UpdateSelection(placeable);
        Token().SetBindValue(View.StatusMessage,
            $"Spawned '{placeable.Name}'.");
        RefreshBlueprints();
    }

    private void BeginSelectExisting()
    {
        Trace("BeginSelectExisting invoked.");
        Token().SetBindValue(View.StatusMessage, "Target a placeable (or the ground near it) to select.");

        _player.EnterTargetMode(HandleSelectTarget,
            new TargetModeSettings
            {
                CursorType = MouseCursor.Action,
                ValidTargets = ObjectTypes.Placeable | ObjectTypes.Tile
            });
    }

    private void HandleSelectTarget(ModuleEvents.OnPlayerTarget targetData)
    {
        Trace(
            $"HandleSelectTarget triggered; targetObject={targetData.TargetObject?.Name ?? "<null>"} position={targetData.TargetPosition}.");

        NwPlaceable? placeable = targetData.TargetObject as NwPlaceable;
        if (placeable == null)
        {
            Trace("HandleSelectTarget did not find placeable directly; attempting nearest lookup.");
            NwArea? area = _player.ControlledCreature?.Area;
            if (area == null)
            {
                Trace("HandleSelectTarget aborting; player area null.");
                Token().SetBindValue(View.StatusMessage, "No placeable found.");
                return;
            }

            Location location = Location.Create(area, targetData.TargetPosition, 0f);
            placeable = location.GetNearestObjectsByType<NwPlaceable>().FirstOrDefault();
            Trace(placeable == null
                ? "HandleSelectTarget nearest lookup failed to find placeable."
                : $"HandleSelectTarget nearest lookup found {placeable.Name}.");
        }

        if (placeable == null)
        {
            Trace("HandleSelectTarget giving up; no placeable found.");
            Token().SetBindValue(View.StatusMessage, "No placeable found at that location.");
            return;
        }

        Trace($"HandleSelectTarget succeeded; updating selection to {placeable.Name}.");

        UpdateSelection(placeable);
        Token().SetBindValue(View.StatusMessage, $"Selected '{placeable.Name}'.");
    }

    private void UpdateSelection(NwPlaceable? placeable)
    {
        if (_lastSelection is not null)
        {
            Effect? selectionEffect = _lastSelection.ActiveEffects.FirstOrDefault(e => e.Tag == PlaceableToolModel.SelectionVfxTag);
            if (selectionEffect is not null)
            {
                _lastSelection.RemoveEffect(selectionEffect);
            }
        }

        _lastSelection = placeable;
        Trace(placeable == null
            ? "UpdateSelection(null) invoked; disabling watchers and clearing binds."
            : $"UpdateSelection({placeable.Name}) invoked; placeable valid={placeable.IsValid}");

        if (placeable == null)
        {
            ToggleBindWatch(false);

            Token().SetBindValue(View.SelectionAvailable, false);
            Token().SetBindValue(View.SelectedName, "No placeable selected");
            Token().SetBindValue(View.SelectedLocation, string.Empty);
            ClearEditFields();
            _selectionEditable = false;
            return;
        }

        Effect vfx = Effect.VisualEffect(VfxType.DurAuraCyan);
        vfx.Tag = PlaceableToolModel.SelectionVfxTag;
        vfx.DurationType = EffectDuration.Permanent;

        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, vfx, placeable);

        ToggleBindWatch(false);

        if (!CanPlayerEdit(placeable, out string? denialReason))
        {
            _selectionEditable = false;

            Token().SetBindValue(View.SelectionAvailable, false);
            Token().SetBindValue(View.SelectedName, placeable.Name);
            Token().SetBindValue(View.SelectedLocation,
                $"{placeable.Position.X:F2}, {placeable.Position.Y:F2}, {placeable.Position.Z:F2}");

            ClearEditFields();

            string status = denialReason ?? "You do not have permission to edit this placeable.";
            Token().SetBindValue(View.StatusMessage, status);
            return;
        }

        _selectionEditable = true;

        Token().SetBindValue(View.SelectionAvailable, true);
        Token().SetBindValue(View.SelectedName, placeable.Name);
        Token().SetBindValue(View.SelectedLocation,
            $"{placeable.Position.X:F2}, {placeable.Position.Y:F2}, {placeable.Position.Z:F2}");
        _pendingOrientation = placeable.Location.Rotation;

        ToggleBindWatch(true);

        LoadSelectionState(placeable);
    }

    private void RecoverSelectedPlaceable()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Token().SetBindValue(View.StatusMessage, "No placeable selected to recover.");
            return;
        }

        if (!EnsureSelectionEditable("recover"))
        {
            return;
        }

        NwPlaceable placeable = _lastSelection;
        Token().SetBindValue(View.StatusMessage, $"Recovering '{placeable.Name}'...");

        _ = NwTask.Run(async () =>
        {
            try
            {
                await PersistenceService.Value.DeletePlaceableAsync(placeable);
            }
            catch (Exception ex)
            {
                await NwTask.SwitchToMainThread();
                if (Token().Player.IsValid)
                {
                    Token().SetBindValue(View.StatusMessage,
                        $"Failed to recover '{placeable.Name}': {ex.Message}");
                }

                return;
            }

            await NwTask.SwitchToMainThread();

            if (placeable.IsValid)
            {
                placeable.Destroy();
            }

            if (!Token().Player.IsValid)
            {
                return;
            }

            Token().SetBindValue(View.StatusMessage, $"Recovered '{placeable.Name}'.");
            UpdateSelection(null);
        });
    }

    private void HandleRecoverAllClick()
    {
        // Check if confirmation is still valid (within 10 seconds)
        if (_recoverAllConfirmationPending && DateTime.UtcNow < _recoverAllConfirmationExpiry)
        {
            // Confirmed - proceed with recovery
            _recoverAllConfirmationPending = false;
            RecoverAllPlaceablesInArea();
            return;
        }

        // Show confirmation dialog
        ShowRecoverAllConfirmation();
    }

    private void ShowRecoverAllConfirmation()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature?.Area == null)
        {
            Token().SetBindValue(View.StatusMessage, "Unable to determine your current area.");
            return;
        }

        Guid characterId = PcKeyUtils.GetPcKey(_player);
        if (characterId == Guid.Empty)
        {
            Token().SetBindValue(View.StatusMessage, "Unable to determine your character ID.");
            return;
        }

        string areaResRef = creature.Area.ResRef;

        // Query how many placeables the player has in this area
        List<PersistentObject> placeables =
            ObjectRepository.Value.GetPlaceablesForCharacterInArea(characterId, areaResRef);

        if (placeables.Count == 0)
        {
            Token().SetBindValue(View.StatusMessage, $"You have no placeables in this area.");
            _player.SendServerMessage("You have no placeables in this area to recover.", ColorConstants.Orange);
            return;
        }

        // Set confirmation state
        _recoverAllConfirmationPending = true;
        _recoverAllConfirmationExpiry = DateTime.UtcNow.AddSeconds(10);

        Token().SetBindValue(View.StatusMessage,
            $"Found {placeables.Count} placeable(s) to recover. Click again within 10 seconds to confirm.");
        _player.SendServerMessage(
            $"Found {placeables.Count} placeable(s) in this area. Click 'Recover All in Area' again within 10 seconds to confirm recovery.",
            ColorConstants.Yellow);
    }

    private void RecoverAllPlaceablesInArea()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature?.Area == null)
        {
            Token().SetBindValue(View.StatusMessage, "Unable to determine your current area.");
            return;
        }

        Guid characterId = PcKeyUtils.GetPcKey(_player);
        if (characterId == Guid.Empty)
        {
            Token().SetBindValue(View.StatusMessage, "Unable to determine your character ID.");
            return;
        }

        string areaResRef = creature.Area.ResRef;
        Token().SetBindValue(View.StatusMessage, "Recovering all placeables in area...");

        _ = NwTask.Run(async () =>
        {
            int recoveredCount = 0;
            int skippedNoData = 0;
            int skippedNoFit = 0;
            int failedCount = 0;

            List<PersistentObject> placeables =
                ObjectRepository.Value.GetPlaceablesForCharacterInArea(characterId, areaResRef);

            await NwTask.SwitchToMainThread();

            if (placeables.Count == 0)
            {
                if (Token().Player.IsValid)
                {
                    Token().SetBindValue(View.StatusMessage, "No placeables found to recover.");
                }

                return;
            }

            foreach (PersistentObject persistentObject in placeables)
            {
                try
                {
                    await NwTask.SwitchToMainThread();

                    // Check if there's source item data
                    if (persistentObject.SourceItemData == null || persistentObject.SourceItemData.Length == 0)
                    {
                        Log.Warn($"Placeable {persistentObject.Id} has no source item data, skipping recovery.");
                        skippedNoData++;
                        continue;
                    }

                    // Deserialize the item at a safe location
                    NwItem? item = NwItem.Deserialize(persistentObject.SourceItemData);
                    if (item == null)
                    {
                        Log.Error($"Failed to deserialize item for placeable {persistentObject.Id}");
                        failedCount++;
                        continue;
                    }

                    // Move item to starting location temporarily
                    item.Location = NwModule.Instance.StartingLocation;

                    // Check if it fits in player's inventory
                    if (!_player.LoginCreature.Inventory.CheckFit(item))
                    {
                        Log.Info(
                            $"Item from placeable {persistentObject.Id} does not fit in player inventory, destroying.");
                        item.Destroy();
                        skippedNoFit++;
                        continue;
                    }

                    // Acquire the item
                    _player.LoginCreature.AcquireItem(item);

                    // Delete from database
                    await ObjectRepository.Value.DeleteObject(persistentObject.Id);

                    // Find and destroy the in-game placeable
                    await NwTask.SwitchToMainThread();
                    NwPlaceable? inGamePlaceable = FindPlaceableByDatabaseId(creature.Area, persistentObject.Id);
                    if (inGamePlaceable != null && inGamePlaceable.IsValid)
                    {
                        inGamePlaceable.Destroy();
                    }

                    recoveredCount++;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Failed to recover placeable {persistentObject.Id}");
                    failedCount++;
                }
            }

            await NwTask.SwitchToMainThread();

            if (!Token().Player.IsValid)
            {
                return;
            }

            // Clear confirmation state
            _recoverAllConfirmationPending = false;

            // Clear selection if any placeables were recovered
            if (recoveredCount > 0)
            {
                UpdateSelection(null);
            }

            // Build detailed status message
            string statusMessage = $"Recovery complete: {recoveredCount} recovered";
            if (skippedNoFit > 0) statusMessage += $", {skippedNoFit} skipped (no space)";
            if (skippedNoData > 0) statusMessage += $", {skippedNoData} skipped (no data)";
            if (failedCount > 0) statusMessage += $", {failedCount} failed";

            Token().SetBindValue(View.StatusMessage, statusMessage);
            _player.SendServerMessage(statusMessage, ColorConstants.Green);
        });
    }

    private NwPlaceable? FindPlaceableByDatabaseId(NwArea area, long databaseId)
    {
        const string DatabaseIdLocalInt = "db_id";

        foreach (NwPlaceable placeable in area.FindObjectsOfTypeInArea<NwPlaceable>())
        {
            LocalVariableInt dbIdVar = placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt);
            if (dbIdVar.HasValue && dbIdVar.Value == databaseId)
            {
                return placeable;
            }
        }

        return null;
    }

    private void HandleWatch(ModuleEvents.OnNuiEvent eventData)
    {
        // Handle search filter changes
        if (eventData.ElementId == View.BlueprintSearch.Key)
        {
            Trace("HandleWatch detected search filter change.");
            ApplyBlueprintFilter();
            return;
        }

        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Trace($"HandleWatch ignored; selection invalid. element={eventData.ElementId ?? "<null>"}");
            return;
        }

        if (!_selectionEditable)
        {
            Trace("HandleWatch ignored; selection not editable.");
            return;
        }

        string? elementId = eventData.ElementId;
        if (string.IsNullOrWhiteSpace(elementId) || IsBlacklisted(elementId))
        {
            Trace(
                $"HandleWatch ignored; elementId empty={string.IsNullOrWhiteSpace(elementId)} blacklisted={IsBlacklisted(elementId)}");
            return;
        }

        bool shouldApply = false;

        Trace(
            $"HandleWatch processing element={elementId} selection={_lastSelection.Name} watchersEnabled={_watchersEnabled}");

        WithWatchDisabled(() =>
        {
            SyncNumericToString(elementId);

            if (TryHandleNumericTextPair(elementId))
            {
                Trace($"HandleWatch updated numeric/text pair for {elementId}.");
                shouldApply = true;
                return;
            }

            if (IsNumericSliderBind(elementId))
            {
                Trace($"HandleWatch detected slider update for {elementId}.");
                shouldApply = true;
            }
        });

        if (shouldApply)
        {
            Trace($"HandleWatch scheduling ApplyPendingData for {elementId}.");
            ApplyPendingData();
        }
    }

    private void SaveSelectedPlaceable()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Token().SetBindValue(View.StatusMessage, "No placeable selected to save.");
            return;
        }

        if (!EnsureSelectionEditable("save changes to"))
        {
            return;
        }

        NwPlaceable placeable = _lastSelection;

        if (!_hasUnsavedChanges)
        {
            Token().SetBindValue(View.StatusMessage, $"No pending changes for '{placeable.Name}'.");
            return;
        }

        Token().SetBindValue(View.StatusMessage, $"Saving '{placeable.Name}'...");
        EnsureCharacterAssociation(placeable);
        MarkPersistent(placeable);

        _ = NwTask.Run(async () =>
        {
            try
            {
                await PersistenceService.Value.SaveSinglePlaceable(placeable);
            }
            catch (Exception ex)
            {
                await NwTask.SwitchToMainThread();
                if (Token().Player.IsValid)
                {
                    Token().SetBindValue(View.StatusMessage,
                        $"Failed to save '{placeable.Name}': {ex.Message}");
                }

                return;
            }

            await NwTask.SwitchToMainThread();

            if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
            {
                return;
            }

            _savedSnapshot = PlaceableDataFactory.From(placeable);
            _pendingSnapshot = _savedSnapshot;
            _savedOrientation = placeable.Location.Rotation;
            _pendingOrientation = _savedOrientation;
            _hasUnsavedChanges = false;

            PushDataToView(_savedSnapshot);
            Token().SetBindValue(View.StatusMessage, $"Saved '{placeable.Name}' to persistence.");
        });
    }

    private void DiscardChanges()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Token().SetBindValue(View.StatusMessage, "No placeable selected to discard.");
            return;
        }

        if (!EnsureSelectionEditable("discard changes to"))
        {
            return;
        }

        NwPlaceable placeable = _lastSelection;
        Token().SetBindValue(View.StatusMessage, $"Discarding changes to '{placeable.Name}'...");

        _ = NwTask.Run(async () =>
        {
            PersistentObject? persisted;
            try
            {
                persisted = await PersistenceService.Value.GetPersistentObjectAsync(placeable);
            }
            catch (Exception ex)
            {
                await NwTask.SwitchToMainThread();
                if (Token().Player.IsValid)
                {
                    Token().SetBindValue(View.StatusMessage,
                        $"Failed to load persisted state: {ex.Message}");
                }

                return;
            }

            if (persisted is null)
            {
                await NwTask.SwitchToMainThread();
                if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
                {
                    return;
                }

                if (_savedSnapshot is null)
                {
                    Token().SetBindValue(View.StatusMessage,
                        $"'{placeable.Name}' has no persisted state to revert to.");
                    return;
                }

                ApplyDataToPlaceable(placeable, _savedSnapshot, _savedOrientation);
                PushDataToView(_savedSnapshot);
                _pendingSnapshot = _savedSnapshot;
                _pendingOrientation = _savedOrientation;
                _hasUnsavedChanges = false;
                Token().SetBindValue(View.StatusMessage, $"Reverted '{placeable.Name}' to the loaded state.");
                return;
            }

            PlaceableData? persistedData = await BuildDataFromPersistentObject(persisted);

            await NwTask.SwitchToMainThread();

            if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
            {
                return;
            }

            if (persistedData is null)
            {
                Token().SetBindValue(View.StatusMessage,
                    $"Failed to interpret the persisted snapshot for '{placeable.Name}'.");
                return;
            }

            float? orientation = persisted.Location?.Orientation;
            ApplyDataToPlaceable(placeable, persistedData, orientation);
            PushDataToView(persistedData);

            _savedSnapshot = persistedData;
            _pendingSnapshot = persistedData;
            _savedOrientation = orientation ?? placeable.Location.Rotation;
            _pendingOrientation = _savedOrientation;
            _hasUnsavedChanges = false;

            Token().SetBindValue(View.StatusMessage, $"Discarded changes to '{placeable.Name}'.");
        });
    }

    private void LoadSelectionState(NwPlaceable placeable)
    {
        string placeableName = placeable.Name;
        Trace($"LoadSelectionState started for {placeableName} (valid={placeable.IsValid}).");
        Token().SetBindValue(View.StatusMessage, $"Selected '{placeableName}'. Loading persisted state...");

        _ = NwTask.Run(async () =>
        {
            Trace($"LoadSelectionState async query starting for {placeableName}.");
            PersistentObject? persisted = null;
            try
            {
                persisted = await PersistenceService.Value.GetPersistentObjectAsync(placeable);
            }
            catch (Exception ex)
            {
                Trace($"LoadSelectionState persistence query threw: {ex.Message}");
                await NwTask.SwitchToMainThread();
                if (Token().Player.IsValid)
                {
                    Token().SetBindValue(View.StatusMessage,
                        $"Failed to query persistence: {ex.Message}");
                }

                return;
            }

            Trace($"LoadSelectionState continuing on main thread for {placeableName}.");
            await NwTask.SwitchToMainThread();

            if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
            {
                Trace("LoadSelectionState aborting; player/token invalid or selection changed.");
                return;
            }

            PlaceableData currentData = PlaceableDataFactory.From(placeable);
            _pendingSnapshot = currentData;
            _pendingOrientation = placeable.Location.Rotation;

            PlaceableData savedData = currentData;
            float orientation = placeable.Location.Rotation;

            if (persisted != null)
            {
                Trace($"LoadSelectionState found persisted snapshot for {placeable.Name}; building data.");
                PlaceableData? persistedData = await BuildDataFromPersistentObject(persisted);
                Trace(persistedData is null
                    ? "LoadSelectionState persisted data build returned null."
                    : "LoadSelectionState persisted data build succeeded.");
                await NwTask.SwitchToMainThread();

                if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
                {
                    Trace("LoadSelectionState aborting after persisted build; selection changed or invalid.");
                    return;
                }

                if (persistedData is not null)
                {
                    Trace("LoadSelectionState applying persisted snapshot to selection.");
                    savedData = persistedData;
                    if (persisted.Location is not null && placeable.Area is not null)
                    {
                        Trace("LoadSelectionState teleporting placeable to persisted location.");
                        orientation = persisted.Location.Orientation;
                        Vector3 savedPosition = new(persisted.Location.X, persisted.Location.Y, persisted.Location.Z);
                        placeable.Location = Location.Create(placeable.Area, savedPosition, orientation);
                        _pendingSnapshot = savedData with
                        {
                            Position = new PlaceableAreaPositionData(savedPosition)
                        };
                        _pendingOrientation = orientation;
                    }
                }
                else
                {
                    Trace("LoadSelectionState falling back to current state due to missing persisted snapshot.");
                    Token().SetBindValue(View.StatusMessage,
                        $"Selected '{placeable.Name}'. Unable to read persisted snapshot, using current state.");
                }
            }

            _savedSnapshot = savedData;
            _savedOrientation = orientation;
            _hasUnsavedChanges = false;

            PushDataToView(_pendingSnapshot ?? savedData);
            Trace("LoadSelectionState pushed data to view.");
            Token().SetBindValue(View.StatusMessage,
                $"Selected '{placeable.Name}'. Adjust sliders then Save to persist.");
        });
    }

    private static async Task<PlaceableData?> BuildDataFromPersistentObject(PersistentObject persisted)
    {
        if (persisted.Serialized.Length == 0)
        {
            return null;
        }

        await NwTask.SwitchToMainThread();

        NwPlaceable? snapshot = NwPlaceable.Deserialize(persisted.Serialized);
        if (snapshot is null)
        {
            return null;
        }

        try
        {
            Vector3 position = persisted.Location is not null
                ? new Vector3(persisted.Location.X, persisted.Location.Y, persisted.Location.Z)
                : snapshot.Position;

            return new PlaceableData(
                snapshot.Name,
                snapshot.Description,
                new PlaceableTransformData(snapshot.VisualTransform.Translation, snapshot.VisualTransform.Rotation,
                    snapshot.VisualTransform.Scale),
                new PlaceableAppearanceData(snapshot.Appearance.RowIndex, snapshot.PortraitResRef),
                new PlaceableAreaPositionData(position));
        }
        finally
        {
            snapshot.Destroy();
        }
    }

    private void PushDataToView(PlaceableData data)
    {
        WithWatchDisabled(() =>
        {
            Token().SetBindValue(View.PositionX, data.Position.Position.X);
            Token().SetBindValue(View.PositionY, data.Position.Position.Y);
            Token().SetBindValue(View.PositionZ, data.Position.Position.Z);

            // Swap X/Y: UI X shows Translation.Y (east/west), UI Y shows Translation.X (north/south)
            Token().SetBindValue(View.TransformX, data.Transform.Translation.Y);
            Token().SetBindValue(View.TransformY, data.Transform.Translation.X);
            Token().SetBindValue(View.TransformZ, data.Transform.Translation.Z);

            Token().SetBindValue(View.RotationX, data.Transform.Rotation.X);
            Token().SetBindValue(View.RotationY, data.Transform.Rotation.Y);
            Token().SetBindValue(View.RotationZ, data.Transform.Rotation.Z);

            Token().SetBindValue(View.Scale, data.Transform.Scale);

            // Raw radians - no conversion
            float orientationRadians = _pendingOrientation ?? 0f;
            Token().SetBindValue(View.Orientation, orientationRadians);

            Token().SetBindValue(View.PositionXString,
                data.Position.Position.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.PositionYString,
                data.Position.Position.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.PositionZString,
                data.Position.Position.Z.ToString(CultureInfo.InvariantCulture));

            // Swap X/Y for string binds to match float binds
            Token().SetBindValue(View.TransformXString,
                data.Transform.Translation.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.TransformYString,
                data.Transform.Translation.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.TransformZString,
                data.Transform.Translation.Z.ToString(CultureInfo.InvariantCulture));

            Token().SetBindValue(View.RotationXString,
                data.Transform.Rotation.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.RotationYString,
                data.Transform.Rotation.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.RotationZString,
                data.Transform.Rotation.Z.ToString(CultureInfo.InvariantCulture));

            Token().SetBindValue(View.ScaleString, data.Transform.Scale.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.OrientationString, orientationRadians.ToString(CultureInfo.InvariantCulture));
        });
    }

    private void ApplyPendingData()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Trace("ApplyPendingData aborted; selection invalid.");
            return;
        }

        if (!_selectionEditable)
        {
            Trace("ApplyPendingData ignored; selection not editable.");
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (now - _lastApplyAt < LiveApplyThrottle)
        {
            Trace($"ApplyPendingData throttled; delta={(now - _lastApplyAt).TotalMilliseconds:F2}ms.");
            return;
        }

        _lastApplyAt = now;

        Trace("ApplyPendingData executing transform update.");

        Vector3 position = new(
            Token().GetBindValue(View.PositionX),
            Token().GetBindValue(View.PositionY),
            Token().GetBindValue(View.PositionZ));

        // Swap X/Y: UI TransformX is engine Y, UI TransformY is engine X
        Vector3 translation = new(
            Token().GetBindValue(View.TransformY),
            Token().GetBindValue(View.TransformX),
            Token().GetBindValue(View.TransformZ));

        Vector3 rotation = new(
            Token().GetBindValue(View.RotationX),
            Token().GetBindValue(View.RotationY),
            Token().GetBindValue(View.RotationZ));

        float scale = Token().GetBindValue(View.Scale);

        // Raw radians - no conversion
        float orientationRadians = Token().GetBindValue(View.Orientation);

        PlaceableData baseline = _pendingSnapshot ?? PlaceableDataFactory.From(_lastSelection);
        PlaceableData updated = baseline with
        {
            Transform = new PlaceableTransformData(translation, rotation, scale),
            Position = new PlaceableAreaPositionData(position)
        };

        _pendingSnapshot = updated;
        _pendingOrientation = orientationRadians;

        ApplyDataToPlaceable(_lastSelection, updated, _pendingOrientation);

        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
            Token().SetBindValue(View.StatusMessage,
                $"Previewing changes to '{_lastSelection.Name}'. Save to persist or Discard to revert.");
        }
    }

    private void ApplyTransformToPosition()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Token().SetBindValue(View.StatusMessage, "No placeable selected.");
            return;
        }

        if (!EnsureSelectionEditable("apply transform to"))
        {
            return;
        }

        // Get current position (engine coordinates)
        Vector3 currentPosition = new(
            Token().GetBindValue(View.PositionX),
            Token().GetBindValue(View.PositionY),
            Token().GetBindValue(View.PositionZ));

        // Read translation directly from UI (negate Y to correct direction)
        float uiTranslationX = Token().GetBindValue(View.TransformX);
        float uiTranslationY = -Token().GetBindValue(View.TransformY);
        float uiTranslationZ = Token().GetBindValue(View.TransformZ);

        // Get orientation in degrees and convert to radians (negate for clockwise rotation)
        float orientationDegrees = Token().GetBindValue(View.Orientation);
        float orientationRadians = -orientationDegrees * (float)(Math.PI / 180.0);

        // Rotate local translation by orientation to get world-space delta
        float cos = (float)Math.Cos(orientationRadians);
        float sin = (float)Math.Sin(orientationRadians);

        // At 0°: UI X should map to Position X, UI Y should map to Position Y (direct add)
        // Rotation handles non-zero orientations
        float worldDeltaX = uiTranslationX * cos - uiTranslationY * sin;
        float worldDeltaY = uiTranslationX * sin + uiTranslationY * cos;

        // Apply to position
        Vector3 newPosition = new(
            currentPosition.X + worldDeltaX,
            currentPosition.Y + worldDeltaY,
            currentPosition.Z + uiTranslationZ);

        // Update the bindings - position gets the combined value, translation resets to zero
        WithWatchDisabled(() =>
        {
            // Update position
            Token().SetBindValue(View.PositionX, newPosition.X);
            Token().SetBindValue(View.PositionY, newPosition.Y);
            Token().SetBindValue(View.PositionZ, newPosition.Z);
            Token().SetBindValue(View.PositionXString, newPosition.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.PositionYString, newPosition.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.PositionZString, newPosition.Z.ToString(CultureInfo.InvariantCulture));

            // Reset translation to zero
            Token().SetBindValue(View.TransformX, 0f);
            Token().SetBindValue(View.TransformY, 0f);
            Token().SetBindValue(View.TransformZ, 0f);
            Token().SetBindValue(View.TransformXString, "0");
            Token().SetBindValue(View.TransformYString, "0");
            Token().SetBindValue(View.TransformZString, "0");
        });

        // Update the pending snapshot and apply to placeable
        Vector3 rotation = new(
            Token().GetBindValue(View.RotationX),
            Token().GetBindValue(View.RotationY),
            Token().GetBindValue(View.RotationZ));
        float scale = Token().GetBindValue(View.Scale);

        PlaceableData baseline = _pendingSnapshot ?? PlaceableDataFactory.From(_lastSelection);
        PlaceableData updated = baseline with
        {
            Transform = new PlaceableTransformData(Vector3.Zero, rotation, scale),
            Position = new PlaceableAreaPositionData(newPosition)
        };

        _pendingSnapshot = updated;
        ApplyDataToPlaceable(_lastSelection, updated, _pendingOrientation);

        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
        }

        Token().SetBindValue(View.StatusMessage,
            $"Applied transform to position for '{_lastSelection.Name}'. Save to persist.");
    }

    private static void ApplyDataToPlaceable(NwPlaceable placeable, PlaceableData data, float? orientation)
    {
        placeable.VisualTransform.Translation = data.Transform.Translation;
        placeable.VisualTransform.Rotation = data.Transform.Rotation;
        placeable.VisualTransform.Scale = data.Transform.Scale;

        if (placeable.Area is not null)
        {
            // Raw radians - Location.Create expects radians
            float facingRadians = orientation ?? placeable.Location.Rotation;
            placeable.Location = Location.Create(placeable.Area, data.Position.Position, facingRadians);
        }
        else
        {
            placeable.Position = data.Position.Position;
        }
    }

    private void ResetEditFields()
    {
        Token().SetBindValue(View.PositionX, 0f);
        Token().SetBindValue(View.PositionY, 0f);
        Token().SetBindValue(View.PositionZ, 0f);

        Token().SetBindValue(View.TransformX, 0f);
        Token().SetBindValue(View.TransformY, 0f);
        Token().SetBindValue(View.TransformZ, 0f);

        Token().SetBindValue(View.RotationX, 0f);
        Token().SetBindValue(View.RotationY, 0f);
        Token().SetBindValue(View.RotationZ, 0f);

        Token().SetBindValue(View.Scale, 1f);
        Token().SetBindValue(View.Orientation, 0f);

        Token().SetBindValue(View.PositionXString, "0");
        Token().SetBindValue(View.PositionYString, "0");
        Token().SetBindValue(View.PositionZString, "0");
        Token().SetBindValue(View.TransformXString, "0");
        Token().SetBindValue(View.TransformYString, "0");
        Token().SetBindValue(View.TransformZString, "0");
        Token().SetBindValue(View.RotationXString, "0");
        Token().SetBindValue(View.RotationYString, "0");
        Token().SetBindValue(View.RotationZString, "0");
        Token().SetBindValue(View.ScaleString, "1");
        Token().SetBindValue(View.OrientationString, "0");
    }

    private void ClearEditFields()
    {
        WithWatchDisabled(ResetEditFields);
        _savedSnapshot = null;
        _pendingSnapshot = null;
        _savedOrientation = null;
        _pendingOrientation = null;
        _hasUnsavedChanges = false;
    }

    private bool EnsureSelectionEditable(string actionDescription)
    {
        if (_selectionEditable)
        {
            return true;
        }

        if (_lastSelection is { IsValid: true } placeable)
        {
            Token().SetBindValue(View.StatusMessage,
                $"You do not have permission to {actionDescription} '{placeable.Name}'.");
        }
        else
        {
            Token().SetBindValue(View.StatusMessage, "No placeable selected.");
        }

        return false;
    }

    private bool CanPlayerEdit(NwPlaceable placeable, out string? denialReason)
    {
        denialReason = null;

        if (_player.IsDM)
        {
            return true;
        }

        Guid playerId = PcKeyUtils.GetPcKey(_player);
        if (playerId == Guid.Empty)
        {
            denialReason = "Unable to determine your character key; cannot edit placeables.";
            return false;
        }

        LocalVariableString ownerVar = placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString);
        if (!ownerVar.HasValue || string.IsNullOrWhiteSpace(ownerVar.Value))
        {
            denialReason = $"'{placeable.Name}' is not assigned to any character and cannot be edited.";
            return false;
        }

        if (!Guid.TryParse(ownerVar.Value, out Guid ownerId))
        {
            denialReason = $"'{placeable.Name}' has invalid ownership data.";
            return false;
        }

        if (ownerId != playerId)
        {
            denialReason = $"You do not own '{placeable.Name}'.";
            return false;
        }

        return true;
    }

    private void ToggleBindWatch(bool enable)
    {
        bool previouslyEnabled = _watchersEnabled;
        if (previouslyEnabled == enable)
        {
            Trace($"ToggleBindWatch called; state unchanged (enable={enable}).");
        }
        else
        {
            Trace($"ToggleBindWatch switching {previouslyEnabled} -> {enable}.");
        }

        _watchersEnabled = enable;

        Token().SetBindWatch(View.PositionX, enable);
        Token().SetBindWatch(View.PositionY, enable);
        Token().SetBindWatch(View.PositionZ, enable);
        Token().SetBindWatch(View.PositionXString, enable);
        Token().SetBindWatch(View.PositionYString, enable);
        Token().SetBindWatch(View.PositionZString, enable);

        Token().SetBindWatch(View.TransformX, enable);
        Token().SetBindWatch(View.TransformY, enable);
        Token().SetBindWatch(View.TransformZ, enable);
        Token().SetBindWatch(View.TransformXString, enable);
        Token().SetBindWatch(View.TransformYString, enable);
        Token().SetBindWatch(View.TransformZString, enable);

        Token().SetBindWatch(View.RotationX, enable);
        Token().SetBindWatch(View.RotationY, enable);
        Token().SetBindWatch(View.RotationZ, enable);
        Token().SetBindWatch(View.RotationXString, enable);
        Token().SetBindWatch(View.RotationYString, enable);
        Token().SetBindWatch(View.RotationZString, enable);

        Token().SetBindWatch(View.Scale, enable);
        Token().SetBindWatch(View.ScaleString, enable);

        Token().SetBindWatch(View.Orientation, enable);
        Token().SetBindWatch(View.OrientationString, enable);
    }

    private void WithWatchDisabled(Action action)
    {
        bool wasEnabled = _watchersEnabled;
        if (wasEnabled)
        {
            Trace("WithWatchDisabled temporarily disabling watchers.");
            ToggleBindWatch(false);
        }

        try
        {
            action();
        }
        finally
        {
            if (wasEnabled)
            {
                Trace("WithWatchDisabled restoring watchers.");
                ToggleBindWatch(true);
            }
        }
    }

    private void WithWatchBlacklist(IEnumerable<string> ids, Action action)
    {
        foreach (string id in ids)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _watchBlacklist.Add(id);
            }
        }

        try
        {
            action();
        }
        finally
        {
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _watchBlacklist.Remove(id);
                }
            }
        }
    }

    private bool IsBlacklisted(string? elementId)
        => !string.IsNullOrEmpty(elementId) && _watchBlacklist.Contains(elementId);

    private bool IsNumericSliderBind(string elementId)
    {
        return elementId.Equals(View.PositionX.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.PositionY.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.PositionZ.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.TransformX.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.TransformY.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.TransformZ.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.RotationX.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.RotationY.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.RotationZ.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.Scale.Key, StringComparison.OrdinalIgnoreCase) ||
               elementId.Equals(View.Orientation.Key, StringComparison.OrdinalIgnoreCase);
    }

    private void SyncNumericToString(string elementId)
    {
        void Sync(NuiBind<float> numeric, NuiBind<string> text)
        {
            if (!elementId.Equals(numeric.Key, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            float value = Token().GetBindValue(numeric);
            WithWatchBlacklist([text.Key],
                () => Token().SetBindValue(text, value.ToString(CultureInfo.InvariantCulture)));
        }

        Sync(View.PositionX, View.PositionXString);
        Sync(View.PositionY, View.PositionYString);
        Sync(View.PositionZ, View.PositionZString);
        Sync(View.TransformX, View.TransformXString);
        Sync(View.TransformY, View.TransformYString);
        Sync(View.TransformZ, View.TransformZString);
        Sync(View.RotationX, View.RotationXString);
        Sync(View.RotationY, View.RotationYString);
        Sync(View.RotationZ, View.RotationZString);
        Sync(View.Scale, View.ScaleString);
        Sync(View.Orientation, View.OrientationString);
    }

    private bool TryHandleNumericTextPair(string elementId)
    {
        bool Handle(NuiBind<float> numeric, NuiBind<string> text)
        {
            if (!elementId.Equals(text.Key, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string raw = Token().GetBindValue(text) ?? string.Empty;
            string sanitized = SanitizeNumericString(raw);

            if (!string.Equals(raw, sanitized, StringComparison.Ordinal))
            {
                WithWatchBlacklist([text.Key], () => Token().SetBindValue(text, sanitized));
            }

            if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                WithWatchBlacklist([numeric.Key], () => Token().SetBindValue(numeric, value));
            }

            return true;
        }

        return Handle(View.PositionX, View.PositionXString) ||
               Handle(View.PositionY, View.PositionYString) ||
               Handle(View.PositionZ, View.PositionZString) ||
               Handle(View.TransformX, View.TransformXString) ||
               Handle(View.TransformY, View.TransformYString) ||
               Handle(View.TransformZ, View.TransformZString) ||
               Handle(View.RotationX, View.RotationXString) ||
               Handle(View.RotationY, View.RotationYString) ||
               Handle(View.RotationZ, View.RotationZString) ||
               Handle(View.Scale, View.ScaleString) ||
               Handle(View.Orientation, View.OrientationString);
    }

    private static string SanitizeNumericString(string input)
    {
        bool seenDot = false;
        bool seenSign = false;
        List<char> buffer = new(input.Length);

        foreach (char c in input)
        {
            if (!seenSign && buffer.Count == 0 && (c == '-' || c == '+'))
            {
                buffer.Add(c);
                seenSign = true;
                continue;
            }

            if (char.IsDigit(c))
            {
                buffer.Add(c);
                continue;
            }

            if (c == '.' && !seenDot)
            {
                buffer.Add('.');
                seenDot = true;
            }
        }

        if (buffer.Count == 0 || (buffer.Count == 1 && (buffer[0] == '-' || buffer[0] == '+')))
        {
            return string.Empty;
        }

        if (buffer[0] == '+')
        {
            buffer.RemoveAt(0);
        }

        return new string(buffer.ToArray());
    }

    private void EnsureCharacterAssociation(NwPlaceable placeable)
    {
        Guid characterId = PcKeyUtils.GetPcKey(_player);
        if (characterId == Guid.Empty)
        {
            return;
        }

        LocalVariableString characterVar = placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString);
        characterVar.Value = characterId.ToString();
    }

    private static void MarkPersistent(NwPlaceable placeable)
    {
        LocalVariableInt persistVar = placeable.GetObjectVariable<LocalVariableInt>(PersistPlcLocalInt);
        persistVar.Value = 1;
    }

    private Task PersistSpawnedPlaceable(NwPlaceable placeable)
    {
        return NwTask.Run(async () =>
        {
            try
            {
                await PersistenceService.Value.SaveSinglePlaceable(placeable);
                await NwTask.SwitchToMainThread();

                if (!Token().Player.IsValid)
                {
                    return;
                }

                if (placeable.IsValid)
                {
                    Token().SetBindValue(View.StatusMessage,
                        $"Spawned '{placeable.Name}' and saved to persistence.");
                }
            }
            catch (Exception ex)
            {
                await NwTask.SwitchToMainThread();

                if (!Token().Player.IsValid)
                {
                    return;
                }

                Token().SetBindValue(View.StatusMessage,
                    $"Spawned '{placeable.Name}', but failed to save: {ex.Message}");
            }
        });
    }

    private void Trace(string message)
    {
        if (!TraceEnabled)
        {
            return;
        }

        string playerName = _player?.PlayerName ?? "<unknown>";
        Log.Info($"[PlaceableToolPresenter][Player={playerName}] {message}");
    }
}
