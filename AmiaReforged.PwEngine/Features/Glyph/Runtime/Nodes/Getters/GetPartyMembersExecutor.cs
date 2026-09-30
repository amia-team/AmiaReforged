using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the list of party member object IDs in the encounter area.
/// </summary>
[GlyphNode]
public partial class GetPartyMembersExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.party_members";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        List<uint> memberIds = [];

        NwArea? area = context.EncounterContext?.Area;
        if (area != null)
        {
            foreach (NwCreature creature in area.Objects.OfType<NwCreature>())
            {
                if (creature.IsPlayerControlled || creature.IsLoginPlayerCharacter)
                {
                    memberIds.Add(creature.ObjectId);
                }
            }
        }

        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["members"] = memberIds,
            ["count"] = memberIds.Count
        }));
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("party.members", "members", null, PropertyAliases: [new("party.members", "party.members", null)])
        ],
        DisplayName = "Get Party Members",
        Category = "Getters",
        Description = "Returns a list of player character object IDs in the encounter area, and their count.",
        ColorClass = "node-getter",
        ScriptCategory = GlyphScriptCategory.Encounter,
        Parameters = [],
        Results =
        [
            Pins.Out("members", "Members", GlyphDataType.List),
            Pins.Out("count", "Count", GlyphDataType.Int)
        ]
    };
}
