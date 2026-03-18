using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that returns all industry memberships for a character.
/// Outputs the total count, a comma-separated list of industry tags,
/// and the first industry's tag and proficiency level for convenience.
/// </summary>
public class GetIndustryMembershipsExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "industry.get_memberships";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? charIdValue = await resolveInput("character_id");
        string charIdStr = charIdValue?.ToString() ?? context.CharacterId ?? string.Empty;

        List<IndustryMembershipInfo> memberships = [];

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.WorldEngine != null)
        {
            memberships = context.WorldEngine.GetIndustryMemberships(charGuid);
        }

        string tags = string.Join(",", memberships.Select(m => m.Tag));
        IndustryMembershipInfo? primary = memberships.FirstOrDefault();

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["membership_count"] = memberships.Count,
            ["industry_tags"] = tags,
            ["primary_industry"] = primary?.Tag ?? string.Empty,
            ["primary_level"] = primary?.Level.ToString() ?? string.Empty,
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Industry Memberships",
        Category = "Industries",
        Description = "Returns all industry memberships for a character, including count, tags, " +
                      "and the primary (first) industry's tag and proficiency level.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
        ],
        OutputPins =
        [
            new GlyphPin { Id = "membership_count", Name = "Membership Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "industry_tags", Name = "Industry Tags", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "primary_industry", Name = "Primary Industry", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "primary_level", Name = "Primary Level", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
        ]
    };
}
