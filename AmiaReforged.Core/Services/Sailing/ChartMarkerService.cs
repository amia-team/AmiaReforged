using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;
 
[ServiceBinding(typeof(ChartMarkerService))]
public sealed class ChartMarkerService
{
    private readonly List<ChartMarker> markers =
   [
     /*   new()
        {
            AreaResRef = "ocean_01",
            Sprite = "chart_port",
            X = 50f,
            Y = 20f,
            Size = 22f
        },

        new()
        {
            AreaResRef = "ocean_01",
            Sprite = "chart_dock",
            X = 120f,
            Y = 90f
        }*/
    ];

    public IEnumerable<ChartMarker> GetMarkers(string areaResRef)
    {
        return markers.Where(m =>
            m.AreaResRef.Equals(
                areaResRef,
                StringComparison.OrdinalIgnoreCase));
    }
}