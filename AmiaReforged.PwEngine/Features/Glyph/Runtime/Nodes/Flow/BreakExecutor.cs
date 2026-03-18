using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// Break node — exits the innermost enclosing loop (e.g., ForEach) immediately.
/// When the interpreter encounters the <see cref="GlyphNodeResult.IsBreak"/> signal,
/// it unwinds the execution stack to the nearest loop frame, cleans up iteration state,
/// and resumes execution from the loop's "completed" output pin.
/// <para>
/// If no enclosing loop exists, the break signal terminates the current execution branch
/// (equivalent to a dead-end).
/// </para>
/// </summary>
public class BreakExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "flow.break";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        return Task.FromResult(GlyphNodeResult.Break());
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Break",
        Category = "Flow Control",
        Description = "Exits the innermost enclosing loop immediately. " +
                      "Execution continues from the loop's Completed output pin.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            new GlyphPin
            {
                Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec,
                Direction = GlyphPinDirection.Input,
            },
        ],
        OutputPins = [],
    };
}
