using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ChartLandmarkService))]
public sealed class ChartLandmarkService
{
    private readonly List<ChartLandmark> _landmarks =
    [
     /*   new ChartLandmark
        {
            AreaResRef = "ocean_01",
            Sprite = "sailing_amia",

            // Tune these once in-game.
            X = 70f,
            Y = 75f,

            Width = 60f,
            Height = 60f
        }*/
    ];

    public IEnumerable<ChartLandmark> GetLandmarks(
        string areaResRef)
    {
        return _landmarks.Where(
            l => string.Equals(
                l.AreaResRef,
                areaResRef,
                StringComparison.OrdinalIgnoreCase));
    }
}