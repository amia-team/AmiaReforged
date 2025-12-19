using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

internal sealed class PlaceableToolModel
{
    /// <summary>
    /// Tag applied to the selection VFX effect on placeables being edited.
    /// Used to identify and remove the effect when deselecting or restoring from persistence.
    /// </summary>
    public const string SelectionVfxTag = "plc_selection_vfx";

    private readonly NwPlayer _player;

    public PlaceableToolModel(NwPlayer player)
    {
        _player = player;
    }

    public IReadOnlyList<PlaceableBlueprint> CollectBlueprints()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature == null)
        {
            return Array.Empty<PlaceableBlueprint>();
        }

        List<PlaceableBlueprint> results = new();

        foreach (NwItem item in creature.Inventory.Items)
        {
            LocalVariableInt marker = item.GetObjectVariable<LocalVariableInt>("is_plc");
            if (!marker.HasValue || marker.Value <= 0)
            {
                continue;
            }

            LocalVariableString resrefVar = item.GetObjectVariable<LocalVariableString>("plc_resref");
            if (!resrefVar.HasValue || string.IsNullOrWhiteSpace(resrefVar.Value))
            {
                continue;
            }
            LocalVariableInt healthOverrideVar = item.GetObjectVariable<LocalVariableInt>("health_override");

            string displayName = item.Name;
            int appearance = ResolveAppearance(item);
            bool isPlot = item.GetObjectVariable<LocalVariableBool>("is_plot").HasValue && item.GetObjectVariable<LocalVariableBool>("is_plot").Value;
            bool isStatic = item.GetObjectVariable<LocalVariableBool>("is_static").HasValue && item.GetObjectVariable<LocalVariableBool>("is_static").Value;

            // Serialize item data now while the item reference is still valid
            byte[]? serializedItemData = item.Serialize();

            results.Add(new PlaceableBlueprint(item, resrefVar.Value!, displayName, appearance, serializedItemData, healthOverrideVar, isPlot, isStatic));
        }

        return results
            .OrderBy(bp => bp.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(bp => bp.ResRef, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveDisplayName(NwItem item)
    {
        LocalVariableString customName = item.GetObjectVariable<LocalVariableString>("plc_name");
        if (customName.HasValue && !string.IsNullOrWhiteSpace(customName.Value))
        {
            return customName.Value!;
        }

        return item.Name;
    }

    private static int ResolveAppearance(NwItem item)
    {
        LocalVariableInt appearanceVar = item.GetObjectVariable<LocalVariableInt>("plc_appearance");
        if (appearanceVar.HasValue && appearanceVar.Value >= 0)
        {
            return appearanceVar.Value;
        }

        return 0;
    }
}
