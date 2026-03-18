using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that returns a character's knowledge point progression snapshot.
/// Outputs total KP, economy-earned KP, level-up KP, and accumulated progression points.
/// </summary>
public class GetKnowledgeProgressionExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "knowledge.get_progression";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput("character_id");
        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;

        int totalKp = 0;
        int economyKp = 0;
        int levelUpKp = 0;
        int accumulated = 0;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.WorldEngine != null)
        {
            KnowledgeProgressionInfo info = context.WorldEngine.GetKnowledgeProgression(charGuid);
            totalKp = info.TotalKp;
            economyKp = info.EconomyKp;
            levelUpKp = info.LevelUpKp;
            accumulated = info.AccumulatedProgressionPoints;
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["total_kp"] = totalKp,
            ["economy_kp"] = economyKp,
            ["levelup_kp"] = levelUpKp,
            ["accumulated_points"] = accumulated,
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Knowledge Progression",
        Category = "Industries",
        Description = "Returns the character's knowledge point progression: total KP, " +
                      "economy-earned KP, level-up KP, and accumulated progression points.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ],
        OutputPins =
        [
            new GlyphPin { Id = "total_kp", Name = "Total KP", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "economy_kp", Name = "Economy KP", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "levelup_kp", Name = "Level-Up KP", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "accumulated_points", Name = "Accumulated Points", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
        ]
    };
}
