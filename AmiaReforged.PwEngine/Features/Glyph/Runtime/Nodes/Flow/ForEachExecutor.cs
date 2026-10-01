using AmiaReforged.PwEngine.Features.Glyph.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// ForEach node — iterates over a list, executing the loop body once per element.
/// Exposes the current element and index as output data pins during each iteration.
/// After the loop completes, execution continues via the "completed" Exec output.
/// <para>
/// The executor manages its own iteration state via <see cref="GlyphExecutionContext.LoopStates"/>,
/// keyed by the node instance ID. On each execution:
/// <list type="bullet">
/// <item>First call: stores the list and sets index to 0, returns <see cref="GlyphNodeResult.LoopBody"/>.</item>
/// <item>Subsequent calls (re-execution by the interpreter after loop body completes): advances the index.
///   If more elements remain, returns <see cref="GlyphNodeResult.LoopBody"/>. Otherwise returns
///   <see cref="GlyphNodeResult.Continue"/> with "completed".</item>
/// </list>
/// </para>
/// </summary>
[GlyphNode]
public partial class ForEachExecutor : IGlyphNodeExecutor
{
    private sealed record IterationState(IReadOnlyList<object?> Items, int Index);

    public const string NodeTypeId = "flow.for_each";

    public virtual string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        if (context.LoopStates.TryGetValue(node.InstanceId, out object? existing))
        {
            var state = (IterationState)existing;
            int nextIndex = state.Index + 1;
            if (nextIndex >= state.Items.Count)
            {
                context.LoopStates.Remove(node.InstanceId);
                return GlyphNodeResult.Continue("completed");
            }
            context.LoopStates[node.InstanceId] = state with { Index = nextIndex };
            return GlyphNodeResult.LoopBody("loop_body", new()
            {
                ["element"] = state.Items[nextIndex], ["index"] = nextIndex, ["count"] = state.Items.Count
            });
        }

        // First call: resolve the list input and initialize iteration state
        object? listValue = await resolveInput("list");

        IReadOnlyList<object?> inputItems;
        var element = CreateDefinition().InputPins.First(p => p.Id == "list").ElementType ?? GlyphDataType.NwObject;
        if (node.PropertyOverrides.GetValueOrDefault("snapshot") == "true")
            inputItems = GlyphCollections.Snapshot(listValue, element, context, node.PropertyOverrides.GetValueOrDefault("element_type"));
        else if (listValue is IEnumerable<object?> enumerable)
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
        context.LoopStates[node.InstanceId] = new IterationState(inputItems, 0);

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
    public virtual GlyphNodeDefinition CreateDefinition() => Definition(TypeId, GlyphDataType.NwObject);

    protected static GlyphNodeDefinition Definition(string typeId, GlyphDataType elementType) => new()
    {
        TypeId = typeId,
        DisplayName = "For Each",
        Category = "Flow Control",
        Description = "Iterates over a list, executing the loop body once per element. " +
                      "Exposes the current Element and Index as output pins.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "list", Name = "List", DataType = GlyphDataType.List, ElementType = elementType, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "loop_body", Name = "Loop Body", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "completed", Name = "Completed", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "element", Name = "Element", DataType = elementType, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "index", Name = "Index", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "count", Name = "Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
