using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that checks whether a character has unlocked a specific interaction
/// through the knowledge system. A knowledge article with a <c>KnowledgeEffectType.UnlockInteraction</c>
/// effect targeting the given interaction tag grants this unlock.
/// </summary>
public class HasUnlockedInteractionExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "knowledge.has_unlocked_interaction";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput("character_id");
        object? tagValue = await resolveInput("interaction_tag");

        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;
        string interactionTag = tagValue?.ToString() ?? string.Empty;

        bool result = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.WorldEngine != null &&
            !string.IsNullOrEmpty(interactionTag))
        {
            result = context.WorldEngine.HasUnlockedInteraction(charGuid, interactionTag);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result,
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Has Unlocked Interaction",
        Category = "Industries",
        Description = "Returns true if the character has learned knowledge that unlocks the specified " +
                      "interaction tag (via a KnowledgeEffect of type UnlockInteraction).",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Unlocked", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
        ]
    };
}
