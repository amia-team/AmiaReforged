using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Flow-control node that performs a skill check (skill rank + d20 vs DC) and branches
/// on the result. Outputs the raw roll and total for downstream use.
/// Uses the NWN engine's skill rank system.
/// </summary>
public class SkillCheckExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.skill_check";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? skillValue = await resolveInput("skill");
        object? dcValue = await resolveInput("dc");

        uint creature = Convert.ToUInt32(creatureValue);
        string skillName = skillValue?.ToString() ?? "Lore";
        int dc = Convert.ToInt32(dcValue);

        int skillId = ResolveSkillId(skillName);
        int rank = 0;
        int roll = 0;

        if (creature != NWScript.OBJECT_INVALID && skillId >= 0)
        {
            rank = NWScript.GetSkillRank(skillId, creature);
            // d20 roll
            roll = NWScript.d20();
        }

        int total = rank + roll;
        bool success = total >= dc;

        return new GlyphNodeResult
        {
            NextExecPinId = success ? "success" : "failure",
            OutputValues = new Dictionary<string, object?>
            {
                ["roll"] = roll,
                ["total"] = total,
                ["rank"] = rank
            }
        };
    }

    /// <summary>
    /// Maps a human-readable skill name to the NWN SKILL_* constant.
    /// </summary>
    private static int ResolveSkillId(string skillName) => skillName.ToUpperInvariant() switch
    {
        "ANIMAL_EMPATHY" or "ANIMAL EMPATHY" => NWScript.SKILL_ANIMAL_EMPATHY,
        "APPRAISE" => NWScript.SKILL_APPRAISE,
        "BLUFF" => NWScript.SKILL_BLUFF,
        "CONCENTRATION" => NWScript.SKILL_CONCENTRATION,
        "CRAFT_ARMOR" or "CRAFT ARMOR" => NWScript.SKILL_CRAFT_ARMOR,
        "CRAFT_TRAP" or "CRAFT TRAP" => NWScript.SKILL_CRAFT_TRAP,
        "CRAFT_WEAPON" or "CRAFT WEAPON" => NWScript.SKILL_CRAFT_WEAPON,
        "DISABLE_TRAP" or "DISABLE TRAP" => NWScript.SKILL_DISABLE_TRAP,
        "DISCIPLINE" => NWScript.SKILL_DISCIPLINE,
        "HEAL" => NWScript.SKILL_HEAL,
        "HIDE" => NWScript.SKILL_HIDE,
        "INTIMIDATE" => NWScript.SKILL_INTIMIDATE,
        "LISTEN" => NWScript.SKILL_LISTEN,
        "LORE" => NWScript.SKILL_LORE,
        "MOVE_SILENTLY" or "MOVE SILENTLY" => NWScript.SKILL_MOVE_SILENTLY,
        "OPEN_LOCK" or "OPEN LOCK" => NWScript.SKILL_OPEN_LOCK,
        "PARRY" => NWScript.SKILL_PARRY,
        "PERFORM" => NWScript.SKILL_PERFORM,
        "PERSUADE" => NWScript.SKILL_PERSUADE,
        "PICK_POCKET" or "PICK POCKET" => NWScript.SKILL_PICK_POCKET,
        "RIDE" => NWScript.SKILL_RIDE,
        "SEARCH" => NWScript.SKILL_SEARCH,
        "SET_TRAP" or "SET TRAP" => NWScript.SKILL_SET_TRAP,
        "SPELLCRAFT" => NWScript.SKILL_SPELLCRAFT,
        "SPOT" => NWScript.SKILL_SPOT,
        "TAUNT" => NWScript.SKILL_TAUNT,
        "TUMBLE" => NWScript.SKILL_TUMBLE,
        "USE_MAGIC_DEVICE" or "USE MAGIC DEVICE" => NWScript.SKILL_USE_MAGIC_DEVICE,
        _ => -1
    };

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Skill Check",
        Category = "Interactions",
        Description = "Performs a skill check (rank + d20 vs DC) and branches on the result. " +
                      "Outputs the roll, rank, and total for downstream use. " +
                      "Skill names: Persuade, Intimidate, Lore, Heal, Bluff, Spot, Listen, Search, etc.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "skill", Name = "Skill", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "Lore" },
            new GlyphPin { Id = "dc", Name = "DC", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "15" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "success", Name = "Success", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "failure", Name = "Failure", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "roll", Name = "Roll (d20)", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "total", Name = "Total", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "rank", Name = "Rank", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
