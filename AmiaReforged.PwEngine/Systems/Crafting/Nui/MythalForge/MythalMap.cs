using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalMap
{
    public MythalMap(NwPlayer player)
    {
        Map = ItemPropertyHelper.GetMythals(player);
    }

    public Dictionary<CraftingTier, int> Map { get; }
}