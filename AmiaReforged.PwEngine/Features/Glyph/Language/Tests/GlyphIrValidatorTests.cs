using AmiaReforged.PwEngine.Features.Glyph.Core;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlyphIrValidatorTests
{
    private GlyphNodeDefinitionRegistry _registry = null!;
    private GlyphIrValidator _validator = null!;
    [SetUp] public void Setup() { _registry = new(); _ = new GlyphBootstrap(_registry); _validator = new(_registry); }
    private static GlyphGraph Ir() => new()
    {
        EventType = GlyphEventType.BeforeGroupSpawn,
        Nodes = [new() { TypeId = "event.before_group_spawn" }, new() { TypeId = "action.modify_spawn_count" }, new() { TypeId = "constant.int" }]
    };
    private static GlyphEdge Edge(GlyphNodeInstance from, string output, GlyphNodeInstance to, string input) => new()
    { SourceNodeId = from.InstanceId, SourcePinId = output, TargetNodeId = to.InstanceId, TargetPinId = input };
    [Test] public void Rejects_unknown_operation_missing_entry_and_duplicate_ids()
    {
        GlyphNodeInstance node = new() { TypeId = "unknown" };
        var errors = _validator.Validate(new() { Nodes = [node, node] });
        Assert.That(errors.Select(e => e.Code), Does.Contain("GLYPH4001").And.Contain("GLYPH4002").And.Contain("GLYPH4006"));
    }
    [TestCase("exec_in", "exec_in", "GLYPH4008")]
    [TestCase("missing", "exec_in", "GLYPH4008")]
    [TestCase("exec_out", "new_count", "GLYPH4010")]
    public void Rejects_invalid_pins_and_types(string output, string input, string code)
    {
        var ir = Ir(); ir.Edges.Add(Edge(ir.Nodes[0], output, ir.Nodes[1], input));
        Assert.That(_validator.Validate(ir).Select(e => e.Code), Does.Contain(code));
    }
    [Test] public void Rejects_multiple_data_sources_and_dangling_edges()
    {
        var ir = Ir(); ir.Edges.Add(Edge(ir.Nodes[2], "out", ir.Nodes[1], "new_count"));
        ir.Edges.Add(Edge(ir.Nodes[2], "out", ir.Nodes[1], "new_count"));
        ir.Edges.Add(Edge(new() { TypeId = "constant.int" }, "out", ir.Nodes[1], "new_count"));
        Assert.That(_validator.Validate(ir).Select(e => e.Code), Does.Contain("GLYPH4011").And.Contain("GLYPH4007"));
    }
    [Test] public void Rejects_event_category_singleton_and_lazy_data_cycles()
    {
        var ir = Ir(); ir.Nodes.Add(new() { TypeId = "event.before_group_spawn" });
        ir.Nodes.Add(new() { TypeId = "event.on_trait_granted" });
        var arithmetic = new GlyphNodeInstance { TypeId = "math.math_op" }; ir.Nodes.Add(arithmetic);
        ir.Edges.Add(Edge(arithmetic, "result", arithmetic, "a"));
        Assert.That(_validator.Validate(ir).Select(e => e.Code), Does.Contain("GLYPH4003").And.Contain("GLYPH4004").And.Contain("GLYPH4005").And.Contain("GLYPH4013"));
    }
    [Test] public void Every_language_symbol_uses_registered_signature_metadata()
    {
        var catalog = new Binding.GlyphLanguageCatalog(_registry);
        Assert.That(catalog.Symbols.Select(s => s.Name).Distinct().Count(), Is.EqualTo(catalog.Symbols.Count));
        foreach (var symbol in catalog.Symbols)
        {
            Assert.That(symbol.Definition, Is.SameAs(_registry.Get(symbol.Definition.TypeId)));
            Assert.That(symbol.Parameters.All(p => symbol.Definition.InputPins.Contains(p)), Is.True);
            if (symbol.OutputPin != null)
                Assert.That(symbol.ReturnType.RuntimeType, Is.EqualTo(symbol.Definition.OutputPins.Single(p => p.Id == symbol.OutputPin).DataType));
        }
    }
    [Test] public void An_action_result_requires_its_producer_on_every_execution_path()
    {
        var entry = new GlyphNodeInstance { TypeId = "event.before_group_spawn" };
        var create = new GlyphNodeInstance { TypeId = "nwn.create_area" };
        var consume = new GlyphNodeInstance { TypeId = "nwn.set_local_int" };
        GlyphGraph graph = new() { EventType = GlyphEventType.BeforeGroupSpawn, Nodes = [entry, create, consume] };
        graph.Edges.Add(Edge(create, "value", consume, "object"));
        graph.Edges.Add(Edge(entry, "exec_out", consume, "exec_in"));
        Assert.That(_validator.Validate(graph).Select(d => d.Code), Does.Contain("GLYPH4014"));
        graph.Edges.RemoveAt(1);
        graph.Edges.Add(Edge(entry, "exec_out", create, "exec_in"));
        graph.Edges.Add(Edge(create, "exec_out", consume, "exec_in"));
        Assert.That(_validator.Validate(graph), Is.Empty);
    }
    [Test] public void Effect_lists_cannot_connect_to_object_iteration()
    {
        var graph = Ir();
        var effects = new GlyphNodeInstance { TypeId = "nwn.effects" };
        var loop = new GlyphNodeInstance { TypeId = "flow.for_each" };
        graph.Nodes.AddRange([effects, loop]);
        graph.Edges.Add(Edge(effects, "value", loop, "list"));
        Assert.That(_validator.Validate(graph).Select(d => d.Code), Does.Contain("GLYPH4010"));
    }
}
