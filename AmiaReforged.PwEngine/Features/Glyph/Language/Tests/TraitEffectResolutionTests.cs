using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class TraitEffectResolutionTests
{
    private GlyphBootstrap _runtime = null!;
    [SetUp] public void Setup() => _runtime = new(new GlyphNodeDefinitionRegistry());

    [TestCase("main")]
    [TestCase("client_enter")]
    [TestCase("level_up")]
    [TestCase("respawn")]
    [TestCase("confirmed")]
    public void Custom_effects_compile_in_each_rebuild_stage(string stage)
    {
        var result = _runtime.Compiler.Compile($$"""
            fn protection(): Void { trait.add_effect(effect.ac_increase(2)) }
            glyph shield : trait.on_effect_resolution {
                {{stage}} { protection() let tag = trait_tag let target = creature }
            }
            """);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        Assert.That(result.Executable!.EventType, Is.EqualTo(GlyphEventType.TraitEffectResolution));
    }

    [TestCase("glyph a : trait.on_effect_resolution { death { trait.add_effect(effect.haste()) } }")]
    [TestCase("glyph a : trait.on_granted { trait.add_effect(effect.haste()) }")]
    [TestCase("glyph a : interaction { tick { trait.add_effect(effect.haste()) } }")]
    [TestCase("glyph a : trait.on_effect_resolution { respawn { let attacker = killer } }")]
    [TestCase("glyph a : trait.on_effect_resolution { attempted {} }")]
    [TestCase("glyph a : trait.on_effect_resolution { client_enter {} client_enter {} }")]
    [TestCase("glyph a : trait.on_effect_resolution { main {} main {} }")]
    [TestCase("glyph a : interaction { main {} }")]
    [TestCase("glyph a : trait.on_effect_resolution { trait.add_effect(effect.haste()) }")]
    public void Invalid_stage_or_context_is_rejected(string source)
    {
        var result = _runtime.Compiler.Compile(source);
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics, Is.Not.Empty);
    }

    [Test]
    public async Task Lifecycle_stages_are_independent_and_omitted_stages_are_noops()
    {
        var result = _runtime.Compiler.Compile("""
            glyph test : trait.on_effect_resolution {
                main { var marker = 0 }
                client_enter { var marker = 1 }
                level_up { var marker = 2 }
                respawn { var marker = 3 }
                confirmed { var marker = 4 }
                death { var marker = 5 var attacker = killer }
            }
            """);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        foreach (var stage in Platform.GlyphEvents.Get(GlyphEventType.TraitEffectResolution).Stages!)
        {
            GlyphExecutionContext context = new() { Graph = result.Executable!.CreateExecutionGraph() };
            context.Set(new TraitGlyphContext { Killer = 123u });
            Assert.That(await _runtime.Interpreter.ExecuteStageAsync(context, stage.EntryTypeId), Is.True);
            Assert.That(context.Locals.Count, Is.EqualTo(stage.Name == "death" ? 2 : 1));
            Assert.That(context.Get<InteractionGlyphContext>(), Is.Null);
        }
        var empty = _runtime.Compiler.Compile("glyph empty : trait.on_effect_resolution { confirmed {} }");
        GlyphExecutionContext omitted = new() { Graph = empty.Executable!.CreateExecutionGraph() };
        Assert.That(await _runtime.Interpreter.ExecuteStageAsync(omitted, TraitRespawnStageExecutor.NodeTypeId), Is.True);
        Assert.That(omitted.Locals, Is.Empty);
    }

    [Test]
    public void Stage_names_remain_valid_identifiers_outside_stage_declarations()
    {
        var result = _runtime.Compiler.Compile("""
            fn death(): Effect = effect.death()
            glyph test : trait.on_effect_resolution {
                confirmed { let respawn = death() trait.add_effect(respawn) }
            }
            """);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test]
    public async Task Effect_contributions_are_collected_without_applying_them()
    {
        AddTraitEffectExecutor executor = new();
        GlyphExecutionContext context = new()
        {
            Graph = new() { EventType = GlyphEventType.TraitEffectResolution },
            CurrentPipelineStage = TraitConfirmedStageExecutor.NodeTypeId
        };
        TraitGlyphContext trait = new();
        context.Set(trait);
        GlyphNwnEffect effect = new(new IntPtr(42));
        var node = new GlyphNodeInstance { TypeId = executor.TypeId };
        await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(effect));
        await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(default(GlyphNwnEffect)));
        Assert.That(trait.Effects, Is.EqualTo(new[] { effect }));

        context.CurrentPipelineStage = TraitDeathStageExecutor.NodeTypeId;
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(effect)));
        Assert.That(trait.Effects.Count, Is.EqualTo(1));
    }

    [Test]
    public void Metadata_advertises_stages_and_death_only_killer()
    {
        var metadata = _runtime.LanguageMetadata;
        var evt = metadata.Events.Single(e => e.Name == "trait.on_effect_resolution");
        Assert.That(evt.Stages, Is.EqualTo(new[] { "main", "client_enter", "level_up", "respawn", "confirmed", "death" }));
        var add = metadata.Functions.Single(f => f.Name == "trait.add_effect");
        Assert.That(add.AvailableIn.Select(s => s.Stage), Is.EquivalentTo(evt.Stages.Where(s => s != "death")));
        var scopes = metadata.Contexts.Where(c => c.Event == evt.Name).ToList();
        Assert.That(scopes.Single(c => c.Stage == "death").Fields.Any(f => f.Name == "killer"), Is.True);
        Assert.That(scopes.Where(c => c.Stage != "death").All(c => c.Fields.All(f => f.Name != "killer")), Is.True);
    }
}
