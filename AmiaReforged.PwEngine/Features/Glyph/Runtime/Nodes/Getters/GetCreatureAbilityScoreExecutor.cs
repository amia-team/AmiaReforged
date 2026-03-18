using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets an ability score and modifier for a creature. Takes a string input for the ability
/// (STR, DEX, CON, INT, WIS, CHA). Pure data node.
/// </summary>
public class GetCreatureAbilityScoreExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_ability_score";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? abilityValue = await resolveInput("ability");

        uint creature = Convert.ToUInt32(creatureValue);
        string abilityStr = abilityValue?.ToString()?.ToUpperInvariant() ?? "STR";

        int abilityType = abilityStr switch
        {
            "STR" => NWScript.ABILITY_STRENGTH,
            "DEX" => NWScript.ABILITY_DEXTERITY,
            "CON" => NWScript.ABILITY_CONSTITUTION,
            "INT" => NWScript.ABILITY_INTELLIGENCE,
            "WIS" => NWScript.ABILITY_WISDOM,
            "CHA" => NWScript.ABILITY_CHARISMA,
            _ => NWScript.ABILITY_STRENGTH
        };

        int score = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetAbilityScore(creature, abilityType)
            : 0;

        int modifier = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetAbilityModifier(abilityType, creature)
            : 0;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["score"] = score,
            ["modifier"] = modifier
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Ability Score",
        Category = "Getters",
        Description = "Returns the ability score and modifier for a creature. " +
                      "Ability input: STR, DEX, CON, INT, WIS, or CHA.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "ability", Name = "Ability", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "STR" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "score", Name = "Score", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "modifier", Name = "Modifier", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
