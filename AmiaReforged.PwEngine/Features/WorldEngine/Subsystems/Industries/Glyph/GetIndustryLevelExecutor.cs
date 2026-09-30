using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that returns a character's proficiency level in a specific industry.
/// Outputs the level name (e.g. "Expert"), the numeric ordinal, and whether the character
/// is a member of that industry at all.
/// </summary>
[GlyphNode(Automatic = false)]
public partial class GetIndustryLevelExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "industry.get_level";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput(Inputs.CharacterId);
        object? tagValue = await resolveInput(Inputs.IndustryTag);

        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;
        string industryTag = tagValue?.ToString() ?? string.Empty;

        string levelName = string.Empty;
        int levelValue = -1;
        bool isMember = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.Industries != null &&
            !string.IsNullOrEmpty(industryTag))
        {
            var level = context.Industries.GetIndustryLevel(charGuid, industryTag);
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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("industry.level", "level_value", null)
        ],
        DisplayName = "Get Industry Level",
        Category = "Industries",
        Description = "Returns the character's proficiency level in a specific industry. " +
                      "Outputs the level name, numeric value, and whether they are a member.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("character_id", "Character ID", GlyphDataType.String),
            Pins.In("industry_tag", "Industry Tag", GlyphDataType.String),
        ],
        Results =
        [
            Pins.Out("level", "Level", GlyphDataType.String),
            Pins.Out("level_value", "Level Value", GlyphDataType.Int),
            Pins.Out("is_member", "Is Member", GlyphDataType.Bool),
        ]
    };
}
