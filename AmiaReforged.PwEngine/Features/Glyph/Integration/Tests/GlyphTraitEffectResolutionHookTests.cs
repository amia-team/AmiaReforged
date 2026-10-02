using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class GlyphTraitEffectResolutionHookTests
{
    private GlyphBootstrap _runtime = null!;
    private readonly List<TraitGlyphBinding> _bindings = [];
    private GlyphTraitHookService _hooks = null!;
    private readonly Guid _characterId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _bindings.Clear();
        _runtime = new(new GlyphNodeDefinitionRegistry(), [new TestEffectModule()]);
        var repository = new Mock<IGlyphRepository>();
        repository.Setup(r => r.GetAllTraitBindingsAsync()).ReturnsAsync(_bindings);
        _hooks = new(_runtime, repository.Object, Mock.Of<ITraitSubsystem>());
    }

    private async Task<Guid> Bind(string source, int priority = 0)
    {
        var result = _runtime.Compiler.Compile(source);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        GlyphDefinition definition = new()
        {
            Id = Guid.NewGuid(), Name = result.Executable!.Name,
            EventType = result.Executable.EventType.ToString(), Category = "Trait",
            SourceText = source, IsActive = false
        };
        _bindings.Add(new()
        {
            Id = Guid.NewGuid(), TraitTag = "shield", GlyphDefinitionId = definition.Id,
            GlyphDefinition = definition, Priority = priority
        });
        await _runtime.Programs.ActivateAsync(definition.Id, result.Executable);
        await _hooks.RefreshCacheAsync();
        return definition.Id;
    }

    private List<GlyphNwnEffect> Run(TraitEffectResolutionStage stage) =>
        _hooks.RunEffectResolution("shield", _characterId, 123u, stage, ["shield", "brave"], killer: 456u);

    [TestCase(TraitEffectResolutionStage.ClientEnter, 1)]
    [TestCase(TraitEffectResolutionStage.LevelUp, 2)]
    [TestCase(TraitEffectResolutionStage.Respawn, 3)]
    [TestCase(TraitEffectResolutionStage.Confirmed, 4)]
    [TestCase(TraitEffectResolutionStage.Death, 0)]
    public async Task Dispatches_only_matching_stage_and_populates_trait_context(TraitEffectResolutionStage stage, int expected)
    {
        await Bind("""
            glyph shield : trait.on_effect_resolution {
                client_enter { if has_trait("brave") { trait.add_effect(test.effect(1)) } }
                level_up { trait.add_effect(test.effect(2)) }
                respawn { trait.add_effect(test.effect(3)) }
                confirmed { trait.add_effect(test.effect(4)) }
                death { var attacker = killer }
            }
            """);
        GlyphExecutionContext? execution = null;
        _runtime.Interpreter.ExecutionCompleted += context => execution = context;
        var effects = Run(stage);
        Assert.That(effects.Select(e => e.Handle.ToInt32()), Is.EqualTo(expected == 0 ? Array.Empty<int>() : new[] { expected }));
        Assert.That(execution, Is.Not.Null);
        Assert.That(execution!.CharacterId, Is.EqualTo(_characterId.ToString()));
        Assert.That(execution.TraitTag, Is.EqualTo("shield"));
        Assert.That(execution.TargetCreature, Is.EqualTo(123u));
        if (stage == TraitEffectResolutionStage.Death)
            Assert.That(execution.Locals.Values.Single().Value, Is.EqualTo(456u));
    }

    [Test]
    public async Task Priority_is_preserved_and_failed_script_contributions_are_discarded()
    {
        await Bind("glyph later : trait.on_effect_resolution { confirmed { trait.add_effect(test.effect(3)) } }", 30);
        await Bind("glyph failed : trait.on_effect_resolution { confirmed { trait.add_effect(test.effect(1)) test.fail() } }", 10);
        await Bind("glyph first : trait.on_effect_resolution { confirmed { trait.add_effect(test.effect(2)) } }", 20);
        Assert.That(Run(TraitEffectResolutionStage.Confirmed).Select(e => e.Handle.ToInt32()), Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public async Task Activation_rollback_and_deactivation_take_effect_without_refreshing_bindings()
    {
        Guid id = await Bind("glyph live : trait.on_effect_resolution { confirmed { trait.add_effect(test.effect(1)) } }");
        var updated = _runtime.Compiler.Compile("glyph live : trait.on_effect_resolution { confirmed { trait.add_effect(test.effect(2)) } }");
        Assert.That(updated.Success, Is.True);
        await _runtime.Programs.ActivateAsync(id, updated.Executable!);
        Assert.That(Run(TraitEffectResolutionStage.Confirmed).Single().Handle.ToInt32(), Is.EqualTo(2));
        await _runtime.Programs.RollbackAsync(id);
        Assert.That(Run(TraitEffectResolutionStage.Confirmed).Single().Handle.ToInt32(), Is.EqualTo(1));
        await _runtime.Programs.DeactivateAsync(id);
        Assert.That(Run(TraitEffectResolutionStage.Confirmed), Is.Empty);
    }

    [Test]
    public async Task Missing_stage_and_unrelated_trait_events_contribute_nothing()
    {
        await Bind("glyph partial : trait.on_effect_resolution { confirmed { trait.add_effect(test.effect(1)) } }");
        await Bind("glyph granted : trait.on_granted { test.fail() }");
        Assert.That(Run(TraitEffectResolutionStage.Respawn), Is.Empty);
        Assert.That(Run(TraitEffectResolutionStage.Confirmed).Count, Is.EqualTo(1));
        Assert.That(_hooks.RunEffectResolution("unbound", _characterId, 123u,
            TraitEffectResolutionStage.Confirmed, ["unbound"]), Is.Empty);
    }

    private sealed class TestEffectModule : IGlyphModule
    {
        public void Configure(GlyphModuleBuilder glyph)
        {
            glyph.Add(new TestEffectExecutor());
            glyph.Add(new TestFailureExecutor());
        }
    }

    // Opaque effect handles avoid requiring a running NWN server in dispatch tests.
    private sealed class TestEffectExecutor : GlyphNodeBase
    {
        public override string TypeId => "test.effect";
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        {
            TypeId = TypeId, DisplayName = "Test Effect", Category = "Test",
            Parameters = [Pins.InInt("marker", "Marker")], Results = [Pins.Out("effect", "Effect", GlyphDataType.Effect)],
            Exports = [new("test.effect", "effect")]
        }.CreateDefinition();
        public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) =>
            GlyphNodeResult.Data(new() { ["effect"] = new GlyphNwnEffect(new IntPtr(await cx.InInt("marker"))) });
    }

    private sealed class TestFailureExecutor : GlyphNodeBase
    {
        public override string TypeId => "test.fail";
        public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
        {
            TypeId = TypeId, DisplayName = "Test Failure", Category = "Test", Archetype = GlyphNodeArchetype.Action,
            Exports = [new("test.fail")]
        }.CreateDefinition();
        public override Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) => throw new InvalidOperationException("Test failure");
    }
}
