using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

public static class ItemModelValidation
{
    public static bool IsValidModelIndex(NwItem item, int modelIndex)
    {
        if (!item.IsValid) return false;
        if (modelIndex < 0) return false;

        string itemClass = item.BaseItem.ItemClass;

        if (string.IsNullOrEmpty(itemClass)) return false;

        uint baseItemId = item.BaseItem.Id;
        bool usesMdlWithoutPrefix = item.BaseItem.ItemType == BaseItemType.SmallShield ||
                                     item.BaseItem.ItemType == BaseItemType.LargeShield ||
                                     item.BaseItem.ItemType == BaseItemType.TowerShield ||
                                     baseItemId == 213 ||
                                     baseItemId == 214 ||
                                     baseItemId == 215;

        string modelResRef;
        int resType;

        if (usesMdlWithoutPrefix)
        {
            modelResRef = $"{itemClass}_{modelIndex:D3}";
            resType = NWScript.RESTYPE_MDL;
        }
        else
        {
            modelResRef = $"i{itemClass}_{modelIndex:D3}";
            resType = NWScript.RESTYPE_TGA;
        }

        string alias = NWScript.ResManGetAliasFor(modelResRef, resType);
        return !string.IsNullOrEmpty(alias);
    }

    public static int GetMaxModelIndex(NwItem item)
    {
        if (!item.IsValid) return 0;
        return (int)item.BaseItem.ModelRangeMax;
    }

    public static IEnumerable<int> GetValidIndices(NwItem item)
    {
        if (!item.IsValid)
        {
            return [];
        }

        int maxModel = GetMaxModelIndex(item);
        List<int> validIndices = new List<int>();

        for (int i = 1; i <= maxModel; i++)
        {
            if (IsValidModelIndex(item, i))
            {
                validIndices.Add(i);
            }
        }

        return validIndices;
    }

    public static bool SupportsModelChanges(NwItem item)
    {
        if (!item.IsValid) return false;

        if (item.BaseItem.ModelType != BaseItemModelType.Simple)
        {
            return false;
        }

        string itemClass = item.BaseItem.ItemClass;
        if (string.IsNullOrEmpty(itemClass)) return false;

        int maxModel = (int)item.BaseItem.ModelRangeMax;
        return maxModel > 0;
    }
}
