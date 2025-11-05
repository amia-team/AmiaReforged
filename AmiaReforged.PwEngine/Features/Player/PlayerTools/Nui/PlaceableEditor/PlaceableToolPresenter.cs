using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

public sealed class PlaceableToolPresenter : ScryPresenter<PlaceableToolView>
{
    private readonly NwPlayer _player;
    private readonly PlaceableToolModel _model;
    private List<PlaceableBlueprint> _blueprints = new();

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private PlaceableBlueprint? _pendingSpawn;
    private NwPlaceable? _lastSelection;

    public PlaceableToolPresenter(PlaceableToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
        _model = new PlaceableToolModel(player);
    }

    public override PlaceableToolView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(400f, 100f, 420f, 520f),
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
        }
    }

    public override void Close()
    {
        _blueprints.Clear();
        _pendingSpawn = null;
        _lastSelection = null;
        _token.Close();
    }

    private void InitializeBinds()
    {
        Token().SetBindValue(View.StatusMessage, "Select a blueprint to spawn or pick an existing placeable.");
        Token().SetBindValue(View.SelectionAvailable, false);
        Token().SetBindValue(View.SelectedName, "No placeable selected");
        Token().SetBindValue(View.SelectedLocation, string.Empty);
        Token().SetBindValue(View.BlueprintCount, 0);
        Token().SetBindValues(View.BlueprintNames, Array.Empty<string>());
        Token().SetBindValues(View.BlueprintResRefs, Array.Empty<string>());
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
            return;
        }

        Token().SetBindValue(View.SelectionAvailable, true);
        Token().SetBindValue(View.SelectedName, placeable.Name);
        Token().SetBindValue(View.SelectedLocation,
            $"{placeable.Position.X:F2}, {placeable.Position.Y:F2}, {placeable.Position.Z:F2}");
    }
}
