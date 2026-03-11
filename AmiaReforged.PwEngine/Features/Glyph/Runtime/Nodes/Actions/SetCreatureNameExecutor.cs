using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Renames a creature by calling NWScript.SetName. Works in any encounter event.
/// </summary>
public class SetCreatureNameExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.set_creature_name";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? nameValue = await resolveInput("name");

        uint creature = Convert.ToUInt32(creatureValue);
        string name = nameValue?.ToString() ?? string.Empty;

        if (creature != NWScript.OBJECT_INVALID && !string.IsNullOrEmpty(name))
        {
            NWScript.SetName(creature, name);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Set Creature Name",
        Category = "Actions",
        Description = "Changes the display name of a creature.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "name", Name = "Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
