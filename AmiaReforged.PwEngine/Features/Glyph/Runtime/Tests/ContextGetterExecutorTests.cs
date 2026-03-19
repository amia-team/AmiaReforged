using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Context;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

[TestFixture]
public class ContextGetterExecutorTests
{
    private const string SourceTypeId = "stage.interaction_attempted";
    private const string SourceDisplayName = "Attempted";
    private const string PinId = "character_id";
    private const string PinDisplayName = "Character ID";

    private static ContextPinDescriptor MakePin(Func<GlyphExecutionContext, object?>? accessor = null) =>
        new(PinId, PinDisplayName, GlyphDataType.String, accessor ?? (ctx => ctx.CharacterId));

    private static ContextGetterExecutor MakeExecutor(ContextPinDescriptor? pin = null) =>
        new(SourceTypeId, SourceDisplayName, pin ?? MakePin(),
            GlyphEventType.InteractionPipeline, GlyphScriptCategory.Interaction);

    private static GlyphExecutionContext CreateContext() => new()
    {
        Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
        MaxExecutionSteps = 100,
    };

    // ==================== Definition Tests ====================

    [Test]
    public void TypeId_is_source_qualified()
    {
        ContextGetterExecutor executor = MakeExecutor();

        executor.TypeId.Should().Be("context.stage.interaction_attempted.character_id");
    }

    [Test]
    public void Definition_has_context_getter_archetype()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.Archetype.Should().Be(GlyphNodeArchetype.ContextGetter);
    }

    [Test]
    public void Definition_display_name_is_source_qualified()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.DisplayName.Should().Be("Attempted: Character ID");
    }

    [Test]
    public void Definition_category_prefixed_with_context()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.Category.Should().Be("Context: Attempted");
    }

    [Test]
    public void Definition_has_no_input_pins()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.InputPins.Should().BeEmpty();
    }

    [Test]
    public void Definition_has_single_output_pin_named_value()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.OutputPins.Should().HaveCount(1);
        GlyphPin output = def.OutputPins[0];
        output.Id.Should().Be("value");
        output.Name.Should().Be(PinDisplayName);
        output.DataType.Should().Be(GlyphDataType.String);
        output.Direction.Should().Be(GlyphPinDirection.Output);
    }

    [Test]
    public void Definition_sets_context_source_type_id()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.ContextSourceTypeId.Should().Be(SourceTypeId);
    }

    [Test]
    public void Definition_color_class_is_node_context()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.ColorClass.Should().Be("node-context");
    }

    [Test]
    public void Definition_propagates_restrict_to_event_type()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.RestrictToEventType.Should().Be(GlyphEventType.InteractionPipeline);
    }

    [Test]
    public void Definition_propagates_script_category()
    {
        GlyphNodeDefinition def = MakeExecutor().CreateDefinition();

        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
    }

    // ==================== Execution Tests ====================

    [Test]
    public async Task Execute_reads_value_from_context_via_accessor()
    {
        ContextGetterExecutor executor = MakeExecutor();
        GlyphNodeInstance node = new() { TypeId = executor.TypeId };
        GlyphExecutionContext context = CreateContext();
        context.CharacterId = "PC_12345";

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues.Should().ContainKey("value");
        result.OutputValues["value"].Should().Be("PC_12345");
    }

    [Test]
    public async Task Execute_returns_null_when_context_value_not_set()
    {
        ContextGetterExecutor executor = MakeExecutor();
        GlyphNodeInstance node = new() { TypeId = executor.TypeId };
        GlyphExecutionContext context = CreateContext();
        // CharacterId not set → default null

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues["value"].Should().BeNull();
    }

    [Test]
    public async Task Execute_uses_custom_accessor()
    {
        ContextPinDescriptor pin = new("custom_pin", "Custom", GlyphDataType.Int, ctx => 42);
        ContextGetterExecutor executor = new(SourceTypeId, SourceDisplayName, pin,
            null, null);
        GlyphNodeInstance node = new() { TypeId = executor.TypeId };
        GlyphExecutionContext context = CreateContext();

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues["value"].Should().Be(42);
    }

    [Test]
    public async Task Execute_does_not_set_next_exec_pin()
    {
        ContextGetterExecutor executor = MakeExecutor();
        GlyphNodeInstance node = new() { TypeId = executor.TypeId };
        GlyphExecutionContext context = CreateContext();

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.NextExecPinId.Should().BeNull();
    }
}
