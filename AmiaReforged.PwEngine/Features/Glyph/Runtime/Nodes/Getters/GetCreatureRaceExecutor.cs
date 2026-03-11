using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the racial type of a creature as both an integer ID and display string. Pure data node.
/// </summary>
public class GetCreatureRaceExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_race";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        uint creature = Convert.ToUInt32(creatureValue);

        int raceId = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetRacialType(creature)
            : -1;

        string raceName = raceId switch
        {
            NWScript.RACIAL_TYPE_DWARF => "Dwarf",
            NWScript.RACIAL_TYPE_ELF => "Elf",
            NWScript.RACIAL_TYPE_GNOME => "Gnome",
            NWScript.RACIAL_TYPE_HALFELF => "Half-Elf",
            NWScript.RACIAL_TYPE_HALFLING => "Halfling",
            NWScript.RACIAL_TYPE_HALFORC => "Half-Orc",
            NWScript.RACIAL_TYPE_HUMAN => "Human",
            NWScript.RACIAL_TYPE_ABERRATION => "Aberration",
            NWScript.RACIAL_TYPE_ANIMAL => "Animal",
            NWScript.RACIAL_TYPE_BEAST => "Beast",
            NWScript.RACIAL_TYPE_CONSTRUCT => "Construct",
            NWScript.RACIAL_TYPE_DRAGON => "Dragon",
            NWScript.RACIAL_TYPE_ELEMENTAL => "Elemental",
            NWScript.RACIAL_TYPE_FEY => "Fey",
            NWScript.RACIAL_TYPE_GIANT => "Giant",
            NWScript.RACIAL_TYPE_HUMANOID_GOBLINOID => "Goblinoid",
            NWScript.RACIAL_TYPE_HUMANOID_MONSTROUS => "Monstrous Humanoid",
            NWScript.RACIAL_TYPE_HUMANOID_ORC => "Orc",
            NWScript.RACIAL_TYPE_HUMANOID_REPTILIAN => "Reptilian",
            NWScript.RACIAL_TYPE_MAGICAL_BEAST => "Magical Beast",
            NWScript.RACIAL_TYPE_OUTSIDER => "Outsider",
            NWScript.RACIAL_TYPE_SHAPECHANGER => "Shapechanger",
            NWScript.RACIAL_TYPE_UNDEAD => "Undead",
            NWScript.RACIAL_TYPE_VERMIN => "Vermin",
            _ => "Unknown"
        };

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["race_id"] = raceId,
            ["race_name"] = raceName
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature Race",
        Category = "Getters",
        Description = "Returns the racial type ID and name of a creature.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "race_id", Name = "Race ID", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "race_name", Name = "Race Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
