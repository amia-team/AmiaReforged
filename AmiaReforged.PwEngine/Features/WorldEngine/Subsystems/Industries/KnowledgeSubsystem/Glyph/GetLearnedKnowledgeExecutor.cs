using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that returns all knowledge tags a character has learned across all industries.
/// Outputs the count and a comma-separated list of tags.
/// </summary>
[GlyphNode(Automatic = false)]
public partial class GetLearnedKnowledgeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "knowledge.get_learned";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput("character_id");
        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;

        List<string> tags = [];

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.Knowledge != null)
        {
            tags = context.Knowledge.GetLearnedKnowledgeTags(charGuid);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["knowledge_count"] = tags.Count,
            ["knowledge_tags"] = string.Join(",", tags),
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Learned Knowledge",
        Category = "Industries",
        Description = "Returns all knowledge tags the character has learned across all industries, " +
                      "as a count and a comma-separated list.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ],
        OutputPins =
        [
            new GlyphPin { Id = "knowledge_count", Name = "Knowledge Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "knowledge_tags", Name = "Knowledge Tags", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ]
    };
}
