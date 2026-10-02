using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

// Glyph nodes must be registered in a module to be available for use in the Glyph editor. This is done by implementing the IGlyphModule interface and adding the node executor to the module builder.
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

[GlyphNode(Automatic = false)]
public partial class GetAreaHasNodeTypeExecutor(IQueryDispatcher queries) : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.get_area_has_node_type";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(GlyphNodeInstance node, GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        string areaResRef = (await resolveInput(Inputs.AreaResref)) as string ?? string.Empty;

        List<ResourceNodeInstance> nodes =
            await queries.DispatchAsync<GetNodesForAreaQuery, List<ResourceNodeInstance>>(
                new GetNodesForAreaQuery(areaResRef));

        string nodeTypeToCheck = (await resolveInput(Inputs.NodeType)) as string ?? string.Empty;

        ResourceType? nodeType = nodeTypeToCheck.ToResourceType();
        bool hasNodeType = nodes.Any(n => n.Definition.Type == nodeType);

        return GlyphNodeResult.Data(new Dictionary<string, object?> { ["has_node_type"] = hasNodeType });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Area Has Node Type",
        Category = "Resources",
        Description = "Returns whether an area has a specific node type. " +
                      "For a character's current area, pass nwn.get_resref(nwn.get_area(character)).",
        Source = "GetAreaHasNodeTypeExecutor",
        Backend = "World Engine",
        Parameters =
        [
            Pins.In("area_resref", "Area ResRef", GlyphDataType.String),
            Pins.In("node_type", "Node Type", GlyphDataType.String)
        ],
        Results = [Pins.Out("has_node_type", "Has Node Type", GlyphDataType.Bool)],
        Exports = [new("get_area_has_node_type", "has_node_type")],
    };
}
