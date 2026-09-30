using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that checks whether a character has learned a specific knowledge article.
/// Returns a single boolean output.
/// </summary>
[GlyphNode(Automatic = false)]
public partial class HasKnowledgeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "knowledge.has";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput(Inputs.CharacterId);
        object? tagValue = await resolveInput(Inputs.KnowledgeTag);

        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;
        string knowledgeTag = tagValue?.ToString() ?? string.Empty;

        bool result = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.Knowledge != null &&
            !string.IsNullOrEmpty(knowledgeTag))
        {
            result = context.Knowledge.HasKnowledge(charGuid, knowledgeTag);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result,
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("has_knowledge", "result", null, CallAliases: [new("player.has_knowledge", "has_knowledge", "context.character_id")])
        ],
        DisplayName = "Has Knowledge",
        Category = "Industries",
        Description = "Returns true if the character has learned the specified knowledge article.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("character_id", "Character ID", GlyphDataType.String),
            Pins.In("knowledge_tag", "Knowledge Tag", GlyphDataType.String),
        ],
        Results =
        [
            Pins.Out("result", "Has Knowledge", GlyphDataType.Bool),
        ]
    };
}
