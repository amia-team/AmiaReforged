using System;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.PlaceableEditor;

internal enum PlaceablePersistenceMode
{
    None = 0,
    JobSystemOnly = 1,
    All = 2
}

internal static class PlaceablePersistenceModeExtensions
{
    private const string SaveModeVariable = "saved_mode";

    public static PlaceablePersistenceMode GetPlaceablePersistenceMode(this NwArea area)
    {
        LocalVariableInt local = area.GetObjectVariable<LocalVariableInt>(SaveModeVariable);
        if (!local.HasValue)
        {
            return PlaceablePersistenceMode.None;
        }

        int value = local.Value;
        return Enum.IsDefined(typeof(PlaceablePersistenceMode), value)
            ? (PlaceablePersistenceMode)value
            : PlaceablePersistenceMode.None;
    }
}
