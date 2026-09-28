using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlyphRuntimeLifecycleTests
{
    private GlyphBootstrap _runtime = null!;
    private GlyphNodeDefinitionRegistry _definitions = null!;
    [SetUp] public void Setup() { _definitions = new(); _runtime = new(_definitions); }
    private GlyphExecutable Compile(string source)
    {
        var compiled = _runtime.Compiler.Compile(source);
        Assert.That(compiled.Success, Is.True, string.Join("\n", compiled.Diagnostics));
        return compiled.Executable!;
    }
    private static GlyphExecutionContext Encounter(GlyphProgramVersion version) => new()
    {
        Graph = version.CreateExecutionGraph(), Profile = new() { Name = "test" },
        EncounterContext = new() { AreaResRef = "test" }
    };
    [Test] public async Task An_in_flight_awaited_executor_finishes_on_its_captured_program()
    {
        var a = Compile("glyph a : encounter.before_group_spawn { spawn.modify_count(2) }");
        var b = Compile("glyph a : encounter.before_group_spawn { spawn.modify_count(9) }");
        Guid id = Guid.NewGuid(); await _runtime.Programs.ActivateAsync(id, a);
        var paused = new PausedExecutor();
        var executors = GlyphBootstrap.CreateExecutors().Where(e => e.TypeId != paused.TypeId).Append(paused);
        var interpreter = new GlyphInterpreter(_definitions, executors);
        var context = Encounter(_runtime.Programs.GetActive(id)!);
        Task<bool> executing = interpreter.ExecuteAsync(context);
        await paused.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _runtime.Programs.ActivateAsync(id, b);
        var next = Encounter(_runtime.Programs.GetActive(id)!);
        await _runtime.Interpreter.ExecuteAsync(next);
        Assert.That(next.SpawnCount, Is.EqualTo(9));
        paused.Resume.SetResult(); await executing;
        Assert.That(context.SpawnCount, Is.EqualTo(2));
    }
    [Test] public async Task Readers_observe_only_whole_candidates_during_concurrent_publication()
    {
        var a = Compile("glyph a : interaction { tick { fail \"A\" } }");
        var b = Compile("glyph a : interaction { tick { fail \"B\" } }");
        Guid id = Guid.NewGuid(); await _runtime.Programs.ActivateAsync(id, a);
        Task[] writers = Enumerable.Range(0, 40).Select(i => Task.Run(() => _runtime.Programs.ActivateAsync(id, i % 2 == 0 ? a : b))).ToArray();
        await Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var current = _runtime.Programs.GetActive(id)!;
                Assert.That(ReferenceEquals(current.Executable, a) || ReferenceEquals(current.Executable, b), Is.True);
                Assert.That(current.DefinitionId, Is.EqualTo(id));
            }
        });
        await Task.WhenAll(writers);
        var history = _runtime.Programs.GetVersions(id);
        Assert.That(history.Count, Is.EqualTo(41));
        for (int i = 1; i < history.Count; i++) Assert.That(history[i].PreviousVersionId, Is.EqualTo(history[i - 1].VersionId));
    }
    [Test] public async Task Startup_restores_published_source_instead_of_invalid_draft_and_retains_rollback()
    {
        Guid id = Guid.NewGuid();
        await _runtime.Programs.ActivateAsync(id, Compile("glyph a : interaction {}"));
        await _runtime.Programs.ActivateAsync(id, Compile("glyph a : interaction { tick { fail \"B\" } }"));
        var definition = new GlyphDefinition
        {
            Id = id, SourceText = "invalid draft", IsActive = true,
            PublishedVersionsJson = GlyphPublishedVersion.Serialize(_runtime.Programs.GetVersions(id))
        };
        var restored = new GlyphRuntimeRegistry();
        GlyphPublishedVersion.Restore(definition, _runtime.Compiler, restored);
        Assert.That(restored.GetActive(id)!.VersionId, Is.EqualTo(_runtime.Programs.GetActive(id)!.VersionId));
        var rollback = await restored.RollbackAsync(id);
        Assert.That(rollback!.Executable.SourceText, Is.EqualTo("glyph a : interaction {}"));
    }
    [Test] public async Task Mutable_interaction_reads_observe_previous_writes_and_stages_stay_independent()
    {
        var program = Compile("""
            glyph work : interaction {
                started { required_rounds = 4 }
                tick { progress += 1 progress += 1 }
                completed { metadata["result"] = "done" }
            }
            """);
        InteractionSession session = new()
        {
            CharacterId = new(Guid.NewGuid()), InteractionTag = "work", TargetId = Guid.NewGuid(),
            TargetMode = InteractionTargetMode.Node, RequiredRounds = 8, Progress = 2
        };
        var context = new GlyphExecutionContext { Graph = program.CreateExecutionGraph(), Session = session, InteractionProgress = 2 };
        await _runtime.Interpreter.ExecuteStageAsync(context, "stage.interaction_tick");
        Assert.That(session.Progress, Is.EqualTo(4));
        Assert.That(session.RequiredRounds, Is.EqualTo(8));
        Assert.That(session.Metadata, Is.Null);
    }
    [Test] public async Task Foreach_break_and_nested_branches_use_the_existing_loop_stack()
    {
        var program = Compile("""
            glyph work : encounter.after_group_spawn {
                foreach member in context.spawned_creatures {
                    if true { break }
                    spawn.skip_bonuses()
                }
                spawn.skip_mutations()
            }
            """);
        var context = new GlyphExecutionContext
        {
            Graph = program.CreateExecutionGraph(), SpawnedCreatures = [1, 2, 3],
            EncounterContext = new() { AreaResRef = "test" }, Profile = new() { Name = "test" }
        };
        await _runtime.Interpreter.ExecuteAsync(context);
        Assert.That(context.ShouldSkipBonuses, Is.False);
        Assert.That(context.ShouldSkipMutations, Is.True);
    }
    private sealed class PausedExecutor : IGlyphNodeExecutor
    {
        public string TypeId => ModifySpawnCountExecutor.NodeTypeId;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public GlyphNodeDefinition CreateDefinition() => new ModifySpawnCountExecutor().CreateDefinition();
        public async Task<GlyphNodeResult> ExecuteAsync(GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
        {
            Entered.SetResult(); await Resume.Task;
            return await new ModifySpawnCountExecutor().ExecuteAsync(node, context, resolveInput);
        }
    }
}
