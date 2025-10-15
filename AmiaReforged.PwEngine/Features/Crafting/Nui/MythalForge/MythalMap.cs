using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

public class MythalMap
{
    public MythalMap(NwPlayer player)
    {
        Map = ItemPropertyHelper.GetMythals(player);
    }

    public Dictionary<CraftingTier, int> Map { get; }
}
