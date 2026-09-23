using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

/// <summary>
/// Focused unit tests for <see cref="DialogueNpcSynchronizationHandler"/>.
///
/// The handler is exercised directly with a fake <see cref="IDialogueNpcSynchronizer"/> — no real
/// <c>NwCreature</c> objects are constructed and no NWN runtime is touched. The fake records the
/// exact create/update/delete calls so each mapping (tree ID, speaker tag, null-tag handling) is
/// asserted. A separate dispatcher-level test proves that a rejected command never publishes the
/// success event and therefore never reaches this subscriber.
/// </summary>
[TestFixture]
public class DialogueNpcSynchronizationHandlerTests
{
    private static PersistedDialogueTree Tree(string id, string? speakerTag = null) =>
        new() { DialogueTreeId = id, Title = id, SpeakerTag = speakerTag };

    private static CreateDialogueTreeCommand CreateCommand(string id, string? speakerTag = null) =>
        new() { Tree = Tree(id, speakerTag) };

    private static UpdateDialogueTreeCommand UpdateCommand(string id, string? speakerTag = null) =>
        new() { DialogueTreeId = id, Tree = Tree(id, speakerTag) };

    private static DeleteDialogueTreeCommand DeleteCommand(string id) =>
        new() { DialogueTreeId = id };

    // 1. Create with a speaker tag: register called exactly once with the correct tag and tree ID.

    [Test]
    public async Task SuccessfulCreate_WithSpeakerTag_RegistersExactlyOnce_ForwardsTagAndTreeId()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };

        await handler.HandleAsync(
            new CommandExecutedEvent<CreateDialogueTreeCommand>(CreateCommand("dt_1", "npc_speaker"), CommandResult.Ok()));

        Assert.That(synchronizer.RegisterCalls, Has.Count.EqualTo(1), "Register is called exactly once");
        Assert.That(synchronizer.RegisterCalls[0].SpeakerTag, Is.EqualTo("npc_speaker"), "Correct speaker tag forwarded");
        Assert.That(synchronizer.RegisterCalls[0].DialogueTreeId, Is.EqualTo("dt_1"), "Correct tree ID forwarded");
    }

    // 2. Create without a speaker tag: register is not called.

    [Test]
    public async Task SuccessfulCreate_WithoutSpeakerTag_DoesNotRegister()
    {
        // null, empty, and whitespace tags must all be treated as "no NPC"
        foreach (string? tag in new[] { null, "", "   " })
        {
            var synchronizer = new RecordingSynchronizer();
            var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };
            await handler.HandleAsync(
                new CommandExecutedEvent<CreateDialogueTreeCommand>(CreateCommand("dt_no", tag), CommandResult.Ok()));
            Assert.That(synchronizer.RegisterCalls, Has.Count.EqualTo(0),
                $"Register must not be called when the speaker tag is '{tag ?? "null"}'");
        }
    }

    // 3. Update: update called exactly once with the immutable tree ID and the new tag, including null.

    [Test]
    public async Task SuccessfulUpdate_ForwardsTreeIdAndNewTag_IncludingNull()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };

        await handler.HandleAsync(
            new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_u", "npc_new"), CommandResult.Ok()));

        Assert.That(synchronizer.UpdateCalls, Has.Count.EqualTo(1), "Update is called exactly once");
        Assert.That(synchronizer.UpdateCalls[0].DialogueTreeId, Is.EqualTo("dt_u"), "Immutable route/tree ID forwarded");
        Assert.That(synchronizer.UpdateCalls[0].NewSpeakerTag, Is.EqualTo("npc_new"), "New speaker tag forwarded");
    }

    [Test]
    public async Task SuccessfulUpdate_WithNullNewTag_ForwardsNull_InsteadOfDiscarding()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };

        await handler.HandleAsync(
            new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_u", null), CommandResult.Ok()));

        Assert.That(synchronizer.UpdateCalls, Has.Count.EqualTo(1), "Update is still called with a null tag");
        Assert.That(synchronizer.UpdateCalls[0].NewSpeakerTag, Is.Null, "Null new tag is forwarded, not discarded");
    }

    // 4. Delete: unregister called exactly once with the deleted tree ID.

    [Test]
    public async Task SuccessfulDelete_UnregistersExactlyOnce_WithDeletedTreeId()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };

        await handler.HandleAsync(
            new CommandExecutedEvent<DeleteDialogueTreeCommand>(DeleteCommand("dt_d"), CommandResult.Ok()));

        Assert.That(synchronizer.UnregisterCalls, Has.Count.EqualTo(1), "Unregister is called exactly once");
        Assert.That(synchronizer.UnregisterCalls[0], Is.EqualTo("dt_d"), "Deleted tree ID forwarded");
    }

    // Rejection: a failed-result event is ignored (defensive), so a non-success event never triggers sync.

    [Test]
    public async Task FailedResultEvent_IsDefensivelyIgnored_ForAllEventTypes()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };

        await handler.HandleAsync(
            new CommandExecutedEvent<CreateDialogueTreeCommand>(CreateCommand("dt_f", "npc_x"), CommandResult.Fail("nope")));
        await handler.HandleAsync(
            new CommandExecutedEvent<UpdateDialogueTreeCommand>(UpdateCommand("dt_f", "npc_x"), CommandResult.Fail("nope")));
        await handler.HandleAsync(
            new CommandExecutedEvent<DeleteDialogueTreeCommand>(DeleteCommand("dt_f"), CommandResult.Fail("nope")));

        Assert.That(synchronizer.RegisterCalls, Has.Count.EqualTo(0), "A failed create must not register");
        Assert.That(synchronizer.UpdateCalls, Has.Count.EqualTo(0), "A failed update must not sync");
        Assert.That(synchronizer.UnregisterCalls, Has.Count.EqualTo(0), "A failed delete must not unregister");
    }

    // Dispatcher-level proof that a rejected command never publishes the success event, so the
    // subscriber cannot be invoked. The real CommandDispatcher + InMemoryEventBus are used; no
    // failed-event path is faked.

    [Test]
    public async Task RejectedCommand_ThroughDispatcher_PublishesNoEvent_AndSubscriberNeverInvoked()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new DialogueNpcSynchronizationHandler { Synchronizer = synchronizer };

        var bus = new InMemoryEventBus();
        bus.Subscribe<CommandExecutedEvent<CreateDialogueTreeCommand>>(
            (@event, _) => handler.HandleAsync(@event, CancellationToken.None));

        var stub = new FlaggingCommandHandler { Fail = true };
        var dispatcher = new CommandDispatcher(
            new ICommandHandlerMarker[] { stub },
            bus);

        CommandResult result = await dispatcher.DispatchAsync(new CreateDialogueTreeCommand
        {
            Tree = Tree("dt_reject", "npc_x")
        });

        Assert.That(result.Success, Is.False, "The stub returns an explicit failure result");
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(0),
            "A rejected command must not publish CommandExecutedEvent, so the subscriber never runs");
        Assert.That(synchronizer.RegisterCalls, Has.Count.EqualTo(0), "The subscriber must not be invoked");
    }

    // The owner exposes the marker binding and all three typed event interfaces.

    [Test]
    public void Owner_ExposesMarkerAndThreeTypedEventInterfaces()
    {
        var type = typeof(DialogueNpcSynchronizationHandler);

        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandlerMarker)));
        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandler<CommandExecutedEvent<CreateDialogueTreeCommand>>)));
        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandler<CommandExecutedEvent<UpdateDialogueTreeCommand>>)));
        Assert.That(type.GetInterfaces(), Contains.Item(typeof(IEventHandler<CommandExecutedEvent<DeleteDialogueTreeCommand>>)));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Test doubles (fixture-local)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// A fake <see cref="IDialogueNpcSynchronizer"/> that records the exact create/update/delete
    /// calls. It never touches NWN APIs, so the handler's orchestration is the only thing under test.
    /// </summary>
    private sealed class RecordingSynchronizer : IDialogueNpcSynchronizer
    {
        public List<(string SpeakerTag, string DialogueTreeId)> RegisterCalls { get; } = new();
        public List<(string DialogueTreeId, string? NewSpeakerTag)> UpdateCalls { get; } = new();
        public List<string> UnregisterCalls { get; } = new();

        public Task<int> RegisterAsync(string speakerTag, string dialogueTreeId, CancellationToken ct = default)
        {
            RegisterCalls.Add((speakerTag, dialogueTreeId));
            return Task.FromResult(0);
        }

        public Task<(int unregistered, int registered)> UpdateAsync(string dialogueTreeId, string? newSpeakerTag, CancellationToken ct = default)
        {
            UpdateCalls.Add((dialogueTreeId, newSpeakerTag));
            return Task.FromResult((0, 0));
        }

        public Task<int> UnregisterAsync(string dialogueTreeId, CancellationToken ct = default)
        {
            UnregisterCalls.Add(dialogueTreeId);
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// A stub command handler for the real dialogue command types. Records that it ran via
    /// <see cref="Mutated"/> and returns either success or an explicit failure result.
    /// </summary>
    private sealed class FlaggingCommandHandler : ICommandHandler<CreateDialogueTreeCommand>,
        ICommandHandler<UpdateDialogueTreeCommand>,
        ICommandHandler<DeleteDialogueTreeCommand>
    {
        public bool Fail { get; init; }
        public bool Mutated { get; private set; }

        public Task<CommandResult> HandleAsync(CreateDialogueTreeCommand command, CancellationToken cancellationToken = default)
        {
            Mutated = true;
            return Task.FromResult(Fail ? CommandResult.Fail("rejected create") : CommandResult.Ok());
        }

        public Task<CommandResult> HandleAsync(UpdateDialogueTreeCommand command, CancellationToken cancellationToken = default)
        {
            Mutated = true;
            return Task.FromResult(Fail ? CommandResult.Fail("rejected update") : CommandResult.Ok());
        }

        public Task<CommandResult> HandleAsync(DeleteDialogueTreeCommand command, CancellationToken cancellationToken = default)
        {
            Mutated = true;
            return Task.FromResult(Fail ? CommandResult.Fail("rejected delete") : CommandResult.Ok());
        }
    }
}
