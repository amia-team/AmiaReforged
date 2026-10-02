using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

[GlyphNode(Automatic = false)]
public partial class GetRegionExecutor(IQueryDispatcher queries) : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.get_region";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        string areaResRef = (await resolveInput(Inputs.AreaResref)) as string ?? string.Empty;
        string region = string.IsNullOrWhiteSpace(areaResRef)
            ? string.Empty
            : await queries.DispatchAsync<GetRegionTagForAreaQuery, string?>(
                new(areaResRef), context.CancellationToken) ?? string.Empty;

        return GlyphNodeResult.Data(new() { ["region"] = region });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Region",
        Category = "Regions",
        Description = "Returns the region tag containing an area ResRef, or an empty string if the area is unregistered. " +
                      "For a character's current region, pass nwn.get_resref(nwn.get_area(character)).",
        Source = "GetRegionTagForAreaQuery",
        Backend = "World Engine",
        Parameters = [Pins.In("area_resref", "Area ResRef", GlyphDataType.String)],
        Results = [Pins.Out("region", "Region", GlyphDataType.String)],
        Exports = [new("get_region", "region")],
    };
}
