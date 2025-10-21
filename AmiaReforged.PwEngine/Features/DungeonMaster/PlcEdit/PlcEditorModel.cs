using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

internal sealed class PlcEditorModel
{
    private readonly NwPlayer _player;

    public PlcEditorModel(NwPlayer player)
    {
        _player = player;

        NwWaypoint? w = NwObject.FindObjectsOfType<NwWaypoint>().FirstOrDefault(w => w.Tag == "ds_copy");

        if (w != null)
        {
            LogManager.GetCurrentClassLogger().Info("Found waypoint");
            _location = w.Location;
        }
    }

    public NwPlaceable? Selected { get; private set; }


    public delegate void OnNewSelectionHandler();

    public event OnNewSelectionHandler? OnNewSelection;

    private Location? _location;
    private int _previous = 0;

    public void Update(PlaceableData data)
    {
        if (Selected is null) return;
        if (PlaceableDataFactory.From(Selected) == data) return;
        _previous = Selected.Appearance.RowIndex;

        Selected.Name = data.Name;
        Selected.Description = data.Description;
        Selected.PortraitResRef = data.Appearance.PortraitResRef;

        PlaceableTableEntry appearance = NwGameTables.PlaceableTable.GetRow(data.Appearance.Appearance);
        Selected.Appearance = appearance;

        Selected.VisualTransform.Translation = data.Transform.Translation;
        Selected.VisualTransform.Rotation = data.Transform.Rotation;
        Selected.VisualTransform.Scale = data.Transform.Scale;

        Selected.Position = data.Position.Position;
    }

    public void EnterTargetingMode()
    {
        _player.EnterTargetMode(StartPlcSelection,
            new TargetModeSettings
                { ValidTargets = ObjectTypes.Placeable | ObjectTypes.Tile });
    }

    private void StartPlcSelection(ModuleEvents.OnPlayerTarget obj)
    {
        if (_player.LoginCreature is null) return;

        if (Selected != null)
        {
            RemoveSelectedVfx();
        }

        if (obj.TargetObject is NwPlaceable placeable)
        {
            Selected = placeable;
            OnNewSelection?.Invoke();

            return;
        }

        NwArea? area = _player.LoginCreature.Area;
        if (area is null) return;

        Location location = Location.Create(area, obj.TargetPosition, 0);

        NwPlaceable? nwPlaceable = location.GetNearestObjectsByType<NwPlaceable>().FirstOrDefault();

        if (nwPlaceable is null)
        {
            _player.SendServerMessage("No placeable found nearby.");
            return;
        }

        Selected = nwPlaceable;
        _previous = Selected.Appearance.RowIndex;

        OnNewSelection?.Invoke();
    }

    private void RemoveSelectedVfx()
    {
        if (Selected is null) return;
        Effect? selectedVfx = Selected.ActiveEffects.FirstOrDefault(e => e.Tag == SelectedVfxTag);

        if (selectedVfx is null) return;

        Selected.RemoveEffect(selectedVfx);
    }

    private const string SelectedVfxTag = "plc_select_vfx";
}
