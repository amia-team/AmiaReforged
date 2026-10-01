using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the racial type of a creature as both an integer ID and display string. Pure data node.
/// </summary>
[GlyphNode]
public partial class GetCreatureRaceExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_race";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        uint creature = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(creatureValue));

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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, DisplayName = "GetCreatureRaceExecutor", Category = "NWN / Compatibility",
        Description = "NWScript GetRacialType with the established runtime pin contract.",
        Source = "NWScript.GetRacialType", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("creature", "Creature")], Results = [Pins.Out("race_id", "Race", GlyphDataType.Int), Pins.Out("race_name", "Race name", GlyphDataType.String)],
        Exports = [new("nwn.get_racial_type", "race_id")]
    };
}
