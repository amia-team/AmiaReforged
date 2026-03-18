using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that returns a character's proficiency level in a specific industry.
/// Outputs the level name (e.g. "Expert"), the numeric ordinal, and whether the character
/// is a member of that industry at all.
/// </summary>
public class GetIndustryLevelExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "industry.get_level";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput("character_id");
        object? tagValue = await resolveInput("industry_tag");

        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;
        string industryTag = tagValue?.ToString() ?? string.Empty;

        string levelName = string.Empty;
        int levelValue = -1;
        bool isMember = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.WorldEngine != null &&
            !string.IsNullOrEmpty(industryTag))
        {
            var level = context.WorldEngine.GetIndustryLevel(charGuid, industryTag);
            if (level != null)
            {
                isMember = true;
                levelName = level.Value.ToString();
                levelValue = (int)level.Value;
            }
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["level"] = levelName,
            ["level_value"] = levelValue,
            ["is_member"] = isMember,
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Industry Level",
        Category = "Industries",
        Description = "Returns the character's proficiency level in a specific industry. " +
                      "Outputs the level name, numeric value, and whether they are a member.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "industry_tag", Name = "Industry Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ],
        OutputPins =
        [
            new GlyphPin { Id = "level", Name = "Level", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "level_value", Name = "Level Value", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "is_member", Name = "Is Member", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
        ]
    };
}
