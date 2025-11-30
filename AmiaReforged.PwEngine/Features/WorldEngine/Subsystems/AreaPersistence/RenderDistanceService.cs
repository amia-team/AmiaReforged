using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaPersistence;

[ServiceBinding(typeof(RenderDistanceService))]
public class RenderDistanceService
{
    public const string RenderFarTag = "render_far";

    public RenderDistanceService()
    {
        List<NwPlaceable> plcs = NwObject.FindObjectsOfType<NwPlaceable>().Where(p => p.Tag.Equals(RenderFarTag, StringComparison.InvariantCultureIgnoreCase)).ToList();
        foreach (NwPlaceable nwPlaceable in plcs)
        {
            NWScript.SetObjectVisibleDistance(nwPlaceable, 1000f);
        }
    }
}
