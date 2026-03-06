using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// ForEach node — iterates over a list, executing the loop body once per element.
/// Exposes the current element and index as output data pins during each iteration.
/// After the loop completes, execution continues via the "completed" Exec output.
/// <para>
/// Note: This node is special — the interpreter must handle it differently from
/// normal nodes because it needs to re-execute the loop body branch multiple times.
/// The executor signals this by returning a special result.
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
        object? listValue = await resolveInput("list");

        // Try to interpret the input as a list
        IList<object?> items;
        if (listValue is IEnumerable<object?> enumerable)
        {
            items = enumerable.ToList();
        }
        else if (listValue is System.Collections.IEnumerable rawEnumerable)
        {
            items = rawEnumerable.Cast<object?>().ToList();
        }
        else
        {
            // Not a list — skip the loop body, go straight to completed
            return GlyphNodeResult.Continue("completed");
        }

        if (items.Count == 0)
        {
            return GlyphNodeResult.Continue("completed");
        }

        // Store the list and current index in context variables keyed by this node instance
        string listKey = $"__foreach_{node.InstanceId}_list";
        string indexKey = $"__foreach_{node.InstanceId}_index";

        context.Variables[listKey] = items;
        context.Variables[indexKey] = 0;

        // Set the first element's output values
        var outputs = new Dictionary<string, object?>
        {
            ["element"] = items[0],
            ["index"] = 0,
            ["count"] = items.Count
        };

        return new GlyphNodeResult
        {
            NextExecPinId = "loop_body",
            OutputValues = outputs
        };
    }

    /// <summary>
    /// Called by the interpreter after the loop body branch completes to advance to
    /// the next iteration. Returns null when the loop is finished.
    /// </summary>
    public static GlyphNodeResult? AdvanceIteration(
        GlyphNodeInstance node,
        GlyphExecutionContext context)
    {
        string listKey = $"__foreach_{node.InstanceId}_list";
        string indexKey = $"__foreach_{node.InstanceId}_index";

        if (!context.Variables.TryGetValue(listKey, out object? listObj) ||
            !context.Variables.TryGetValue(indexKey, out object? indexObj))
        {
            return null;
        }

        IList<object?> items = (IList<object?>)listObj!;
        int nextIndex = Convert.ToInt32(indexObj) + 1;

        if (nextIndex >= items.Count)
        {
            // Loop complete — clean up and signal completion
            context.Variables.Remove(listKey);
            context.Variables.Remove(indexKey);
            return GlyphNodeResult.Continue("completed");
        }

        // Store updated index
        context.Variables[indexKey] = nextIndex;

        // Update output values for the next iteration
        var outputs = new Dictionary<string, object?>
        {
            ["element"] = items[nextIndex],
            ["index"] = nextIndex,
            ["count"] = items.Count
        };

        return new GlyphNodeResult
        {
            NextExecPinId = "loop_body",
            OutputValues = outputs
        };
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "For Each",
        Category = "Flow Control",
        Description = "Iterates over a list, executing the loop body once per element. " +
                      "Exposes the current Element and Index as output pins.",
        ColorClass = "node-flow",
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
