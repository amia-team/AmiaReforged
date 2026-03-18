using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// ForEach node — iterates over a list, executing the loop body once per element.
/// Exposes the current element and index as output data pins during each iteration.
/// After the loop completes, execution continues via the "completed" Exec output.
/// <para>
/// The executor manages its own iteration state via <see cref="GlyphExecutionContext.Variables"/>,
/// keyed by the node instance ID. On each execution:
/// <list type="bullet">
/// <item>First call: stores the list and sets index to 0, returns <see cref="GlyphNodeResult.LoopBody"/>.</item>
/// <item>Subsequent calls (re-execution by the interpreter after loop body completes): advances the index.
///   If more elements remain, returns <see cref="GlyphNodeResult.LoopBody"/>. Otherwise returns
///   <see cref="GlyphNodeResult.Continue"/> with "completed".</item>
/// </list>
/// </para>
/// </summary>
public class ForEachExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "flow.for_each";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        string listKey = $"__foreach_{node.InstanceId}_list";
        string indexKey = $"__foreach_{node.InstanceId}_index";

        // Check if this is a re-entry (loop continuation) or first call
        if (context.Variables.TryGetValue(listKey, out object? existingList) &&
            context.Variables.TryGetValue(indexKey, out object? existingIndex))
        {
            // Re-entry: advance to next iteration
            IList<object?> items = (IList<object?>)existingList!;
            int nextIndex = Convert.ToInt32(existingIndex) + 1;

            if (nextIndex >= items.Count)
            {
                // Loop complete — clean up and signal completion
                context.Variables.Remove(listKey);
                context.Variables.Remove(indexKey);
                return GlyphNodeResult.Continue("completed");
            }

            // Store updated index
            context.Variables[indexKey] = nextIndex;

            return GlyphNodeResult.LoopBody("loop_body", new Dictionary<string, object?>
            {
                ["element"] = items[nextIndex],
                ["index"] = nextIndex,
                ["count"] = items.Count,
            });
        }

        // First call: resolve the list input and initialize iteration state
        object? listValue = await resolveInput("list");

        IList<object?> inputItems;
        if (listValue is IEnumerable<object?> enumerable)
        {
            inputItems = enumerable.ToList();
        }
        else if (listValue is System.Collections.IEnumerable rawEnumerable)
        {
            inputItems = rawEnumerable.Cast<object?>().ToList();
        }
        else
        {
            // Not a list — skip the loop body, go straight to completed
            return GlyphNodeResult.Continue("completed");
        }

        if (inputItems.Count == 0)
        {
            return GlyphNodeResult.Continue("completed");
        }

        // Store iteration state
        context.Variables[listKey] = inputItems;
        context.Variables[indexKey] = 0;

        return GlyphNodeResult.LoopBody("loop_body", new Dictionary<string, object?>
        {
            ["element"] = inputItems[0],
            ["index"] = 0,
            ["count"] = inputItems.Count,
        });
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "For Each",
        Category = "Flow Control",
        Description = "Iterates over a list, executing the loop body once per element. " +
                      "Exposes the current Element and Index as output pins.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "list", Name = "List", DataType = GlyphDataType.List, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "loop_body", Name = "Loop Body", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "completed", Name = "Completed", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "element", Name = "Element", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "index", Name = "Index", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "count", Name = "Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
