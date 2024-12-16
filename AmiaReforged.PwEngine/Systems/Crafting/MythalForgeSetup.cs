using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(MythalForgeSetup))]
public class MythalForgeSetup
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<CraftingTier, List<MythalProperty>> _properties = new();

    private readonly NuiManager windowManager;

    public MythalForgeSetup(NuiManager windowManager)
    {
        this.windowManager = windowManager;

        InitForges();
    }

    private void InitForges()
    {
        IEnumerable<NwPlaceable> forges = NwObject.FindObjectsWithTag<NwPlaceable>("mythal_forge");

        foreach (NwPlaceable nwPlaceable in forges)
        {
            nwPlaceable.OnUsed += OpenForge;
        }
    }

    private void OpenForge(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (windowManager.WindowIsOpen(player, typeof(MythalForgeController)))
            return;
        
        windowManager.OpenWindow<MythalForgeView, MythalForgeController>(player);
    }
}