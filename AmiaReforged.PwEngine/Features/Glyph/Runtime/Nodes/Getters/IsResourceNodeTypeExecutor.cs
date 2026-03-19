using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Checks whether a game object is a resource node of a specific type.
/// The resource type is selected from a dropdown in the node's inspector.
/// <para>
/// Resolves the target object's tag and looks it up in the resource node definition repository
/// via <see cref="IGlyphWorldEngineApi.GetResourceNodeType"/>. If the definition's
/// <c>ResourceType</c> matches the selected type, the <c>result</c> output is <c>true</c>.
/// </para>
/// </summary>
public class IsResourceNodeTypeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.is_resource_node_type";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? targetVal = await resolveInput("target");
        uint targetHandle = Convert.ToUInt32(targetVal ?? 0);

        string selectedType = node.PropertyOverrides.TryGetValue("resource_type", out string? typeOverride)
            ? typeOverride
            : "Ore";

        bool isMatch = false;

        if (targetHandle != 0 && targetHandle != 0x7F000000 && context.WorldEngine != null)
        {
            string? actualType = context.WorldEngine.GetResourceNodeType(targetHandle);
            isMatch = actualType != null &&
                      string.Equals(actualType, selectedType, StringComparison.OrdinalIgnoreCase);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = isMatch
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Is Resource Node Type",
        Category = "Getters",
        Description = "Checks whether a game object is a resource node of the selected type. " +
                      "Returns true if the object's resource definition matches.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Properties =
        [
            new GlyphPropertyDefinition
            {
                Id = "resource_type",
                DisplayName = "Resource Type",
                DefaultValue = "Ore",
                AllowedValues = ["Ore", "Geode", "Boulder", "Tree", "Flora"]
            }
        ],
        InputPins =
        [
            new GlyphPin
            {
                Id = "target", Name = "Target", DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Input
            }
        ],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "result", Name = "Result", DataType = GlyphDataType.Bool,
                Direction = GlyphPinDirection.Output
            }
        ]
    };
}
