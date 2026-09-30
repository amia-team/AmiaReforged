using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using NUnit.Framework;
using NWN.Core;
using System.Text.Json;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public sealed class NwnStandardLibraryTests
{
    private GlyphNodeDefinitionRegistry _registry = null!;
    private GlyphBootstrap _runtime = null!;
    [SetUp] public void Setup() { _registry = new(); _runtime = new(_registry); }
    private void Valid(string body)
    {
        var compiled = _runtime.Compiler.Compile("glyph nwn_test : interaction { completed { " + body + " } }");
        Assert.That(compiled.Success, Is.True, string.Join("\n", compiled.Diagnostics));
    }
    [Test] public void The_standard_surface_is_substantial_and_keeps_provenance()
    {
        Assert.That(GlyphNwnSurface.Bindings.Count(b => b.Status is "bound" or "adapted"), Is.GreaterThan(400));
        Assert.That(GlyphNwnSurface.Constants.Count, Is.GreaterThan(3000));
        var metadata = GlyphLanguageMetadata.Create(_runtime.Compiler.Catalog);
        Assert.That(metadata.Functions.Where(f => f.Name.StartsWith("nwn.")).All(f => f.Source != null), Is.True);
        Assert.That(metadata.ConstantDomains.Any(d => d.Name == "OBJECT_TYPE"), Is.True);
        Assert.That(metadata.Types, Does.Contain("Location").And.Contain("List<Effect>"));
    }
    [Test] public void Typed_locals_constants_locations_effects_and_creation_compose()
    {
        Valid("""
            let loc = player.get_location()
            let summoned = nwn.create_object(OBJECT_TYPE.CREATURE, "amia_restless_spirit", loc)
            summoned.set_name("Restless Spirit")
            summoned.set_local_int("uses", summoned.get_local_int("uses") + 1)
            summoned.set_local_float("scale", 1.5)
            summoned.set_local_string("role", "guardian")
            summoned.set_local_object("owner", player)
            summoned.set_local_location("home", loc)
            let home = summoned.get_local_location("home")
            let new_home = nwn.location(home.get_area(), home.get_x(), home.get_y(), home.get_z(), home.get_facing())
            summoned.jump_to_location(new_home)
            summoned.apply_effect(effect.visual_effect(VFX.DUR_AURA_PURPLE))
            summoned.apply_effect(effect.haste(), duration: 30.0)
            summoned.action_move_to_location(home)
            summoned.action_attack(player)
            summoned.clear_actions()
            summoned.delete_local_int("uses")
            summoned.delete_local_float("scale")
            summoned.delete_local_string("role")
            summoned.delete_local_object("owner")
            summoned.delete_local_location("home")
            summoned.destroy()
            """);
    }
    [Test] public void Inventory_area_and_effect_iteration_keep_their_element_types()
    {
        Valid("""
            foreach item in player.inventory() {
                if item.get_tag() == "quest_item" { item.destroy() }
            }
            foreach creature in nwn.objects_in_area(player.get_area(), OBJECT_TYPE.CREATURE) {
                if creature.get_distance(player) < 10.0 { creature.set_local_object("near_player", player) }
            }
            foreach aura in player.effects() {
                if nwn.get_effect_type(aura) == EFFECT_TYPE.HASTE { player.remove_effect(aura) }
            }
            """);
        var bad = _runtime.Compiler.Compile("glyph t : interaction { completed { foreach aura in player.effects() { aura.destroy() } } }");
        Assert.That(bad.Diagnostics.Any(d => d.Code == "GLYPH2004"), Is.True);
    }
    [TestCase("player.set_local_int(\"foo\", \"banana\")")]
    [TestCase("player.set_local_object(\"foo\", 1)")]
    [TestCase("player.set_local_location(\"foo\", player)")]
    [TestCase("player.apply_effect(player.get_location())")]
    public void Semantic_type_errors_are_compile_time_diagnostics(string body)
    {
        var result = _runtime.Compiler.Compile("glyph t : interaction { completed { " + body + " } }");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2004"), Is.True);
    }
    [Test] public void Existing_source_names_share_the_authoritative_runtime_identity()
    {
        foreach (var pair in new[] { ("distance", "nwn.get_distance_between"), ("Object.get_distance", "nwn.get_distance_between"), ("Object.is_player", "nwn.is_player"), ("set_name", "nwn.set_name"), ("creature.hp", "nwn.get_current_hit_points") })
            Assert.That(_runtime.Compiler.Catalog.Find(pair.Item1)!.Definition.TypeId, Is.EqualTo(_runtime.Compiler.Catalog.Find(pair.Item2)!.Definition.TypeId));
        Valid("let a = Object.nearest_object_by_type(player, \"door\") let b = player.get_nearest_object_by_type(\"door\") let c = nwn.get_nearest_object_by_type(player, OBJECT_TYPE.DOOR)");
    }
    [Test] public void Global_functions_constants_and_aggregate_types_are_available_to_event_programs()
    {
        var result = _runtime.Compiler.Compile("""
            const HASTE_KIND = EFFECT_TYPE.HASTE
            const Local.ONE : Int = 1
            fn get_kind(effect_value: Effect): Int = nwn.get_effect_type(effect_value)
            struct Holder { actor: Object }
            glyph globals : interaction { completed {
                let holder = Holder(player)
                if get_kind(effect.haste()) == HASTE_KIND { holder.actor.set_local_int("one", Local.ONE) }
            } }
            """);
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        Assert.That(_runtime.Compiler.Compile("fn recur(value: Int): Int = recur(value) glyph t : interaction { completed { let x = recur(1) } }").Diagnostics.Any(d => d.Code == "GLYPH2012"), Is.True);
    }
    [Test] public void Standard_document_and_environment_have_the_same_typed_constants()
    {
        var lexer = new GlyphLexer(GlyphStandardLibrary.SourceDocument(), "global.glyph");
        var parser = new GlyphParser(lexer.Lex());
        var syntax = parser.Parse();
        Assert.That(lexer.Diagnostics.Concat(parser.Diagnostics), Is.Empty);
        var parsed = GlyphGlobalEnvironment.FromDeclarations(syntax!.GlobalDeclarations, out var diagnostics);
        Assert.That(diagnostics, Is.Empty);
        Assert.That(parsed.ResolvedConstants.Count, Is.EqualTo(GlyphStandardLibrary.Environment.ResolvedConstants.Count));
        foreach (var constant in parsed.ResolvedConstants)
            Assert.That(constant.Value with { Span = GlyphStandardLibrary.Environment.ResolvedConstants[constant.Key].Span }, Is.EqualTo(GlyphStandardLibrary.Environment.ResolvedConstants[constant.Key]));
    }
    [Test] public void Supplied_global_preludes_compose_with_the_standard_library()
    {
        var parser = new GlyphParser(new GlyphLexer("fn haste_kind(): Int = EFFECT_TYPE.HASTE").Lex());
        var globals = GlyphGlobalEnvironment.FromDeclarations(parser.Parse()!.GlobalDeclarations, out _);
        var compiler = new GlyphCompiler(_registry, globals);
        var result = compiler.Compile("glyph t : interaction { completed { player.set_local_int(\"kind\", haste_kind()) } }");
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        Assert.That(new GlyphCompiler(_registry, GlyphStandardLibrary.Environment).Compile("glyph t : interaction { completed { } }").Success, Is.True);
    }
    [Test] public void Program_declarations_cannot_hide_errors_in_the_supplied_prelude()
    {
        var parser = new GlyphParser(new GlyphLexer("const BAD = UNKNOWN").Lex());
        var globals = GlyphGlobalEnvironment.FromDeclarations(parser.Parse()!.GlobalDeclarations, out _);
        var compiler = new GlyphCompiler(_registry, globals);
        var result = compiler.Compile("const GOOD = 1 glyph t : interaction { completed { } }");
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2013"), Is.True);
    }
    [Test] public void The_referenced_API_matches_the_reviewed_upgrade_snapshot()
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "GlyphStandard", "NWN_API_SNAPSHOT.json");
        var previous = JsonSerializer.Deserialize<GlyphNwnSnapshot>(File.ReadAllText(path), GlyphNwnCoverage.JsonOptions)!;
        Assert.That(GlyphNwnCoverage.Compare(previous), Is.Empty,
            "Review NWN dependency/binding changes with Glyph.Docs --compare-nwn before regenerating the snapshot.");
    }
    [Test] public void Upgrade_audit_reports_added_removed_changed_functions_and_constants()
    {
        var current = GlyphNwnCoverage.Current;
        var methods = current.Bindings.Skip(1).ToList();
        methods[0] = methods[0] with { Signature = "old signature", Status = "deferred" };
        methods.Add(new("RemovedMethod", "void RemovedMethod()", "excluded", null, ""));
        var constants = current.Constants.Skip(1).ToList();
        constants[0] = constants[0] with { Value = "old value" };
        constants.Add(new("REMOVED_CONSTANT", "int", "1", null));
        var differences = GlyphNwnCoverage.Compare(new("old version", methods, constants));
        foreach (string kind in new[] { "API version:", "Added function:", "Removed function:", "Changed function:", "Added constant:", "Removed constant:", "Changed constant:" })
            Assert.That(differences.Any(d => d.StartsWith(kind, StringComparison.Ordinal)), Is.True, kind);
    }
    [TestCase("2147483648")]
    [TestCase("1 - 2147483648")]
    public void Integer_overflow_remains_a_source_diagnostic(string expression)
    {
        Assert.That(_runtime.Compiler.Compile("glyph t : interaction { completed { player.set_local_int(\"x\", " + expression + ") } }").Success, Is.False);
    }
    [Test] public void Signed_constants_support_the_native_int32_boundary()
    {
        Valid("player.set_local_int(\"mask\", -2147483648) player.set_local_int(\"bonus\", DAMAGE_BONUS.VALUE_1)");
        Assert.That(GlyphStandardLibrary.Environment.GetResolvedConstant("DAMAGE_TYPE.CUSTOM19")!.Value, Is.EqualTo(int.MinValue));
    }
    [Test] public void Upgrade_audit_handles_overloaded_native_members()
    {
        var current = GlyphNwnCoverage.Current;
        var original = current.Bindings[0];
        var previous = new GlyphNwnSnapshot(current.ApiVersion,
            current.Bindings.Append(original with { Signature = "old extra overload" }).ToArray(), current.Constants);
        Assert.That(GlyphNwnCoverage.Compare(previous), Is.EqualTo(new[] { "Removed function: old extra overload" }));
    }
    [Test] public void Exclusions_do_not_leak_callbacks_or_arbitrary_CLR_access()
    {
        Assert.That(_runtime.Compiler.Catalog.Find("nwn.delay_command"), Is.Null);
        Assert.That(GlyphNwnSurface.Bindings.Single(b => b.Member == "DelayCommand").Status, Is.EqualTo("deferred"));
        foreach (string expression in new[] { "System.IO.File.ReadAllText(\"x\")", "player.GetType()", "nwn.execute_script_chunk(\"x\")" })
            Assert.That(_runtime.Compiler.Compile("glyph t : interaction { completed { let x = " + expression + " } }").Success, Is.False);
    }
    [Test] public async Task Invalid_handles_and_typed_engine_values_round_trip_without_a_server()
    {
        Assert.That(GlyphNwnValue.NormalizeObject(0), Is.EqualTo(NWScript.OBJECT_INVALID));
        var location = new GlyphNwnLocation(new IntPtr(5));
        var effect = new GlyphNwnEffect(new IntPtr(6));
        GlyphNodeContext cx = new(new() { TypeId = "test" }, new() { Graph = new() }, pin => Task.FromResult<object?>(pin == "location" ? location : pin == "effect" ? effect : null));
        Assert.That(await cx.In<GlyphNwnLocation>("location"), Is.EqualTo(location));
        Assert.That(await cx.In<GlyphNwnEffect>("effect"), Is.EqualTo(effect));
        Assert.That(await cx.InObject("missing"), Is.EqualTo(NWScript.OBJECT_INVALID));
        foreach (IGlyphNodeExecutor executor in new IGlyphNodeExecutor[] { new InventoryExecutor(), new EffectsExecutor(), new ObjectsInAreaExecutor() })
        {
            var values = await executor.ExecuteAsync(new() { TypeId = executor.TypeId }, new() { Graph = new() }, _ => Task.FromResult<object?>(NWScript.OBJECT_INVALID));
            Assert.That((System.Collections.IEnumerable)values.OutputValues["value"]!, Is.Empty);
        }
    }
}
