using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.PlcEdit;

internal sealed class PlcEditorModel(NwPlayer player)
{
    public NwPlaceable? Selected { get; private set; }

    public delegate void OnNewSelectionHandler();

    public event OnNewSelectionHandler? OnNewSelection;

    public void Update(PlaceableData data)
    {
        if (Selected is null) return;
        if (PlaceableDataFactory.From(Selected) == data) return;

        Selected.Name = data.Name;
        Selected.Description = data.Description;
        Selected.PortraitResRef = data.Appearance.PortraitResRef;
        ObjectPlugin.SetAppearance(Selected, data.Appearance.Appearance);
        Selected.VisualTransform.Translation = data.Transform.Translation;
        Selected.VisualTransform.Rotation = data.Transform.Rotation;
        Selected.VisualTransform.Scale = data.Transform.Scale;

        Selected.Position = data.Position.Position;
    }

    public void EnterTargetingMode()
    {
        player.EnterTargetMode(StartPlcSelection,
            new TargetModeSettings
                { ValidTargets = ObjectTypes.Placeable | ObjectTypes.Tile });
    }

    private void StartPlcSelection(ModuleEvents.OnPlayerTarget obj)
    {
        if (player.LoginCreature is null) return;

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

        NwArea? area = player.LoginCreature.Area;
        if (area is null) return;

        Location location = Location.Create(area, obj.TargetPosition, 0);

        NwPlaceable? nwPlaceable = location.GetNearestObjectsByType<NwPlaceable>().FirstOrDefault();

        if (nwPlaceable is null)
        {
            player.SendServerMessage("No placeable found nearby.");
            return;
        }

        Selected = nwPlaceable;
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