﻿using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.NwObjectHelpers;

public static class ItemPropertyValidations
{
    public static bool SameSubtype(ItemProperty p1, ItemProperty p2)
    {
        string p1Label = ItemPropertyHelper.GameLabel(p1);
        string p2Label = ItemPropertyHelper.GameLabel(p2);

        string drPrefix = "Damage Resistance: ";
        string removedPrefix1 = p1Label.Replace(drPrefix, "");
        string removedPrefix2 = p2Label.Replace(drPrefix, "");
                
        string[] split1 = removedPrefix1.Split(" ");
        string[] split2 = removedPrefix2.Split(" ");
                
        return split1[0] == split2[0];
    }
}