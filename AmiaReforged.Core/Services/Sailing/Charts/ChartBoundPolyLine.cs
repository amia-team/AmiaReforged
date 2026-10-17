using Anvil.API;
using Newtonsoft.Json;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>The native NUI polyline accepts bound points. Anvil 8193.37.3's sealed
/// convenience class exposes only a static List, so retain its wire format with a points bind.</summary>
public sealed class ChartBoundPolyLine : NuiDrawListItem
{
    public override NuiDrawListItemType Type => NuiDrawListItemType.PolyLine;
    [JsonProperty("points")] public NuiBind<List<float>> Points { get; }

    public ChartBoundPolyLine(Color color, bool fill, float thickness, string points)
    { Color = color; Fill = fill; LineThickness = thickness; Points = new(points); }
}
