using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the list of party member object IDs in the encounter area.
/// </summary>
public class GetPartyMembersExecutor : IGlyphNodeExecutor
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

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Party Members",
        Category = "Getters",
        Description = "Returns a list of player character object IDs in the encounter area, and their count.",
        ColorClass = "node-getter",
        ScriptCategory = GlyphScriptCategory.Encounter,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "members", Name = "Members", DataType = GlyphDataType.List, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "count", Name = "Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
