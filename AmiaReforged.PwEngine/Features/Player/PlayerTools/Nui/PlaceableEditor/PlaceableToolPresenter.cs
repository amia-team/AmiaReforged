using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.AreaPersistence;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

public sealed class PlaceableToolPresenter : ScryPresenter<PlaceableToolView>
{
    private const string PersistPlcLocalInt = "persist_plc";

    private readonly NwPlayer _player;
    private readonly PlaceableToolModel _model;
    private List<PlaceableBlueprint> _blueprints = new();

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private PlaceableBlueprint? _pendingSpawn;
    private NwPlaceable? _lastSelection;
    private PlaceableData? _savedSnapshot;
    private PlaceableData? _pendingSnapshot;
    private float? _savedOrientation;
    private float? _pendingOrientation;
    private bool _hasUnsavedChanges;

    private readonly HashSet<string> _watchBlacklist = new(StringComparer.OrdinalIgnoreCase);
    private bool _watchersEnabled;
    private static readonly TimeSpan LiveApplyThrottle = TimeSpan.FromMilliseconds(50);
    private DateTime _lastApplyAt = DateTime.MinValue;

    public PlaceableToolPresenter(PlaceableToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new PlaceableToolModel(player);
    }

    public override PlaceableToolView View { get; }

    [Inject] private Lazy<PlaceablePersistenceService> PersistenceService { get; init; } = null!;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(320f, 80f, 520f, 760f),
            Resizable = false
        };
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

    InitializeBinds();
    RefreshBlueprints();
    UpdateSelection(null);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
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

        Token().SetBindValue(View.StatusMessage,
            "Select a blueprint to spawn or pick an existing placeable. Use the sliders to preview changes, then Save or Discard.");
        Token().SetBindValue(View.SelectionAvailable, false);
        Token().SetBindValue(View.SelectedName, "No placeable selected");
        Token().SetBindValue(View.SelectedLocation, string.Empty);
        Token().SetBindValue(View.BlueprintCount, 0);
        Token().SetBindValues(View.BlueprintNames, Array.Empty<string>());
        Token().SetBindValues(View.BlueprintResRefs, Array.Empty<string>());

        ResetEditFields();

        ToggleBindWatch(true);
    }

    private void HandleClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.RefreshButton.Id)
        {
            RefreshBlueprints();
            return;
        }

        if (eventData.ElementId == View.SelectExistingButton.Id)
        {
            BeginSelectExisting();
            return;
        }

        if (eventData.ElementId == View.SpawnButton.Id && eventData.ArrayIndex >= 0 &&
            eventData.ArrayIndex < _blueprints.Count)
        {
            BeginSpawn(_blueprints[eventData.ArrayIndex]);
            return;
        }

        if (eventData.ElementId == View.SaveButton.Id)
        {
            SaveSelectedPlaceable();
            return;
        }

        if (eventData.ElementId == View.DiscardButton.Id)
        {
            DiscardChanges();
            return;
        }

        if (eventData.ElementId == View.RecoverButton.Id)
        {
            RecoverSelectedPlaceable();
        }
    }

    private void RefreshBlueprints()
    {
        _blueprints = _model.CollectBlueprints().ToList();

        Token().SetBindValues(View.BlueprintNames, _blueprints.Select(bp => bp.DisplayName).ToArray());
        Token().SetBindValues(View.BlueprintResRefs, _blueprints.Select(bp => bp.ResRef).ToArray());
        Token().SetBindValue(View.BlueprintCount, _blueprints.Count);

        Token().SetBindValue(View.StatusMessage,
            _blueprints.Count == 0
                ? "No placeable blueprints found in your inventory."
                : "Use a Target Spawn button to pick where the placeable should appear.");
    }

    private void BeginSpawn(PlaceableBlueprint blueprint)
    {
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
        if (_pendingSpawn is null)
        {
            return;
        }

        PlaceableBlueprint blueprint = _pendingSpawn;
        _pendingSpawn = null;

        NwArea? area = targetData.TargetObject switch
        {
            NwGameObject gameObject when gameObject.Area != null => gameObject.Area,
            _ => _player.ControlledCreature?.Area
        };

        if (area == null)
        {
            Token().SetBindValue(View.StatusMessage, "Unable to determine a valid area for spawn.");
            return;
        }

        Location? location = targetData.TargetObject switch
        {
            NwGameObject gameObject => gameObject.Location,
            _ => Location.Create(area, targetData.TargetPosition, _player.ControlledCreature?.Location?.Rotation ?? 0f)
        };

        NwPlaceable? placeable = NwPlaceable.Create(blueprint.ResRef, location);
        if (placeable == null)
        {
            Token().SetBindValue(View.StatusMessage, $"Failed to create placeable '{blueprint.DisplayName}'.");
            return;
        }

        placeable.Name = blueprint.DisplayName;

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
            $"Spawned '{placeable.Name}'. Use Target Spawn again to place another copy.");
    }

    private void BeginSelectExisting()
    {
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
        NwPlaceable? placeable = targetData.TargetObject as NwPlaceable;
        if (placeable == null)
        {
            NwArea? area = _player.ControlledCreature?.Area;
            if (area == null)
            {
                Token().SetBindValue(View.StatusMessage, "No placeable found.");
                return;
            }

            Location location = Location.Create(area, targetData.TargetPosition, 0f);
            placeable = location.GetNearestObjectsByType<NwPlaceable>().FirstOrDefault();
        }

        if (placeable == null)
        {
            Token().SetBindValue(View.StatusMessage, "No placeable found at that location.");
            return;
        }

        UpdateSelection(placeable);
        Token().SetBindValue(View.StatusMessage, $"Selected '{placeable.Name}'.");
    }

    private void UpdateSelection(NwPlaceable? placeable)
    {
        _lastSelection = placeable;

        if (placeable == null)
        {
            Token().SetBindValue(View.SelectionAvailable, false);
            Token().SetBindValue(View.SelectedName, "No placeable selected");
            Token().SetBindValue(View.SelectedLocation, string.Empty);
            ClearEditFields();
            return;
        }

        Token().SetBindValue(View.SelectionAvailable, true);
        Token().SetBindValue(View.SelectedName, placeable.Name);
        Token().SetBindValue(View.SelectedLocation,
            $"{placeable.Position.X:F2}, {placeable.Position.Y:F2}, {placeable.Position.Z:F2}");
        _pendingOrientation = placeable.Location.Rotation;

        LoadSelectionState(placeable);
    }

    private void RecoverSelectedPlaceable()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            Token().SetBindValue(View.StatusMessage, "No placeable selected to recover.");
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

    private void HandleWatch(ModuleEvents.OnNuiEvent eventData)
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            return;
        }

        string? elementId = eventData.ElementId;
        if (string.IsNullOrWhiteSpace(elementId) || IsBlacklisted(elementId))
        {
            return;
        }

        SyncNumericToString(elementId);

        if (TryHandleNumericTextPair(elementId))
        {
            ApplyPendingData();
            return;
        }

        if (IsNumericSliderBind(elementId))
        {
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

        NwPlaceable placeable = _lastSelection;

        if (!_hasUnsavedChanges)
        {
            Token().SetBindValue(View.StatusMessage, $"No pending changes for '{placeable.Name}'.");
            return;
        }

        Token().SetBindValue(View.StatusMessage, $"Saving '{placeable.Name}'...");
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
        Token().SetBindValue(View.StatusMessage, $"Selected '{placeable.Name}'. Loading persisted state...");

        _ = NwTask.Run(async () =>
        {
            PersistentObject? persisted = null;
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
                        $"Failed to query persistence: {ex.Message}");
                }
                return;
            }

            await NwTask.SwitchToMainThread();

            if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
            {
                return;
            }

            PlaceableData currentData = PlaceableDataFactory.From(placeable);
            _pendingSnapshot = currentData;
            _pendingOrientation = placeable.Location.Rotation;

            PlaceableData savedData = currentData;
            float orientation = placeable.Location.Rotation;

            if (persisted != null)
            {
                PlaceableData? persistedData = await BuildDataFromPersistentObject(persisted);
                await NwTask.SwitchToMainThread();

                if (!Token().Player.IsValid || _lastSelection != placeable || !_lastSelection.IsValid)
                {
                    return;
                }

                if (persistedData is not null)
                {
                    savedData = persistedData;
                    if (persisted.Location is not null && placeable.Area is not null)
                    {
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
                    Token().SetBindValue(View.StatusMessage,
                        $"Selected '{placeable.Name}'. Unable to read persisted snapshot, using current state.");
                }
            }

            _savedSnapshot = savedData;
            _savedOrientation = orientation;
            _hasUnsavedChanges = false;

            PushDataToView(_pendingSnapshot ?? savedData);
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

            Token().SetBindValue(View.TransformX, data.Transform.Translation.X);
            Token().SetBindValue(View.TransformY, data.Transform.Translation.Y);
            Token().SetBindValue(View.TransformZ, data.Transform.Translation.Z);

            Token().SetBindValue(View.RotationX, data.Transform.Rotation.X);
            Token().SetBindValue(View.RotationY, data.Transform.Rotation.Y);
            Token().SetBindValue(View.RotationZ, data.Transform.Rotation.Z);

            Token().SetBindValue(View.Scale, data.Transform.Scale);

            Token().SetBindValue(View.PositionXString,
                data.Position.Position.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.PositionYString,
                data.Position.Position.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.PositionZString,
                data.Position.Position.Z.ToString(CultureInfo.InvariantCulture));

            Token().SetBindValue(View.TransformXString,
                data.Transform.Translation.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.TransformYString,
                data.Transform.Translation.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.TransformZString,
                data.Transform.Translation.Z.ToString(CultureInfo.InvariantCulture));

            Token().SetBindValue(View.RotationXString,
                data.Transform.Rotation.X.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.RotationYString,
                data.Transform.Rotation.Y.ToString(CultureInfo.InvariantCulture));
            Token().SetBindValue(View.RotationZString,
                data.Transform.Rotation.Z.ToString(CultureInfo.InvariantCulture));

            Token().SetBindValue(View.ScaleString, data.Transform.Scale.ToString(CultureInfo.InvariantCulture));
        });
    }

    private void ApplyPendingData()
    {
        if (_lastSelection is null || !_lastSelection.IsValid)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (now - _lastApplyAt < LiveApplyThrottle)
        {
            return;
        }

        _lastApplyAt = now;

        Vector3 position = new(
            Token().GetBindValue(View.PositionX),
            Token().GetBindValue(View.PositionY),
            Token().GetBindValue(View.PositionZ));

        Vector3 translation = new(
            Token().GetBindValue(View.TransformX),
            Token().GetBindValue(View.TransformY),
            Token().GetBindValue(View.TransformZ));

        Vector3 rotation = new(
            Token().GetBindValue(View.RotationX),
            Token().GetBindValue(View.RotationY),
            Token().GetBindValue(View.RotationZ));

        float scale = Token().GetBindValue(View.Scale);

        PlaceableData baseline = _pendingSnapshot ?? PlaceableDataFactory.From(_lastSelection);
        PlaceableData updated = baseline with
        {
            Transform = new PlaceableTransformData(translation, rotation, scale),
            Position = new PlaceableAreaPositionData(position)
        };

        _pendingSnapshot = updated;
        _pendingOrientation = _lastSelection.Location.Rotation;

        ApplyDataToPlaceable(_lastSelection, updated, _pendingOrientation);

        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
            Token().SetBindValue(View.StatusMessage,
                $"Previewing changes to '{_lastSelection.Name}'. Save to persist or Discard to revert.");
        }
    }

    private static void ApplyDataToPlaceable(NwPlaceable placeable, PlaceableData data, float? orientation)
    {
        placeable.VisualTransform.Translation = data.Transform.Translation;
        placeable.VisualTransform.Rotation = data.Transform.Rotation;
        placeable.VisualTransform.Scale = data.Transform.Scale;

        if (orientation.HasValue && placeable.Area is not null)
        {
            placeable.Location = Location.Create(placeable.Area, data.Position.Position, orientation.Value);
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

    private void ToggleBindWatch(bool enable)
    {
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
    }

    private void WithWatchDisabled(Action action)
    {
        bool wasEnabled = _watchersEnabled;
        if (wasEnabled)
        {
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
               elementId.Equals(View.Scale.Key, StringComparison.OrdinalIgnoreCase);
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
            WithWatchBlacklist(new[] { text.Key },
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
                WithWatchBlacklist(new[] { text.Key }, () => Token().SetBindValue(text, sanitized));
            }

            if (float.TryParse(sanitized, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                WithWatchBlacklist(new[] { numeric.Key }, () => Token().SetBindValue(numeric, value));
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
               Handle(View.Scale, View.ScaleString);
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
}
