using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration.Tests;

[TestFixture]
public class SessionObjectExecutorTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint TestObjectId = 0x00001234;
    private const uint AnotherObjectId = 0x0000ABCD;

    // ==================== StoreSessionObjectExecutor ====================

    [Test]
    public async Task Store_writes_object_to_session_metadata()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "my_target",
                "object" => TestObjectId,
                _ => null
            }));

        result.NextExecPinId.Should().Be("exec_out");
        context.Session!.Metadata.Should().ContainKey("my_target");
        context.Session.Metadata["my_target"].Should().Be(TestObjectId);
    }

    [Test]
    public async Task Store_falls_back_to_context_metadata_when_no_session()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithoutSession();

        await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "stashed_npc",
                "object" => TestObjectId,
                _ => null
            }));

        context.Session.Should().BeNull();
        context.InteractionMetadata.Should().ContainKey("stashed_npc");
        context.InteractionMetadata!["stashed_npc"].Should().Be(TestObjectId);
    }

    [Test]
    public async Task Store_skips_write_when_key_is_empty()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "",
                "object" => TestObjectId,
                _ => null
            }));

        result.NextExecPinId.Should().Be("exec_out");
        context.Session!.Metadata.Should().BeNull();
    }

    [Test]
    public async Task Store_overwrites_existing_key()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        // First store
        await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "target",
                "object" => TestObjectId,
                _ => null
            }));

        // Overwrite with a different object
        await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "target",
                "object" => AnotherObjectId,
                _ => null
            }));

        context.Session!.Metadata!["target"].Should().Be(AnotherObjectId);
    }

    [Test]
    public async Task Store_coerces_int_value_to_uint()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "from_int",
                "object" => (int)42,
                _ => null
            }));

        context.Session!.Metadata!["from_int"].Should().Be((uint)42);
    }

    [Test]
    public async Task Store_coerces_string_value_to_uint()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "from_string",
                "object" => "4660",
                _ => null
            }));

        context.Session!.Metadata!["from_string"].Should().Be((uint)4660);
    }

    [Test]
    public async Task Store_writes_object_invalid_for_null_value()
    {
        StoreSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "null_obj",
                "object" => null,
                _ => null
            }));

        context.Session!.Metadata!["null_obj"].Should().Be(ObjectInvalid);
    }

    // ==================== RetrieveSessionObjectExecutor ====================

    [Test]
    public async Task Retrieve_reads_object_from_session_metadata()
    {
        RetrieveSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();
        context.Session!.Metadata = new Dictionary<string, object> { ["saved_npc"] = TestObjectId };

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "key" ? "saved_npc" : null));

        result.OutputValues.Should().NotBeNull();
        result.OutputValues!["object"].Should().Be(TestObjectId);
        result.OutputValues["exists"].Should().Be(true);
    }

    [Test]
    public async Task Retrieve_returns_invalid_and_false_when_key_missing()
    {
        RetrieveSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();
        context.Session!.Metadata = new Dictionary<string, object>();

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "key" ? "nonexistent" : null));

        result.OutputValues!["object"].Should().Be(ObjectInvalid);
        result.OutputValues["exists"].Should().Be(false);
    }

    [Test]
    public async Task Retrieve_falls_back_to_context_metadata_when_no_session()
    {
        RetrieveSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithoutSession();
        context.InteractionMetadata = new Dictionary<string, object> { ["ctx_obj"] = AnotherObjectId };

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "key" ? "ctx_obj" : null));

        result.OutputValues!["object"].Should().Be(AnotherObjectId);
        result.OutputValues["exists"].Should().Be(true);
    }

    [Test]
    public async Task Retrieve_returns_invalid_when_key_is_empty()
    {
        RetrieveSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();
        context.Session!.Metadata = new Dictionary<string, object> { ["x"] = TestObjectId };

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "key" ? "" : null));

        result.OutputValues!["object"].Should().Be(ObjectInvalid);
        result.OutputValues["exists"].Should().Be(false);
    }

    [Test]
    public async Task Retrieve_handles_stored_int_value()
    {
        RetrieveSessionObjectExecutor executor = new();
        GlyphNodeInstance node = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();
        // Simulate someone storing an int directly in metadata
        context.Session!.Metadata = new Dictionary<string, object> { ["int_val"] = (int)99 };

        GlyphNodeResult result = await executor.ExecuteAsync(node, context,
            pin => Task.FromResult<object?>(pin == "key" ? "int_val" : null));

        result.OutputValues!["object"].Should().Be((uint)99);
        result.OutputValues["exists"].Should().Be(true);
    }

    // ==================== Round-trip (Store then Retrieve) ====================

    [Test]
    public async Task Store_then_retrieve_round_trips_object_id()
    {
        StoreSessionObjectExecutor storeExec = new();
        RetrieveSessionObjectExecutor retrieveExec = new();
        GlyphNodeInstance storeNode = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };
        GlyphNodeInstance retrieveNode = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };
        GlyphExecutionContext context = CreateContextWithSession();

        // Store an object
        await storeExec.ExecuteAsync(storeNode, context,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "round_trip_obj",
                "object" => TestObjectId,
                _ => null
            }));

        // Retrieve it
        GlyphNodeResult result = await retrieveExec.ExecuteAsync(retrieveNode, context,
            pin => Task.FromResult<object?>(pin == "key" ? "round_trip_obj" : null));

        result.OutputValues!["object"].Should().Be(TestObjectId);
        result.OutputValues["exists"].Should().Be(true);
    }

    [Test]
    public async Task Store_then_retrieve_works_across_separate_contexts_with_shared_session()
    {
        // Simulates cross-stage persistence: two separate contexts sharing the same session
        InteractionSession session = CreateSession();
        StoreSessionObjectExecutor storeExec = new();
        RetrieveSessionObjectExecutor retrieveExec = new();

        // Stage 1: Started — store an object
        GlyphExecutionContext ctx1 = CreateContextForSession(session);
        GlyphNodeInstance storeNode = new() { TypeId = StoreSessionObjectExecutor.NodeTypeId };

        await storeExec.ExecuteAsync(storeNode, ctx1,
            pin => Task.FromResult<object?>(pin switch
            {
                "key" => "tool_object",
                "object" => TestObjectId,
                _ => null
            }));

        // Stage 2: Tick — new context, same session, retrieve the object
        GlyphExecutionContext ctx2 = CreateContextForSession(session);
        GlyphNodeInstance retrieveNode = new() { TypeId = RetrieveSessionObjectExecutor.NodeTypeId };

        GlyphNodeResult result = await retrieveExec.ExecuteAsync(retrieveNode, ctx2,
            pin => Task.FromResult<object?>(pin == "key" ? "tool_object" : null));

        result.OutputValues!["object"].Should().Be(TestObjectId);
        result.OutputValues["exists"].Should().Be(true);
    }

    // ==================== CoerceToUint ====================

    [TestCase((uint)42, (uint)42)]
    [TestCase((int)42, (uint)42)]
    [TestCase((long)42, (uint)42)]
    public void CoerceToUint_handles_numeric_types(object input, uint expected)
    {
        StoreSessionObjectExecutor.CoerceToUint(input).Should().Be(expected);
    }

    [Test]
    public void CoerceToUint_parses_string()
    {
        StoreSessionObjectExecutor.CoerceToUint("12345").Should().Be((uint)12345);
    }

    [Test]
    public void CoerceToUint_returns_invalid_for_null()
    {
        StoreSessionObjectExecutor.CoerceToUint(null).Should().Be(ObjectInvalid);
    }

    [Test]
    public void CoerceToUint_returns_invalid_for_unparseable_string()
    {
        StoreSessionObjectExecutor.CoerceToUint("not_a_number").Should().Be(ObjectInvalid);
    }

    // ==================== Definition Validation ====================

    [Test]
    public void Store_definition_has_correct_pins_and_metadata()
    {
        GlyphNodeDefinition def = new StoreSessionObjectExecutor().CreateDefinition();

        def.TypeId.Should().Be("interaction.store_session_object");
        def.DisplayName.Should().Be("Store Session Object");
        def.Archetype.Should().Be(GlyphNodeArchetype.Action);
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);

        def.InputPins.Should().HaveCount(3);
        def.InputPins.Should().Contain(p => p.Id == "exec_in" && p.DataType == GlyphDataType.Exec);
        def.InputPins.Should().Contain(p => p.Id == "key" && p.DataType == GlyphDataType.String);
        def.InputPins.Should().Contain(p => p.Id == "object" && p.DataType == GlyphDataType.NwObject);

        def.OutputPins.Should().HaveCount(1);
        def.OutputPins.Should().Contain(p => p.Id == "exec_out" && p.DataType == GlyphDataType.Exec);
    }

    [Test]
    public void Retrieve_definition_has_correct_pins_and_metadata()
    {
        GlyphNodeDefinition def = new RetrieveSessionObjectExecutor().CreateDefinition();

        def.TypeId.Should().Be("interaction.retrieve_session_object");
        def.DisplayName.Should().Be("Retrieve Session Object");
        def.Archetype.Should().Be(GlyphNodeArchetype.PureFunction);
        def.ScriptCategory.Should().Be(GlyphScriptCategory.Interaction);

        def.InputPins.Should().HaveCount(1);
        def.InputPins.Should().Contain(p => p.Id == "key" && p.DataType == GlyphDataType.String);

        def.OutputPins.Should().HaveCount(2);
        def.OutputPins.Should().Contain(p => p.Id == "object" && p.DataType == GlyphDataType.NwObject);
        def.OutputPins.Should().Contain(p => p.Id == "exists" && p.DataType == GlyphDataType.Bool);
    }

    // ==================== Helpers ====================

    private static InteractionSession CreateSession() => new()
    {
        CharacterId = new CharacterId(Guid.NewGuid()),
        InteractionTag = "test_interaction",
        TargetId = Guid.NewGuid(),
        TargetMode = InteractionTargetMode.Node,
        RequiredRounds = 3
    };

    private static GlyphExecutionContext CreateContextWithSession()
    {
        return new GlyphExecutionContext
        {
            Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
            MaxExecutionSteps = 1000,
            EnableTracing = false,
            Session = CreateSession()
        };
    }

    private static GlyphExecutionContext CreateContextWithoutSession()
    {
        return new GlyphExecutionContext
        {
            Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
            MaxExecutionSteps = 1000,
            EnableTracing = false,
            Session = null
        };
    }

    private static GlyphExecutionContext CreateContextForSession(InteractionSession session)
    {
        return new GlyphExecutionContext
        {
            Graph = new GlyphGraph { EventType = GlyphEventType.InteractionPipeline, Name = "Test" },
            MaxExecutionSteps = 1000,
            EnableTracing = false,
            Session = session
        };
    }
}
