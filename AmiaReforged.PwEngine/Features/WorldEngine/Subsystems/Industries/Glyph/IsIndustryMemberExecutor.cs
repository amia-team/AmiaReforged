using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that checks whether a character is enrolled in a specific industry.
/// Returns a single boolean output.
/// </summary>
[GlyphNode(Automatic = false)]
public partial class IsIndustryMemberExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "industry.is_member";

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

        bool result = false;

        if (Guid.TryParse(charIdStr, out Guid charGuid) && context.Industries != null &&
            !string.IsNullOrEmpty(industryTag))
        {
            result = context.Industries.IsIndustryMember(charGuid, industryTag);
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
            new("industry.is_member", "result", null)
        ],
        DisplayName = "Is Industry Member",
        Category = "Industries",
        Description = "Returns true if the character is enrolled in the specified industry.",
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
            Pins.Out("result", "Is Member", GlyphDataType.Bool),
        ]
    };
}
