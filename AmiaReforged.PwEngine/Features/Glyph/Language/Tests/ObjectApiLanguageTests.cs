using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public sealed class ObjectApiLanguageTests
{
    private GlyphBootstrap _runtime = null!;
    [SetUp] public void Setup() => _runtime = new(new GlyphNodeDefinitionRegistry());

    [TestCase("get_ability_score", "ABILITY.STRENGTH")]
    [TestCase("get_item_stack_size", "")]
    [TestCase("get_locked", "")]
    [TestCase("get_store_gold", "")]
    [TestCase("get_location", "")]
    [TestCase("get_tag", "")]
    [TestCase("set_local_int", "\"state\", 5")]
    [TestCase("action_attack", "player")]
    [TestCase("destroy", "")]
    [TestCase("is_player", "")]
    public void Generic_Object_has_no_raw_NWScript_receiver(string method, string arguments)
    {
        var catalog = _runtime.Compiler.Catalog;
        Assert.That(catalog.TryResolveReceiverMethod(method, GlyphDataType.NwObject, out _), Is.False);
        var result = _runtime.Compiler.Compile($"glyph t : interaction {{ completed {{ player.{method}({arguments}) }} }}");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == "GLYPH2002"), Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test] public void All_registered_receivers_are_deliberate_typed_values()
    {
        var catalog = _runtime.Compiler.Catalog;
        Assert.That(catalog.ReceiverMethods, Is.Not.Empty);
        Assert.That(catalog.ReceiverMethods.All(r => r.ReceiverType is GlyphDataType.Location or GlyphDataType.Effect &&
            r.Policy == GlyphReceiverPolicy.LanguageValue), Is.True);
        Assert.That(catalog.ReceiverMethods.Count, Is.EqualTo(21));
    }

    [TestCase("let score = nwn.get_ability_score(player, ABILITY.STRENGTH)")]
    [TestCase("let count = nwn.get_item_stack_size(player)")]
    [TestCase("let locked = nwn.get_locked(player)")]
    [TestCase("nwn.set_local_int(player, \"state\", 5)")]
    [TestCase("nwn.action_attack(player, context.creature)")]
    [TestCase("nwn.action_move_to_location(player, nwn.get_location(context.creature))")]
    [TestCase("nwn.clear_all_actions(player)")]
    [TestCase("let d = nwn.get_distance_between(player, context.creature)")]
    [TestCase("let target = nwn.get_nearest_object_by_type(player, OBJECT_TYPE.CREATURE) if nwn.is_player(target) { message(target, \"found\") }")]
    [TestCase("let location = nwn.get_location(player) let x = location.get_x() let area = location.get_area() let facing = location.get_facing()")]
    [TestCase("let aura = effect.haste() let kind = aura.get_effect_type() let duration = aura.get_effect_duration()")]
    [TestCase("if player.has_item(\"key\") { message(player, \"found\") }")]
    [TestCase("if player.has_knowledge(\"mining.basic\") { nwn.set_local_int(player, \"known\", 1) }")]
    public void Procedures_typed_values_and_domain_apis_compose(string body)
    {
        var result = _runtime.Compiler.Compile("glyph t : interaction { completed { " + body + " } }");
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
    }

    [Test] public void Canonical_queries_lower_to_existing_runtime_nodes()
    {
        var result = _runtime.Compiler.Compile("glyph t : interaction { tick { if nwn.is_player(player) && nwn.get_distance_between(player, context.creature) > 10.0 { message(player, \"far\") } } }");
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
        var nodes = result.Executable!.CreateExecutionGraph().Nodes.Select(n => n.TypeId).ToArray();
        Assert.That(nodes, Does.Contain("getter.distance_between").And.Contain("getter.is_player"));
    }

    [TestCase("nwn.is_player(42)", "GLYPH2004")]
    [TestCase("nwn.is_player()", "GLYPH2003")]
    [TestCase("nwn.is_player(player, player)", "GLYPH2003")]
    [TestCase("nwn.get_nearest_object_by_type(42)", "GLYPH2004")]
    [TestCase("nwn.action_attack()", "GLYPH2003")]
    [TestCase("let loc = nwn.get_location(player) loc.get_x(player)", "GLYPH2003")]
    [TestCase("player.get_x()", "GLYPH2004")]
    public void Procedure_and_typed_receiver_signatures_are_checked(string body, string code)
    {
        var result = _runtime.Compiler.Compile("glyph t : interaction { completed { " + body + " } }");
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True, string.Join("\n", result.Diagnostics));
    }
}
