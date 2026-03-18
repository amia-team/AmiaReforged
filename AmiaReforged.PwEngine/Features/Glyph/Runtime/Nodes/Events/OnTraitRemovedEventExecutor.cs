using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.OnTraitRemoved"/> graphs.
/// Exposes the character ID, trait tag, and target creature as output pins.
/// </summary>
public class OnTraitRemovedEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "event.on_trait_removed";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        Dictionary<string, object?> outputs = new Dictionary<string, object?>
        {
            ["character_id"] = context.CharacterId ?? string.Empty,
            ["trait_tag"] = context.TraitTag ?? string.Empty,
            ["target_creature"] = context.TargetCreature
        };

        return Task.FromResult(new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "On Trait Removed",
        Category = "Events",
        Description = "Entry point for scripts that run when a trait is removed from a character. " +
                      "Provides the character ID, trait tag, and target creature reference.",
        ColorClass = "node-event",
        Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.OnTraitRemoved,
        ScriptCategory = GlyphScriptCategory.Trait,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "trait_tag", Name = "Trait Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_creature", Name = "Target Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output }
        ]
    };
}
