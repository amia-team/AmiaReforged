using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class GetRegionExecutorTests
{
    private static QueryDispatcher Queries()
    {
        var repository = new InMemoryRegionRepository();
        repository.Add(new()
        {
            Tag = new RegionTag("northern_wilderness"),
            Name = "Northern Wilderness",
            Areas = [new(new AreaTag("north_forest"), [],
                new(Climate.Temperate, EconomyQuality.Average, new QualityRange()))],
        });
        return new([new GetRegionTagForAreaQueryHandler(repository)]);
    }

    [TestCase("north_forest", "northern_wilderness")]
    [TestCase("NORTH_FOREST", "northern_wilderness")]
    [TestCase("unknown_area", "")]
    public async Task Lookup_uses_the_region_query_handler_and_area_resref_mapping(string areaResRef, string expected)
    {
        var executor = new GetRegionExecutor(Queries());
        var result = await executor.ExecuteAsync(new() { TypeId = executor.TypeId },
            new() { Graph = new GlyphGraph() }, pin => Task.FromResult<object?>(
                pin == GetRegionExecutor.Inputs.AreaResref ? areaResRef : null));

        Assert.That(result.OutputValues["region"], Is.EqualTo(expected));
        Assert.That(result.NextExecPinId, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task Empty_area_returns_empty_without_dispatching_a_query(string? areaResRef)
    {
        var queries = new Mock<IQueryDispatcher>(MockBehavior.Strict);
        var executor = new GetRegionExecutor(queries.Object);
        var result = await executor.ExecuteAsync(new() { TypeId = executor.TypeId },
            new() { Graph = new GlyphGraph() }, _ => Task.FromResult<object?>(areaResRef));

        Assert.That(result.OutputValues["region"], Is.Empty);
        queries.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Query_is_awaited_and_receives_the_execution_cancellation_token()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = new Mock<IQueryDispatcher>(MockBehavior.Strict);
        queries.Setup(q => q.DispatchAsync<GetRegionTagForAreaQuery, string?>(
            It.Is<GetRegionTagForAreaQuery>(query => query.AreaResRef == "north_forest"), cancellation.Token))
            .Returns(pending.Task);
        var executor = new GetRegionExecutor(queries.Object);
        var execution = executor.ExecuteAsync(new() { TypeId = executor.TypeId },
            new() { Graph = new GlyphGraph(), CancellationToken = cancellation.Token },
            _ => Task.FromResult<object?>("north_forest"));

        Assert.That(execution.IsCompleted, Is.False);
        pending.SetResult("northern_wilderness");
        Assert.That((await execution).OutputValues["region"], Is.EqualTo("northern_wilderness"));
        queries.VerifyAll();
        queries.VerifyNoOtherCalls();
    }

    [Test]
    public void Query_failures_are_not_disguised_as_an_unregistered_area()
    {
        var executor = new GetRegionExecutor(new QueryDispatcher(Array.Empty<IQueryHandlerMarker>()));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.ExecuteAsync(
            new() { TypeId = executor.TypeId }, new() { Graph = new GlyphGraph() },
            _ => Task.FromResult<object?>("north_forest")));
    }

    [TestCase("north_forest", "northern_wilderness")]
    [TestCase("unknown_area", "")]
    public async Task Registered_module_compiles_and_executes_a_source_call_through_CQRS(string areaResRef, string expected)
    {
        var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry(), [new RegionGlyphModule(Queries())]);
        var compilation = runtime.Compiler.Compile($$"""
            glyph region_lookup : interaction {
                tick { set_metadata("region", get_region("{{areaResRef}}")) }
            }
            """);
        Assert.That(compilation.Diagnostics, Is.Empty, string.Join("\n", compilation.Diagnostics));
        var context = new GlyphExecutionContext { Graph = compilation.Executable!.CreateExecutionGraph() };
        context.Set(new InteractionGlyphContext());

        Assert.That(await runtime.Interpreter.ExecuteStageAsync(context, InteractionTickStageExecutor.NodeTypeId), Is.True);
        Assert.That(context.Get<InteractionGlyphContext>()!.InteractionMetadata!["region"], Is.EqualTo(expected));
        var function = runtime.LanguageMetadata.Functions.Single(f => f.Name == "get_region");
        Assert.That(function.ReturnType, Is.EqualTo("String"));
        Assert.That(function.Parameters.Single().Name, Is.EqualTo("area_resref"));
        Assert.That(function.Parameters.Single().Required, Is.True);
    }

    [Test]
    public void Character_current_area_and_explicit_area_resref_calls_compile()
    {
        var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry(), [new RegionGlyphModule(Queries())]);
        var compilation = runtime.Compiler.Compile("""
            glyph current_region : interaction {
                tick {
                    let area = nwn.get_area(player)
                    let region = get_region(nwn.get_resref(area))
                    if region != "" { message(player, region) }
                    let specified = get_region(area_resref: "north_forest")
                    message(player, specified)
                }
            }
            """);
        Assert.That(compilation.Diagnostics, Is.Empty, string.Join("\n", compilation.Diagnostics));
    }

    [TestCase("get_region()", "GLYPH2003")]
    [TestCase("get_region(3)", "GLYPH2004")]
    [TestCase("get_region(player)", "GLYPH2004")]
    public void Invalid_source_arguments_are_diagnosed(string call, string code)
    {
        var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry(), [new RegionGlyphModule(Queries())]);
        var compilation = runtime.Compiler.Compile("glyph invalid_region : interaction { tick { let region = " + call + " } }");
        Assert.That(compilation.Success, Is.False);
        Assert.That(compilation.Diagnostics.Any(d => d.Code == code), Is.True, string.Join("\n", compilation.Diagnostics));
    }
}
