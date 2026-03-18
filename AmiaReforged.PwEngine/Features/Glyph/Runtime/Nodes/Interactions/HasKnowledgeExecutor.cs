using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that checks whether a character has learned a specific knowledge article.
/// Returns a single boolean output.
/// </summary>
public class HasKnowledgeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "knowledge.has";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput("character_id");
        object? tagValue = await resolveInput("knowledge_tag");

        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;
        string knowledgeTag = tagValue?.ToString() ?? string.Empty;

        bool result = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.WorldEngine != null &&
            !string.IsNullOrEmpty(knowledgeTag))
        {
            result = context.WorldEngine.HasKnowledge(charGuid, knowledgeTag);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result,
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Has Knowledge",
        Category = "Industries",
        Description = "Returns true if the character has learned the specified knowledge article.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "knowledge_tag", Name = "Knowledge Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Has Knowledge", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
        ]
    };
}
