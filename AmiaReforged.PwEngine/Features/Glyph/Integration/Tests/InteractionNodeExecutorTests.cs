using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class InteractionNodeExecutorTests
{
    // ==================== FailInteractionExecutor ====================

    [Test]
    public async Task Fail_at_attempted_stage_sets_block_flag_and_message()
    {
        FailInteractionExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = FailInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreatePipelineContext();
        context.CurrentPipelineStage = InteractionAttemptedStageExecutor.NodeTypeId;

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "message" ? "Custom block msg" : null));

        context.ShouldBlockInteraction.Should().BeTrue();
        context.BlockInteractionMessage.Should().Be("Custom block msg");
        result.NextExecPinId.Should().BeNull("FailInteraction terminates the chain");
    }

    [Test]
    public async Task Fail_at_attempted_stage_uses_default_message_when_null()
    {
        FailInteractionExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = FailInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreatePipelineContext();
        context.CurrentPipelineStage = InteractionAttemptedStageExecutor.NodeTypeId;

        await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(null));

        context.ShouldBlockInteraction.Should().BeTrue();
        context.BlockInteractionMessage.Should().Be("Interaction failed");
    }

    [Test]
    public async Task Fail_at_tick_stage_sets_cancel_flag_and_message()
    {
        FailInteractionExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = FailInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreatePipelineContext();
        context.CurrentPipelineStage = InteractionTickStageExecutor.NodeTypeId;

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "message" ? "Script says stop" : null));

        context.ShouldCancelInteraction.Should().BeTrue();
        context.CancelInteractionMessage.Should().Be("Script says stop");
        result.NextExecPinId.Should().BeNull();
    }

    [Test]
    public async Task Fail_at_started_stage_sets_cancel_flag()
    {
        FailInteractionExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = FailInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreatePipelineContext();
        context.CurrentPipelineStage = InteractionStartedStageExecutor.NodeTypeId;

        await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(null));

        context.ShouldCancelInteraction.Should().BeTrue();
        context.ShouldBlockInteraction.Should().BeFalse();
    }

    [Test]
    public async Task Fail_at_completed_stage_sets_cancel_flag()
    {
        FailInteractionExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = FailInteractionExecutor.NodeTypeId };
        GlyphExecutionContext context = CreatePipelineContext();
        context.CurrentPipelineStage = InteractionCompletedStageExecutor.NodeTypeId;

        await executor.ExecuteAsync(node, context, _ => Task.FromResult<object?>(null));

        context.ShouldCancelInteraction.Should().BeTrue();
    }

    // ==================== Stage Executor Outputs ====================

    [Test]
    public async Task Attempted_stage_outputs_context_values()
    {
        InteractionAttemptedStageExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = InteractionAttemptedStageExecutor.NodeTypeId };

        Guid targetId = Guid.NewGuid();
        GlyphExecutionContext context = CreatePipelineContext();
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
    public async Task Started_stage_includes_session_and_rounds()
    {
        InteractionStartedStageExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = InteractionStartedStageExecutor.NodeTypeId };

        Guid sessionId = Guid.NewGuid();
        GlyphExecutionContext context = CreatePipelineContext();
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
    public async Task Tick_stage_includes_progress()
    {
        InteractionTickStageExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = InteractionTickStageExecutor.NodeTypeId };

        GlyphExecutionContext context = CreatePipelineContext();
        context.InteractionProgress = 3;

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues!["progress"].Should().Be(3);
    }

    [Test]
    public async Task Completed_stage_includes_response_tag()
    {
        InteractionCompletedStageExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = InteractionCompletedStageExecutor.NodeTypeId };

        GlyphExecutionContext context = CreatePipelineContext();
        context.InteractionResponseTag = "success_rare_gem";

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            _ => Task.FromResult<object?>(null));

        result.OutputValues!["response_tag"].Should().Be("success_rare_gem");
    }

    [Test]
    public async Task Attempted_stage_prefers_wired_input_over_context()
    {
        InteractionAttemptedStageExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = InteractionAttemptedStageExecutor.NodeTypeId };

        GlyphExecutionContext context = CreatePipelineContext();
        context.CharacterId = "context-char";
        context.InteractionTag = "context-tag";
        context.InteractionTargetId = Guid.NewGuid();

        // Simulate wired inputs overriding context values
        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => pin switch
            {
                "character_id" => Task.FromResult<object?>("wired-char"),
                "creature" => Task.FromResult<object?>("wired-creature"),
                "interaction_tag" => Task.FromResult<object?>("wired-tag"),
                "target_id" => Task.FromResult<object?>("wired-target"),
                _ => Task.FromResult<object?>(null)
            });

        result.OutputValues!["character_id"].Should().Be("wired-char");
        result.OutputValues["creature"].Should().Be("wired-creature");
        result.OutputValues["interaction_tag"].Should().Be("wired-tag");
        result.OutputValues["target_id"].Should().Be("wired-target");
    }

    // ==================== GetInteractionInfoExecutor ====================

    [Test]
    public async Task GetInfo_executor_reads_context_values()
    {
        GetInteractionInfoExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = GetInteractionInfoExecutor.NodeTypeId };

        Guid targetId = Guid.NewGuid();
        GlyphExecutionContext context = CreatePipelineContext();
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

    // ==================== Node Definition Tests ====================

    [Test]
    public void Fail_executor_definition_is_restricted_to_InteractionPipeline()
    {
        GlyphNodeDefinition def = new FailInteractionExecutor().CreateDefinition();
        def.TypeId.Should().Be("interaction.fail");
        def.RestrictToEventType.Should().Be(GlyphEventType.InteractionPipeline);
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
    }

    [Test]
    public void Stage_definitions_are_singleton_pipeline_stages()
    {
        GlyphNodeDefinition attempted = new InteractionAttemptedStageExecutor().CreateDefinition();
        attempted.TypeId.Should().Be("stage.interaction_attempted");
        attempted.Archetype.Should().Be(GlyphNodeArchetype.PipelineStage);
        attempted.IsSingleton.Should().BeTrue();
        attempted.RestrictToEventType.Should().Be(GlyphEventType.InteractionPipeline);
        attempted.InputPins.Should().HaveCount(4);
        attempted.InputPins.Should().NotContain(p => p.Id == "exec_in", "stage nodes are entry points with no exec_in");
        attempted.InputPins.Should().Contain(p => p.Id == "character_id");
        attempted.InputPins.Should().Contain(p => p.Id == "creature");
        attempted.InputPins.Should().Contain(p => p.Id == "interaction_tag");
        attempted.InputPins.Should().Contain(p => p.Id == "target_id");
        attempted.OutputPins.Should().Contain(p => p.Id == "exec_out" && p.Name == "Then");

        GlyphNodeDefinition started = new InteractionStartedStageExecutor().CreateDefinition();
        started.TypeId.Should().Be("stage.interaction_started");
        started.Archetype.Should().Be(GlyphNodeArchetype.PipelineStage);
        started.IsSingleton.Should().BeTrue();
        started.InputPins.Should().HaveCount(4);
        started.OutputPins.Should().Contain(p => p.Id == "exec_out" && p.Name == "Then");

        GlyphNodeDefinition tick = new InteractionTickStageExecutor().CreateDefinition();
        tick.TypeId.Should().Be("stage.interaction_tick");
        tick.InputPins.Should().HaveCount(4);
        tick.OutputPins.Should().Contain(p => p.Id == "exec_out" && p.Name == "Then");

        GlyphNodeDefinition completed = new InteractionCompletedStageExecutor().CreateDefinition();
        completed.TypeId.Should().Be("stage.interaction_completed");
        completed.InputPins.Should().HaveCount(4);
        completed.OutputPins.Should().Contain(p => p.Id == "exec_out" && p.Name == "Then");
    }

    [Test]
    public void GetInfo_executor_definition_has_correct_output_pins()
    {
        GlyphNodeDefinition def = new GetInteractionInfoExecutor().CreateDefinition();
        def.TypeId.Should().Be("interaction.get_info");
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);
        def.OutputPins.Should().Contain(p => p.Id == "tag");
        def.OutputPins.Should().Contain(p => p.Id == "target_id");
        def.OutputPins.Should().Contain(p => p.Id == "progress");
    }

    // ==================== Helpers ====================

    private static GlyphExecutionContext CreatePipelineContext() => new()
    {
        Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
        MaxExecutionSteps = 1000,
        EnableTracing = false
    };
}
