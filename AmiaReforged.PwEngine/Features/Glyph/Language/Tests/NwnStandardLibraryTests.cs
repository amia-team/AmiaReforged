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
            let loc = nwn.get_location(player)
            let summoned = nwn.create_object(OBJECT_TYPE.CREATURE, "amia_restless_spirit", loc)
            nwn.set_name(summoned, "Restless Spirit")
            nwn.set_local_int(summoned, "uses", nwn.get_local_int(summoned, "uses") + 1)
            nwn.set_local_float(summoned, "scale", 1.5)
            nwn.set_local_string(summoned, "role", "guardian")
            nwn.set_local_object(summoned, "owner", player)
            nwn.set_local_location(summoned, "home", loc)
            let home = nwn.get_local_location(summoned, "home")
            let new_home = nwn.location(home.get_area(), home.get_x(), home.get_y(), home.get_z(), home.get_facing())
            nwn.jump_to_location(summoned, new_home)
            nwn.apply_effect_to_object(summoned, effect.visual_effect(VFX.DUR_AURA_PURPLE))
            nwn.apply_effect_to_object(summoned, effect.haste(), duration: 30.0)
            nwn.action_move_to_location(summoned, home)
            nwn.action_attack(summoned, player)
            nwn.clear_all_actions(summoned)
            nwn.delete_local_int(summoned, "uses")
            nwn.delete_local_float(summoned, "scale")
            nwn.delete_local_string(summoned, "role")
            nwn.delete_local_object(summoned, "owner")
            nwn.delete_local_location(summoned, "home")
            nwn.destroy_object(summoned)
            """);
    }
    [Test] public void Inventory_area_and_effect_iteration_keep_their_element_types()
    {
        Valid("""
            foreach item in nwn.inventory(player) {
                if nwn.get_tag(item) == "quest_item" { nwn.destroy_object(item) }
            }
            foreach creature in nwn.objects_in_area(nwn.get_area(player), OBJECT_TYPE.CREATURE) {
                if nwn.get_distance_between(creature, player) < 10.0 { nwn.set_local_object(creature, "near_player", player) }
            }
            foreach aura in nwn.effects(player) {
                if nwn.get_effect_type(aura) == EFFECT_TYPE.HASTE { nwn.remove_effect(player, aura) }
            }
            """);
        var bad = _runtime.Compiler.Compile("glyph t : interaction { completed { foreach aura in nwn.effects(player) { nwn.destroy_object(aura) } } }");
        Assert.That(bad.Diagnostics.Any(d => d.Code == "GLYPH2004"), Is.True);
    }
    [TestCase("nwn.set_local_int(player, \"foo\", \"banana\")")]
    [TestCase("nwn.set_local_object(player, \"foo\", 1)")]
    [TestCase("nwn.set_local_location(player, \"foo\", player)")]
    [TestCase("nwn.apply_effect_to_object(player, nwn.get_location(player))")]
    public void Semantic_type_errors_are_compile_time_diagnostics(string body)
    {
        var result = _runtime.Compiler.Compile("glyph t : interaction { completed { " + body + " } }");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2004"), Is.True);
    }
    [Test] public void Removed_source_aliases_keep_the_authoritative_runtime_identity()
    {
        var catalog = _runtime.Compiler.Catalog;
        foreach (string alias in new[] { "distance", "Object.get_distance", "Object.is_player", "Object.nearest_object_by_type", "set_name", "creature.hp", "creature.ac", "creature.name" })
            Assert.That(catalog.Find(alias), Is.Null, alias);
        Assert.That(catalog.Find("nwn.get_distance_between")!.Definition.TypeId, Is.EqualTo("getter.distance_between"));
        Assert.That(catalog.Find("nwn.is_player")!.Definition.TypeId, Is.EqualTo("getter.is_player"));
        Assert.That(catalog.Find("nwn.nearest_object_by_kind")!.Definition.TypeId, Is.EqualTo("getter.nearest_object_by_type"));
        Valid("let a = nwn.nearest_object_by_kind(player, \"door\") let b = nwn.get_nearest_object_by_type(player, OBJECT_TYPE.DOOR)");
    }
    [Test] public void Global_functions_constants_and_aggregate_types_are_available_to_event_programs()
    {
        var result = _runtime.Compiler.Compile("""
            const HASTE_KIND = EFFECT_TYPE.HASTE
            const Local.ONE : Int = 1
            fn get_kind(effect_value: Effect): Int = nwn.get_effect_type(effect_value)
            struct Holder { actor: Object, }
            glyph globals : interaction { completed {
                let holder = Holder(player)
                if get_kind(effect.haste()) == HASTE_KIND { nwn.set_local_int(holder.actor, "one", Local.ONE) }
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
        var result = compiler.Compile("glyph t : interaction { completed { nwn.set_local_int(player, \"kind\", haste_kind()) } }");
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
        Assert.That(_runtime.Compiler.Compile("glyph t : interaction { completed { nwn.set_local_int(player, \"x\", " + expression + ") } }").Success, Is.False);
    }
    [Test] public void Signed_constants_support_the_native_int32_boundary()
    {
        Valid("nwn.set_local_int(player, \"mask\", -2147483648) nwn.set_local_int(player, \"bonus\", DAMAGE_BONUS.VALUE_1)");
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
