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
        foreach (NwPlaceable nwPlaceable in NwObject.FindObjectsWithTag<NwPlaceable>(RenderFarTag))
        {
            NWScript.SetObjectVisibleDistance(nwPlaceable, 1000f);
        }
    }
}
