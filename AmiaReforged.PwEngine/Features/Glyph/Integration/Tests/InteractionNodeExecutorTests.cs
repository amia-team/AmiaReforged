using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class InteractionNodeExecutorTests
{
    // ==================== BlockInteractionExecutor ====================

    [Test]
    public async Task Block_executor_sets_context_flags_and_message()
    {
        // Given
        BlockInteractionExecutor executor = new BlockInteractionExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = BlockInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionAttempted);

        // When
        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "message" ? "Custom block msg" : null));

        // Then
        context.ShouldBlockInteraction.Should().BeTrue();
        context.BlockInteractionMessage.Should().Be("Custom block msg");
        result.NextExecPinId.Should().Be("exec_out");
    }

    [Test]
    public async Task Block_executor_uses_default_message_when_input_is_null()
    {
        BlockInteractionExecutor executor = new BlockInteractionExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = BlockInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionAttempted);

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        context.ShouldBlockInteraction.Should().BeTrue();
        context.BlockInteractionMessage.Should().Be("Interaction blocked by script");
        result.NextExecPinId.Should().Be("exec_out");
    }

    // ==================== CancelInteractionExecutor ====================

    [Test]
    public async Task Cancel_executor_sets_context_flags_and_message()
    {
        CancelInteractionExecutor executor = new CancelInteractionExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = CancelInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionTick);

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "message" ? "Script says stop" : null));

        context.ShouldCancelInteraction.Should().BeTrue();
        context.CancelInteractionMessage.Should().Be("Script says stop");
        result.NextExecPinId.Should().Be("exec_out");
    }

    [Test]
    public async Task Cancel_executor_uses_default_message_when_input_is_null()
    {
        CancelInteractionExecutor executor = new CancelInteractionExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = CancelInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionTick);

        await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(null));

        context.ShouldCancelInteraction.Should().BeTrue();
        context.CancelInteractionMessage.Should().Be("Interaction cancelled by script");
    }

    // ==================== GetInteractionInfoExecutor ====================

    [Test]
    public async Task GetInfo_executor_reads_context_values()
    {
        GetInteractionInfoExecutor executor = new GetInteractionInfoExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = GetInteractionInfoExecutor.NodeTypeId };

        Guid targetId = Guid.NewGuid();
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionTick);
        context.InteractionTag = "mining";
        context.InteractionTargetId = targetId;
        context.InteractionTargetMode = "Placeable";
        context.InteractionAreaResRef = "area_mines";
        context.InteractionProgress = 3;
        context.InteractionRequiredRounds = 5;
        context.InteractionProficiency = "expert";

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["tag"].Should().Be("mining");
        result.OutputValues["target_id"].Should().Be(targetId.ToString());
        result.OutputValues["target_mode"].Should().Be("Placeable");
        result.OutputValues["area_resref"].Should().Be("area_mines");
        result.OutputValues["progress"].Should().Be(3);
        result.OutputValues["required_rounds"].Should().Be(5);
        result.OutputValues["proficiency"].Should().Be("expert");
    }

    // ==================== Event Entry Node Executors ====================

    [Test]
    public async Task Attempted_entry_executor_outputs_context_values()
    {
        OnInteractionAttemptedEventExecutor executor = new OnInteractionAttemptedEventExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = OnInteractionAttemptedEventExecutor.NodeTypeId };

        Guid targetId = Guid.NewGuid();
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionAttempted);
        context.CharacterId = "char-123";
        context.InteractionTag = "prospecting";
        context.InteractionTargetId = targetId;
        context.InteractionTargetMode = "Node";
        context.InteractionAreaResRef = "area_wastes";
        context.InteractionProficiency = "novice";

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.NextExecPinId.Should().Be("exec_out");
        result.OutputValues!["character_id"].Should().Be("char-123");
        result.OutputValues["interaction_tag"].Should().Be("prospecting");
        result.OutputValues["target_id"].Should().Be(targetId.ToString());
        result.OutputValues["target_mode"].Should().Be("Node");
        result.OutputValues["area_resref"].Should().Be("area_wastes");
        result.OutputValues["proficiency"].Should().Be("novice");
    }

    [Test]
    public async Task Started_entry_executor_includes_session_and_rounds()
    {
        OnInteractionStartedEventExecutor executor = new OnInteractionStartedEventExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = OnInteractionStartedEventExecutor.NodeTypeId };

        Guid sessionId = Guid.NewGuid();
        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionStarted);
        context.CharacterId = "char-456";
        context.InteractionTag = "mining";
        context.InteractionSessionId = sessionId;
        context.InteractionRequiredRounds = 5;

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues!["session_id"].Should().Be(sessionId.ToString());
        result.OutputValues["required_rounds"].Should().Be(5);
    }

    [Test]
    public async Task Tick_entry_executor_includes_progress()
    {
        OnInteractionTickEventExecutor executor = new OnInteractionTickEventExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = OnInteractionTickEventExecutor.NodeTypeId };

        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionTick);
        context.InteractionProgress = 3;

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues!["progress"].Should().Be(3);
    }

    [Test]
    public async Task Completed_entry_executor_includes_response_tag()
    {
        OnInteractionCompletedEventExecutor executor = new OnInteractionCompletedEventExecutor();
        GlyphNodeInstance node = new GlyphNodeInstance { TypeId = OnInteractionCompletedEventExecutor.NodeTypeId };

        GlyphExecutionContext context = CreateContext(GlyphEventType.OnInteractionCompleted);
        context.InteractionResponseTag = "success_rare_gem";

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues!["response_tag"].Should().Be("success_rare_gem");
    }

    // ==================== Node Definition Tests ====================

    [Test]
    public void Block_executor_definition_is_restricted_to_OnInteractionAttempted()
    {
        GlyphNodeDefinition def = BlockInteractionExecutor.CreateDefinition();
        def.TypeId.Should().Be("interaction.block");
        def.RestrictToEventType.Should().Be(GlyphEventType.OnInteractionAttempted);
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
    }

    [Test]
    public void Cancel_executor_definition_is_restricted_to_OnInteractionTick()
    {
        GlyphNodeDefinition def = CancelInteractionExecutor.CreateDefinition();
        def.TypeId.Should().Be("interaction.cancel");
        def.RestrictToEventType.Should().Be(GlyphEventType.OnInteractionTick);
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
    }

    [Test]
    public void GetInfo_executor_definition_has_correct_output_pins()
    {
        GlyphNodeDefinition def = GetInteractionInfoExecutor.CreateDefinition();
        def.TypeId.Should().Be("interaction.get_info");
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
        def.OutputPins.Should().Contain(p => p.Id == "tag");
        def.OutputPins.Should().Contain(p => p.Id == "target_id");
        def.OutputPins.Should().Contain(p => p.Id == "progress");
    }

    // ==================== Helpers ====================

    private static GlyphExecutionContext CreateContext(GlyphEventType eventType) => new()
    {
        Graph = new GlyphGraph { EventType = eventType, Name = "Test" },
        MaxExecutionSteps = 1000,
        EnableTracing = false
    };
}
