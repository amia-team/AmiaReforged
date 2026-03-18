using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// Tests for the Break node and interpreter break-handling logic.
/// Verifies that Break correctly exits loops, unwinds through nested frames,
/// and behaves gracefully when used outside a loop.
/// </summary>
[TestFixture]
public class BreakExecutorTests
{
    // ──────────────────────────────────────────────────────────────
    //  Stub executors used only in these tests
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal entry-point node. Returns exec_out. No external context needed.
    /// </summary>
    private class StubEntryExecutor : IGlyphNodeExecutor
    {
        public const string NodeTypeId = "event.before_group_spawn";
        public string TypeId => NodeTypeId;

        public Task<GlyphNodeResult> ExecuteAsync(
            GlyphNodeInstance node, GlyphExecutionContext context,
            Func<string, Task<object?>> resolveInput)
            => Task.FromResult(GlyphNodeResult.Continue("exec_out"));

        public GlyphNodeDefinition CreateDefinition() => new()
        {
            TypeId = NodeTypeId, DisplayName = "Stub Entry", Category = "Test",
            InputPins = [],
            OutputPins =
            [
                new GlyphPin
                {
                    Id = "exec_out", Name = "Exec", DataType = GlyphDataType.Exec,
                    Direction = GlyphPinDirection.Output,
                },
            ],
        };
    }

    /// <summary>
    /// A counter node that increments a context variable each time it's executed.
    /// Used to verify how many times a node ran during a test.
    /// </summary>
    private class CounterExecutor : IGlyphNodeExecutor
    {
        public const string NodeTypeId = "test.counter";
        public string TypeId => NodeTypeId;

        public Task<GlyphNodeResult> ExecuteAsync(
            GlyphNodeInstance node, GlyphExecutionContext context,
            Func<string, Task<object?>> resolveInput)
        {
            string key = $"__counter_{node.InstanceId}";
            int current = context.Variables.TryGetValue(key, out object? val) ? Convert.ToInt32(val) : 0;
            context.Variables[key] = current + 1;
            return Task.FromResult(GlyphNodeResult.Continue("exec_out"));
        }

        public GlyphNodeDefinition CreateDefinition() => new()
        {
            TypeId = NodeTypeId, DisplayName = "Counter", Category = "Test",
            InputPins =
            [
                new GlyphPin
                {
                    Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec,
                    Direction = GlyphPinDirection.Input,
                },
            ],
            OutputPins =
            [
                new GlyphPin
                {
                    Id = "exec_out", Name = "Exec", DataType = GlyphDataType.Exec,
                    Direction = GlyphPinDirection.Output,
                },
            ],
        };
    }

    /// <summary>
    /// A ForEach-like executor that provides a fixed list so tests don't need to wire data edges.
    /// Iterates over a list of [0, 1, 2, 3, 4] (5 elements).
    /// </summary>
    private class FixedForEachExecutor : IGlyphNodeExecutor
    {
        public string TypeId => ForEachExecutor.NodeTypeId;

        public Task<GlyphNodeResult> ExecuteAsync(
            GlyphNodeInstance node, GlyphExecutionContext context,
            Func<string, Task<object?>> resolveInput)
        {
            string listKey = $"__foreach_{node.InstanceId}_list";
            string indexKey = $"__foreach_{node.InstanceId}_index";

            if (context.Variables.TryGetValue(listKey, out object? existingList) &&
                context.Variables.TryGetValue(indexKey, out object? existingIndex))
            {
                IList<object?> items = (IList<object?>)existingList!;
                int nextIndex = Convert.ToInt32(existingIndex) + 1;

                if (nextIndex >= items.Count)
                {
                    context.Variables.Remove(listKey);
                    context.Variables.Remove(indexKey);
                    return Task.FromResult(GlyphNodeResult.Continue("completed"));
                }

                context.Variables[indexKey] = nextIndex;
                return Task.FromResult(GlyphNodeResult.LoopBody("loop_body", new Dictionary<string, object?>
                {
                    ["element"] = items[nextIndex],
                    ["index"] = nextIndex,
                }));
            }

            // First call — fixed list of 5 elements
            IList<object?> fixedList = new List<object?> { 0, 1, 2, 3, 4 };
            context.Variables[listKey] = fixedList;
            context.Variables[indexKey] = 0;

            return Task.FromResult(GlyphNodeResult.LoopBody("loop_body", new Dictionary<string, object?>
            {
                ["element"] = fixedList[0],
                ["index"] = 0,
            }));
        }

        public GlyphNodeDefinition CreateDefinition() => new ForEachExecutor().CreateDefinition();
    }

    /// <summary>
    /// A conditional break: resolves its "should_break" input. If true, returns Break(). Otherwise passes through.
    /// For tests, reads from context.Variables["__should_break_{instanceId}"] to decide.
    /// </summary>
    private class ConditionalBreakExecutor : IGlyphNodeExecutor
    {
        public const string NodeTypeId = "test.conditional_break";
        public string TypeId => NodeTypeId;

        public Task<GlyphNodeResult> ExecuteAsync(
            GlyphNodeInstance node, GlyphExecutionContext context,
            Func<string, Task<object?>> resolveInput)
        {
            // Check the counter on a referenced node to decide whether to break.
            // Convention: property "break_after" = number of iterations before breaking.
            int breakAfter = 2; // default
            if (node.PropertyOverrides.TryGetValue("break_after", out string? val))
            {
                breakAfter = int.Parse(val);
            }

            // Check how many times the loop body counter has fired
            string counterKey = node.PropertyOverrides.TryGetValue("counter_key", out string? ck)
                ? ck
                : "__test_iteration";

            int current = context.Variables.TryGetValue(counterKey, out object? c) ? Convert.ToInt32(c) : 0;

            if (current >= breakAfter)
            {
                return Task.FromResult(GlyphNodeResult.Break());
            }

            return Task.FromResult(GlyphNodeResult.Continue("exec_out"));
        }

        public GlyphNodeDefinition CreateDefinition() => new()
        {
            TypeId = NodeTypeId, DisplayName = "Conditional Break", Category = "Test",
            InputPins =
            [
                new GlyphPin
                {
                    Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec,
                    Direction = GlyphPinDirection.Input,
                },
            ],
            OutputPins =
            [
                new GlyphPin
                {
                    Id = "exec_out", Name = "Exec", DataType = GlyphDataType.Exec,
                    Direction = GlyphPinDirection.Output,
                },
            ],
        };
    }

    // ──────────────────────────────────────────────────────────────
    //  Helper methods
    // ──────────────────────────────────────────────────────────────

    private GlyphInterpreter _interpreter = null!;
    private IGlyphNodeExecutor[] _executors = null!;

    [SetUp]
    public void SetUp()
    {
        _executors =
        [
            new StubEntryExecutor(),
            new FixedForEachExecutor(),
            new BreakExecutor(),
            new DoNothingExecutor(),
            new SequenceExecutor(),
            new CounterExecutor(),
            new ConditionalBreakExecutor(),
        ];

        GlyphNodeDefinitionRegistry registry = new();
        foreach (IGlyphNodeExecutor exec in _executors)
        {
            registry.Register(exec.CreateDefinition());
        }

        _interpreter = new GlyphInterpreter(registry, _executors);
    }

    private static GlyphExecutionContext CreateContext(GlyphGraph graph) => new()
    {
        Graph = graph,
        MaxExecutionSteps = 500,
        EnableTracing = true,
    };

    private static int GetCounter(GlyphExecutionContext ctx, Guid nodeId)
    {
        string key = $"__counter_{nodeId}";
        return ctx.Variables.TryGetValue(key, out object? val) ? Convert.ToInt32(val) : 0;
    }

    // ──────────────────────────────────────────────────────────────
    //  Tests
    // ──────────────────────────────────────────────────────────────

    [Test]
    public async Task BreakExecutor_returns_IsBreak_result()
    {
        BreakExecutor executor = new();
        GlyphNodeInstance node = new()
        {
            TypeId = BreakExecutor.NodeTypeId,
            PropertyOverrides = new Dictionary<string, string>(),
        };
        GlyphExecutionContext context = new()
        {
            Graph = new GlyphGraph { EventType = GlyphEventType.BeforeGroupSpawn, Name = "Test" },
            MaxExecutionSteps = 100,
        };

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.IsBreak.Should().BeTrue();
        result.NextExecPinId.Should().BeNull();
        result.IsLoopNode.Should().BeFalse();
    }

    [Test]
    public async Task Break_inside_ForEach_exits_loop_early()
    {
        // Graph: Entry → ForEach(5 items) → [loop_body] → Counter → Break
        //                                 → [completed] → CompletedCounter
        GlyphNodeInstance entry = new() { TypeId = StubEntryExecutor.NodeTypeId };
        GlyphNodeInstance forEach = new() { TypeId = ForEachExecutor.NodeTypeId };
        GlyphNodeInstance bodyCounter = new() { TypeId = CounterExecutor.NodeTypeId };
        GlyphNodeInstance breakNode = new() { TypeId = BreakExecutor.NodeTypeId };
        GlyphNodeInstance completedCounter = new() { TypeId = CounterExecutor.NodeTypeId };

        GlyphGraph graph = new()
        {
            EventType = GlyphEventType.BeforeGroupSpawn,
            Name = "Break Test",
            Nodes = [entry, forEach, bodyCounter, breakNode, completedCounter],
            Edges =
            [
                // Entry → ForEach
                new GlyphEdge
                {
                    SourceNodeId = entry.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = forEach.InstanceId, TargetPinId = "exec_in",
                },
                // ForEach loop_body → Counter
                new GlyphEdge
                {
                    SourceNodeId = forEach.InstanceId, SourcePinId = "loop_body",
                    TargetNodeId = bodyCounter.InstanceId, TargetPinId = "exec_in",
                },
                // Counter → Break
                new GlyphEdge
                {
                    SourceNodeId = bodyCounter.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = breakNode.InstanceId, TargetPinId = "exec_in",
                },
                // ForEach completed → CompletedCounter
                new GlyphEdge
                {
                    SourceNodeId = forEach.InstanceId, SourcePinId = "completed",
                    TargetNodeId = completedCounter.InstanceId, TargetPinId = "exec_in",
                },
            ],
        };

        GlyphExecutionContext ctx = CreateContext(graph);
        bool success = await _interpreter.ExecuteAsync(ctx);

        success.Should().BeTrue();

        // The body counter should fire exactly once (first iteration), then Break exits the loop.
        GetCounter(ctx, bodyCounter.InstanceId).Should().Be(1,
            "Break should exit the loop on the first iteration, so the body runs once.");

        // The completed counter should fire once (Break resumes via the 'completed' pin).
        GetCounter(ctx, completedCounter.InstanceId).Should().Be(1,
            "After Break, execution should flow through the loop's 'completed' pin.");

        // ForEach iteration state should be cleaned up
        string listKey = $"__foreach_{forEach.InstanceId}_list";
        string indexKey = $"__foreach_{forEach.InstanceId}_index";
        ctx.Variables.Should().NotContainKey(listKey, "ForEach list variable should be cleaned up after Break.");
        ctx.Variables.Should().NotContainKey(indexKey, "ForEach index variable should be cleaned up after Break.");
    }

    [Test]
    public async Task Break_after_N_iterations_stops_loop()
    {
        // Graph: Entry → ForEach(5 items) → [loop_body] → Counter → ConditionalBreak (break_after=3)
        //                                                            → exec_out → (dead end)
        //                                 → [completed] → CompletedCounter
        // The counter increments each iteration. ConditionalBreak checks the counter.
        GlyphNodeInstance entry = new() { TypeId = StubEntryExecutor.NodeTypeId };
        GlyphNodeInstance forEach = new() { TypeId = ForEachExecutor.NodeTypeId };
        GlyphNodeInstance bodyCounter = new() { TypeId = CounterExecutor.NodeTypeId };
        GlyphNodeInstance condBreak = new()
        {
            TypeId = ConditionalBreakExecutor.NodeTypeId,
            PropertyOverrides = new Dictionary<string, string>
            {
                ["break_after"] = "3",
                ["counter_key"] = $"__counter_{bodyCounter.InstanceId}",
            },
        };
        GlyphNodeInstance completedCounter = new() { TypeId = CounterExecutor.NodeTypeId };

        // Need a DoNothing after condBreak for when it doesn't break
        GlyphNodeInstance doNothing = new() { TypeId = DoNothingExecutor.NodeTypeId };

        GlyphGraph graph = new()
        {
            EventType = GlyphEventType.BeforeGroupSpawn,
            Name = "Conditional Break Test",
            Nodes = [entry, forEach, bodyCounter, condBreak, completedCounter, doNothing],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = entry.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = forEach.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = forEach.InstanceId, SourcePinId = "loop_body",
                    TargetNodeId = bodyCounter.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = bodyCounter.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = condBreak.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = condBreak.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = doNothing.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = forEach.InstanceId, SourcePinId = "completed",
                    TargetNodeId = completedCounter.InstanceId, TargetPinId = "exec_in",
                },
            ],
        };

        GlyphExecutionContext ctx = CreateContext(graph);
        bool success = await _interpreter.ExecuteAsync(ctx);

        success.Should().BeTrue();

        // Counter fires 3 times (iterations 0, 1, 2), then on iteration 3 the ConditionalBreak triggers.
        GetCounter(ctx, bodyCounter.InstanceId).Should().Be(3,
            "Body counter should execute 3 times before conditional break fires.");

        GetCounter(ctx, completedCounter.InstanceId).Should().Be(1,
            "Completed pin should be followed after break.");
    }

    [Test]
    public async Task Break_outside_loop_terminates_branch_gracefully()
    {
        // Graph: Entry → Break → Counter (unreachable)
        GlyphNodeInstance entry = new() { TypeId = StubEntryExecutor.NodeTypeId };
        GlyphNodeInstance breakNode = new() { TypeId = BreakExecutor.NodeTypeId };
        GlyphNodeInstance counter = new() { TypeId = CounterExecutor.NodeTypeId };

        GlyphGraph graph = new()
        {
            EventType = GlyphEventType.BeforeGroupSpawn,
            Name = "Break No Loop Test",
            Nodes = [entry, breakNode, counter],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = entry.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = breakNode.InstanceId, TargetPinId = "exec_in",
                },
                // Break has no output pins, so no edge to counter — but even if there were,
                // the Break result's IsBreak flag means execution terminates or unwinds.
            ],
        };

        GlyphExecutionContext ctx = CreateContext(graph);
        bool success = await _interpreter.ExecuteAsync(ctx);

        success.Should().BeTrue("Break outside a loop should not crash the interpreter.");
        GetCounter(ctx, counter.InstanceId).Should().Be(0, "Counter after break should never execute.");
    }

    [Test]
    public async Task Break_inside_Sequence_inside_ForEach_unwinds_through_Sequence()
    {
        // Graph: Entry → ForEach(5 items) → [loop_body] → Sequence
        //                                                   → then_0 → Counter_A → Break
        //                                                   → then_1 → Counter_B (should NOT run — Break unwound)
        //                                 → [completed] → CompletedCounter
        GlyphNodeInstance entry = new() { TypeId = StubEntryExecutor.NodeTypeId };
        GlyphNodeInstance forEach = new() { TypeId = ForEachExecutor.NodeTypeId };
        GlyphNodeInstance sequence = new() { TypeId = SequenceExecutor.NodeTypeId };
        GlyphNodeInstance counterA = new() { TypeId = CounterExecutor.NodeTypeId };
        GlyphNodeInstance breakNode = new() { TypeId = BreakExecutor.NodeTypeId };
        GlyphNodeInstance counterB = new() { TypeId = CounterExecutor.NodeTypeId };
        GlyphNodeInstance completedCounter = new() { TypeId = CounterExecutor.NodeTypeId };

        GlyphGraph graph = new()
        {
            EventType = GlyphEventType.BeforeGroupSpawn,
            Name = "Break Through Sequence Test",
            Nodes = [entry, forEach, sequence, counterA, breakNode, counterB, completedCounter],
            Edges =
            [
                new GlyphEdge
                {
                    SourceNodeId = entry.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = forEach.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = forEach.InstanceId, SourcePinId = "loop_body",
                    TargetNodeId = sequence.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = sequence.InstanceId, SourcePinId = "then_0",
                    TargetNodeId = counterA.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = counterA.InstanceId, SourcePinId = "exec_out",
                    TargetNodeId = breakNode.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = sequence.InstanceId, SourcePinId = "then_1",
                    TargetNodeId = counterB.InstanceId, TargetPinId = "exec_in",
                },
                new GlyphEdge
                {
                    SourceNodeId = forEach.InstanceId, SourcePinId = "completed",
                    TargetNodeId = completedCounter.InstanceId, TargetPinId = "exec_in",
                },
            ],
        };

        GlyphExecutionContext ctx = CreateContext(graph);
        bool success = await _interpreter.ExecuteAsync(ctx);

        success.Should().BeTrue();

        // Counter A runs once (then_0 executes, then Break fires)
        GetCounter(ctx, counterA.InstanceId).Should().Be(1,
            "Counter A (before Break in Sequence) should run once.");

        // Counter B should NOT run — Break unwinds through the Sequence frame to reach the loop
        GetCounter(ctx, counterB.InstanceId).Should().Be(0,
            "Counter B (then_1 in Sequence) should NOT run — Break should unwind past the Sequence frame.");

        // Completed counter should run — Break exits the loop and follows 'completed'
        GetCounter(ctx, completedCounter.InstanceId).Should().Be(1,
            "Completed counter should run after Break exits the loop.");
    }

    [Test]
    public async Task Break_definition_has_correct_shape()
    {
        BreakExecutor executor = new();
        GlyphNodeDefinition def = executor.CreateDefinition();

        def.TypeId.Should().Be("flow.break");
        def.DisplayName.Should().Be("Break");
        def.Category.Should().Be("Flow Control");
        def.Archetype.Should().Be(GlyphNodeArchetype.FlowControl);
        def.InputPins.Should().HaveCount(1);
        def.InputPins[0].Id.Should().Be("exec_in");
        def.InputPins[0].DataType.Should().Be(GlyphDataType.Exec);
        def.OutputPins.Should().BeEmpty("Break has no output pins — it signals the interpreter directly.");
    }

    [Test]
    public void GlyphNodeResult_Break_factory_sets_correct_flags()
    {
        GlyphNodeResult result = GlyphNodeResult.Break();

        result.IsBreak.Should().BeTrue();
        result.IsLoopNode.Should().BeFalse();
        result.NextExecPinId.Should().BeNull();
        result.BranchPinIds.Should().BeNull();
        result.OutputValues.Should().BeEmpty();
    }
}
