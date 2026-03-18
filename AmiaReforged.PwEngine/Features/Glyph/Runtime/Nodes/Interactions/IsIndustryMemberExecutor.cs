using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that checks whether a character is enrolled in a specific industry.
/// Returns a single boolean output.
/// </summary>
public class IsIndustryMemberExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "industry.is_member";

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

        bool result = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.WorldEngine != null &&
            !string.IsNullOrEmpty(industryTag))
        {
            result = context.WorldEngine.IsIndustryMember(charGuid, industryTag);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result,
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Is Industry Member",
        Category = "Industries",
        Description = "Returns true if the character is enrolled in the specified industry.",
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
            new GlyphPin { Id = "result", Name = "Is Member", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
        ]
    };
}
