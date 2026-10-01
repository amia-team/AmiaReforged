using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the level (hit dice) of a creature by its object ID.
/// </summary>
[GlyphNode]
public partial class GetCreatureLevelExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_level";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? creatureVal = await resolveInput("creature");
        uint creatureId = Convert.ToUInt32(creatureVal ?? 0);

        int level = 0;
        NwCreature? creature = creatureId.ToNwObject<NwCreature>();

        if (creature != null)
        {
            level = creature.Level;
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["level"] = level
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, DisplayName = "GetCreatureLevelExecutor", Category = "NWN / Compatibility",
        Description = "NWScript GetHitDice with the established runtime pin contract.",
        Source = "NWScript.GetHitDice", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("creature", "Creature")], Results = [Pins.Out("level", "Level", GlyphDataType.Int)],
        Exports = [new("nwn.get_hit_dice", "level")]
    };
}
