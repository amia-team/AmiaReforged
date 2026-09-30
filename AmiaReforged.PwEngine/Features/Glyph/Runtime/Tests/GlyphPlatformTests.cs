using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Context;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class GlyphPlatformTests
{
    private GlyphNodeDefinitionRegistry _registry = null!;
    private List<IGlyphNodeExecutor> _executors = null!;
    private GlyphCompiler _compiler = null!;

    [SetUp] public void Setup()
    {
        _registry = new();
        _executors = GlyphGeneratedRegistry.CreateExecutors();
        foreach (var executor in _executors) _registry.Register(executor.CreateDefinition());
        _compiler = new(_registry);
    }

    [Test] public void Registered_platform_is_conformant_and_deterministic()
    {
        Assert.DoesNotThrow(() => GlyphFeatureVerifier.Verify(_registry, _executors));
        Assert.That(_executors.Select(e => e.TypeId), Is.Ordered.Using<string>(StringComparer.Ordinal));
        Assert.That(GlyphGeneratedRegistry.CreateExecutors().Select(e => e.TypeId),
            Is.EqualTo(_executors.Select(e => e.TypeId)));
        Assert.That(_compiler.Catalog.Symbols.Any(s => s.Definition.TypeId.StartsWith("flow.")), Is.False);
        Assert.That(_compiler.Catalog.Symbols.Any(s => s.Definition.Archetype is GlyphNodeArchetype.EventEntry or
            GlyphNodeArchetype.ContextGetter or GlyphNodeArchetype.PipelineStage), Is.False);
    }

    [Test] public void Action_value_and_predicate_definitions_preserve_their_contracts()
    {
        var heal = HealCreatureExecutor.Descriptor.CreateDefinition();
        Assert.That(heal.InputPins.Select(p => p.Id), Is.EqualTo(new[] { "exec_in", "creature", "amount" }));
        Assert.That(heal.InputPins.Single(p => p.Id == "amount").DefaultValue, Is.EqualTo("10"));
        Assert.That(heal.OutputPins.Single().Id, Is.EqualTo("exec_out"));
        var distance = GetDistanceBetweenExecutor.Descriptor.CreateDefinition();
        Assert.That(distance.InputPins.All(p => p.DataType != GlyphDataType.Exec), Is.True);
        Assert.That(distance.OutputPins.Single().Id, Is.EqualTo("distance"));
        Assert.That(_compiler.Catalog.Find("Object.get_distance")!.ReturnType, Is.EqualTo(GlyphTypeSymbol.Float));
        var predicate = _compiler.Catalog.Find("skill_check")!;
        Assert.That(predicate.Strategy, Is.EqualTo(GlyphLoweringStrategy.PredicateBranch));
        Assert.That(predicate.ReturnType, Is.EqualTo(GlyphTypeSymbol.Bool));
        Assert.That(predicate.Definition.OutputPins.Where(p => p.DataType == GlyphDataType.Exec).Select(p => p.Id),
            Is.EqualTo(new[] { "success", "failure" }));
        Assert.That(_compiler.Catalog.Find("set_progress")!.AllowedStages, Is.EqualTo(new[] { "started", "tick" }));
        Assert.That(_compiler.Catalog.Find("spawn.modify_count")!.Definition.RestrictToEventType,
            Is.EqualTo(GlyphEventType.BeforeGroupSpawn));
    }

    [Test] public void Every_advertised_function_and_receiver_binds_and_lowers_to_valid_IR()
    {
        var metadata = GlyphLanguageMetadata.Create(_compiler.Catalog);
        foreach (var function in metadata.Functions)
        {
            Assert.That(function.AvailableIn, Is.Not.Empty, function.Name);
            foreach (var scope in function.AvailableIn)
            {
                var fields = metadata.Contexts.Single(c => c.Event == scope.Event && c.Stage == scope.Stage).Fields;
                string Arguments(IEnumerable<GlyphParameterMetadataDto> parameters) => string.Join(", ", parameters.Select(p =>
                    p.TestValue(fields)));
                string call = function.Name + "(" + Arguments(function.Parameters) + ")";
                string body = Consume(call, function.Kind, function.ReturnType, fields);
                var program = Compile(scope, body, function.Name);
                Assert.That(program.CreateExecutionGraph().Nodes.Any(n => n.TypeId == _compiler.Catalog.Find(function.CanonicalName)!.Definition.TypeId), Is.True, function.Name + " was not lowered");
            }
        }
        foreach (var receiver in metadata.ReceiverMethods)
            foreach (var scope in receiver.AvailableIn)
            {
                var fields = metadata.Contexts.Single(c => c.Event == scope.Event && c.Stage == scope.Stage).Fields;
                string first = new GlyphParameterMetadataDto("receiver", "Receiver", receiver.ReceiverType, true, null).TestValue(fields);
                string args = string.Join(", ", receiver.Parameters.Select(p => p.TestValue(fields)));
                string call = "(" + first + ")." + receiver.Name + "(" + args + ")";
                Compile(scope, Consume(call, receiver.Kind, receiver.ReturnType, fields), receiver.Name);
            }
    }

    private static string Consume(string call, string kind, string returnType, IReadOnlyList<GlyphFieldMetadataDto> fields)
    {
        if (kind == "Action" && returnType == "Void") return call;
        if (kind == "PredicateBranch" || returnType == "Bool") return "if " + call + " { }";
        string obj = fields.First(f => f.Type == "Object" && f.Name.StartsWith("context.")).Name;
        return returnType switch
        {
            "Int" or "Float" => $"nwn.set_local_float({obj}, \"probe\", {call})",
            "String" => $"nwn.set_local_string({obj}, \"probe\", {call})",
            "Object" => $"nwn.set_local_object({obj}, \"probe\", {call})",
            "Location" => $"nwn.set_local_location({obj}, \"probe\", {call})",
            "Effect" => $"nwn.apply_effect({obj}, {call})",
            "List<Object>" => $"foreach element in {call} {{ nwn.set_local_int(element, \"probe\", 1) }}",
            "List<Effect>" => $"foreach element in {call} {{ nwn.remove_effect({obj}, element) }}",
            _ => throw new InvalidOperationException("Add a conformance consumer for " + returnType)
        };
    }

    private Runtime.Programs.GlyphExecutable Compile(GlyphAvailabilityDto scope, string body, string label)
    {
        if (scope.Stage != null) body = scope.Stage + " { " + body + " }";
        var result = _compiler.Compile("glyph probe : " + scope.Event + " { " + body + " }");
        Assert.That(result.Success, Is.True, label + "/" + scope + ": " + string.Join(", ", result.Diagnostics));
        return result.Executable!;
    }

    [Test] public async Task Every_schema_drives_outputs_getters_and_metadata_from_typed_context()
    {
        foreach (var def in _registry.GetAll().Where(d => d.ContextSchema != null))
        {
            var context = new GlyphExecutionContext { Graph = new GlyphGraph(), SpawnCount = 7, TriggeringPlayer = 42,
                CharacterId = "character", TraitTag = "trait", TargetCreature = 12,
                InteractionCreature = 9, InteractionProgress = 3, InteractionRequiredRounds = 8 };
            var node = new GlyphNodeInstance { TypeId = def.TypeId };
            var result = await _executors.Single(e => e.TypeId == def.TypeId).ExecuteAsync(node, context, _ => Task.FromResult<object?>(null));
            Assert.That(result.OutputValues.Keys, Is.EquivalentTo(def.ContextSchema!.Fields.Select(f => f.PinId)));
            foreach (var field in def.ContextSchema.Fields)
            {
                var getter = _executors.Single(e => e.TypeId == $"context.{def.TypeId}.{field.PinId}");
                var read = await getter.ExecuteAsync(new() { TypeId = getter.TypeId }, context, _ => Task.FromResult<object?>(null));
                Assert.That(read.OutputValues["value"], Is.EqualTo(result.OutputValues[field.PinId]), def.TypeId + "/" + field.PinId);
            }
            var empty = new GlyphExecutionContext { Graph = new GlyphGraph() };
            Assert.DoesNotThrow(() => def.ContextSchema.Read(empty));
            Assert.That(empty.Get<EncounterGlyphContext>(), Is.Null);
            Assert.That(empty.Get<InteractionGlyphContext>(), Is.Null);
        }
    }

    [Test] public void Typed_capabilities_and_legacy_properties_share_one_state()
    {
        var context = new GlyphExecutionContext { Graph = new GlyphGraph() };
        Assert.That(context.Get<InteractionGlyphContext>(), Is.Null);
        var interaction = new InteractionGlyphContext { InteractionProgress = 11 };
        context.Set(interaction);
        Assert.That(context.InteractionProgress, Is.EqualTo(11));
        context.InteractionProgress = 17;
        Assert.That(interaction.InteractionProgress, Is.EqualTo(17));
        Assert.That(context.Get<EncounterGlyphContext>(), Is.Null);
    }

    [Test] public void Module_factories_can_close_over_dependencies_without_a_service_locator()
    {
        GlyphModuleBuilder builder = new();
        new IndustryGlyphModule().Configure(builder);
        builder.Add(() => new DependencyExecutor("test.dependency"));
        Assert.That(builder.Build().Select(e => e.TypeId), Does.Contain("industry.get_level"));
        Assert.That(builder.Build().Select(e => e.TypeId), Does.Contain("test.dependency"));
        builder.Add(() => new DependencyExecutor("test.dependency"));
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [TestCase("output", "missing return output")]
    [TestCase("receiver", "receiver type")]
    [TestCase("default", "invalid Int default")]
    [TestCase("stage", "unknown/unavailable stage")]
    [TestCase("alias", "unknown target")]
    public void Broken_contracts_fail_conformance(string fault, string expected)
    {
        var descriptor = new GlyphIntrinsicDescriptor
        {
            TypeId = "test.broken", DisplayName = "Broken", Category = "Tests",
            Parameters = [Pins.In("value", "Value", GlyphDataType.Int, fault == "default" ? "garbage" : "1")],
            Results = [Pins.Out("result", "Result", GlyphDataType.Int)],
            Exports = [new("test_broken", fault == "output" ? "absent" : "result",
                AllowedStages: fault == "stage" ? ["missing"] : null,
                ReceiverMethods: fault == "receiver" ? ["broken"] : null,
                CallAliases: fault == "alias" ? [new("bad_alias", "missing", null)] : null)]
        };
        _registry.Register(descriptor.CreateDefinition());
        Assert.That(GlyphFeatureVerifier.Errors(_registry).Any(e => e.Contains(expected)), Is.True);
    }

    [Test] public void Dependency_modules_reach_runtime_compiler_and_editor_metadata()
    {
        var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry(), [new DependencyModule("dependency value")]);
        Assert.That(runtime.Compiler.Catalog.Find("module_value"), Is.Not.Null);
        Assert.That(GlyphLanguageMetadata.Create(runtime.Compiler.Catalog).Functions.Any(f => f.Name == "module_value"), Is.True);
        Assert.That(runtime.Compiler.Compile("glyph probe : interaction { tick { let x = module_value() } }").Success, Is.True);
    }

    private sealed class DependencyModule(string dependency) : IGlyphModule
    {
        public void Configure(GlyphModuleBuilder glyph) => glyph.Add(() => new DependencyExecutor("test.dependency", dependency));
    }

    [Test] public void Api_reference_is_stable_and_covers_aliases_receivers_context_and_writable_state()
    {
        var metadata = GlyphLanguageMetadata.Create(_compiler.Catalog);
        string first = GlyphApiReference.Generate(metadata);
        Assert.That(GlyphApiReference.Generate(metadata), Is.EqualTo(first));
        foreach (var function in metadata.Functions) Assert.That(first, Does.Contain("### `" + function.Name + "`"));
        foreach (var receiver in metadata.ReceiverMethods) Assert.That(first, Does.Contain(receiver.Name));
        Assert.That(first, Does.Contain("context.party_size"));
        Assert.That(first, Does.Contain("| status | String | set_status |"));
        Assert.That(metadata.WritableState.Single(s => s.Name == "status").AvailableIn.Select(s => s.Stage),
            Is.EquivalentTo(new[] { "started", "tick", "completed" }));
    }

    [Test] public void Unregistered_CLR_members_and_unavailable_context_remain_forbidden()
    {
        foreach (string expression in new[] { "player.Destroy()", "player.Area", "player.GetObjectVariable(\"x\")", "context.progress" })
            Assert.That(_compiler.Compile("glyph probe : interaction { attempted { let x = " + expression + " } }").Success, Is.False);
    }

    private sealed class DependencyExecutor(string id, string? value = null) : IGlyphNodeExecutor
    {
        public string TypeId => id;
        public GlyphNodeDefinition CreateDefinition() => new()
        {
            TypeId = id, DisplayName = id, Category = "Tests",
            OutputPins = [Pins.Out("value", "Value", GlyphDataType.String)],
            Intrinsics = [new("module_value", "value")]
        };
        public Task<GlyphNodeResult> ExecuteAsync(GlyphNodeInstance node, GlyphExecutionContext context,
            Func<string, Task<object?>> resolveInput) => Task.FromResult(GlyphNodeResult.Data(new() { ["value"] = value }));
    }
}

internal static class GlyphParameterTestValues
{
    public static string TestValue(this GlyphParameterMetadataDto parameter, IReadOnlyList<GlyphFieldMetadataDto> fields) => parameter.Type switch
    {
        "Object" => fields.First(f => f.Type == "Object" && f.Name.StartsWith("context.")).Name,
        "ObjectList" or "List<Object>" => "party.members()",
        "List<Effect>" => "nwn.effects(OBJECT.INVALID)",
        "Location" => "nwn.get_location(OBJECT.INVALID)",
        "Effect" => "effect.haste()",
        "String" => System.Text.Json.JsonSerializer.Serialize(parameter.DefaultValue ?? "test"),
        "Int" => "1",
        "Float" => "1.0",
        "Bool" => "true",
        _ => throw new InvalidOperationException("Add a representative value for " + parameter.Type)
    };
}
