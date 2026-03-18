using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the level (hit dice) of a creature by its object ID.
/// </summary>
public class GetCreatureLevelExecutor : IGlyphNodeExecutor
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

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature Level",
        Category = "Getters",
        Description = "Returns the level (hit dice) of a creature given its object ID.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "level", Name = "Level", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
