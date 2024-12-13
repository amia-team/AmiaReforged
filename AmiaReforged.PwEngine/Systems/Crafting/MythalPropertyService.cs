using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting;

// [ServiceBinding(typeof(MythalPropertyService))]
public class MythalPropertyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<CraftingTier, List<MythalProperty>> _properties = new();

    public MythalPropertyService()
    {

    }


}