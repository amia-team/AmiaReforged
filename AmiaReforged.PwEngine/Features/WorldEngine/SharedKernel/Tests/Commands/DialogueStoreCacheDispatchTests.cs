using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Commands;

/// <summary>
/// Dispatcher-contract tests for the dialogue store-cache event flow.
///
/// These exercise the real <see cref="CommandDispatcher"/> and the synchronous
/// <see cref="InMemoryEventBus"/> with small local stub handlers for the real
/// Update/Delete/Create dialogue command types. They isolate dispatcher publication and
/// routing: a successful command publishes exactly one corresponding
/// <see cref="CommandExecutedEvent{TCommand}"/> and a rejected or throwing command publishes
/// none. They do not establish EF persistence or actual cache clearing — packet 04 owns the
/// real cache, and the CRUD handlers' SaveChangesAsync-before-return ordering is a separate
/// source-inspection fact, not something asserted here.
/// </summary>
[TestFixture]
public class DialogueStoreCacheDispatchTests
{
    private static PersistedDialogueTree SampleTree(string id) =>
        new() { DialogueTreeId = id, Title = id };

    private static (CommandDispatcher Dispatcher, InMemoryEventBus Bus, EventRecorder Recorder)
        WireUp(params ICommandHandlerMarker[] commandHandlers)
    {
        var bus = new InMemoryEventBus();
        var recorder = new EventRecorder();
        bus.Subscribe<CommandExecutedEvent<UpdateDialogueTreeCommand>>(
            (@event, _) => recorder.HandleAsync(@event, _));
        bus.Subscribe<CommandExecutedEvent<DeleteDialogueTreeCommand>>(
            (@event, _) => recorder.HandleAsync(@event, _));

        var dispatcher = new CommandDispatcher(commandHandlers.ToList(), bus);
        return (dispatcher, bus, recorder);
    }

    // === Successful update / delete: exactly one corresponding event, observed by the subscriber ===

    [Test]
    public async Task SuccessfulUpdate_ThroughDispatcher_PublishesOneUpdateEvent_ObservedBySubscriber()
    {
        var updateStub = new FlaggingCommandHandler();
        var (dispatcher, bus, recorder) = WireUp(updateStub);

        UpdateDialogueTreeCommand command = new()
        {
            DialogueTreeId = "dt_update",
            Tree = SampleTree("dt_update")
        };
        CommandResult result = await dispatcher.DispatchAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(1),
            "A successful update must publish exactly one CommandExecutedEvent");
        Assert.That(bus.PublishedEvents[0], Is.InstanceOf<CommandExecutedEvent<UpdateDialogueTreeCommand>>());

        Assert.That(recorder.Update, Is.Not.Null, "The subscriber must observe the update event");
        Assert.That(recorder.Update!.Command, Is.EqualTo(command), "Subscriber sees the original command");
        Assert.That(recorder.Update!.Result.Success, Is.True, "Subscriber sees the successful result");
        Assert.That(updateStub.Mutated, Is.True,
            "Stub flag is ordering evidence that the handler ran before the event was published");
        Assert.That(recorder.Delete, Is.Null, "The update must not route to the delete subscriber");
    }

    [Test]
    public async Task SuccessfulDelete_ThroughDispatcher_PublishesOneDeleteEvent_ObservedBySubscriber()
    {
        var deleteStub = new FlaggingCommandHandler();
        var (dispatcher, bus, recorder) = WireUp(deleteStub);

        DeleteDialogueTreeCommand command = new() { DialogueTreeId = "dt_delete" };
        CommandResult result = await dispatcher.DispatchAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(1),
            "A successful delete must publish exactly one CommandExecutedEvent");
        Assert.That(bus.PublishedEvents[0], Is.InstanceOf<CommandExecutedEvent<DeleteDialogueTreeCommand>>());

        Assert.That(recorder.Delete, Is.Not.Null, "The subscriber must observe the delete event");
        Assert.That(recorder.Delete!.Command, Is.EqualTo(command), "Subscriber sees the original command");
        Assert.That(recorder.Delete!.Result.Success, Is.True, "Subscriber sees the successful result");
        Assert.That(deleteStub.Mutated, Is.True,
            "Stub flag is ordering evidence that the handler ran before the event was published");
        Assert.That(recorder.Update, Is.Null, "The delete must not route to the update subscriber");
    }

    // === Rejected command: no event, subscriber never runs (explicit failure from the stub) ===

    [Test]
    public async Task RejectedUpdate_ThroughDispatcher_PublishesNoEvent_AndSubscriberNeverRuns()
    {
        var updateStub = new FlaggingCommandHandler { Fail = true };
        var (dispatcher, bus, recorder) = WireUp(updateStub);

        CommandResult result = await dispatcher.DispatchAsync(new UpdateDialogueTreeCommand
        {
            DialogueTreeId = "dt_reject",
            Tree = SampleTree("dt_reject")
        });

        Assert.That(result.Success, Is.False, "The stub returns an explicit failure result");
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(0),
            "A rejected command must not publish CommandExecutedEvent, so the subscriber never runs");
        Assert.That(recorder.Update, Is.Null);
    }

    [Test]
    public async Task RejectedDelete_ThroughDispatcher_PublishesNoEvent_AndSubscriberNeverRuns()
    {
        var deleteStub = new FlaggingCommandHandler { Fail = true };
        var (dispatcher, bus, recorder) = WireUp(deleteStub);

        CommandResult result = await dispatcher.DispatchAsync(new DeleteDialogueTreeCommand { DialogueTreeId = "dt_reject" });

        Assert.That(result.Success, Is.False);
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(0),
            "A rejected command must not publish CommandExecutedEvent, so the subscriber never runs");
        Assert.That(recorder.Delete, Is.Null);
    }

    // === Throwing handler: failed dispatcher result, no success event ===

    [Test]
    public async Task ThrowingHandler_ThroughDispatcher_ReturnsFailure_AndPublishesNoSuccessEvent()
    {
        var (dispatcher, bus, recorder) = WireUp(new ThrowingCommandHandler());

        CommandResult result = await dispatcher.DispatchAsync(new UpdateDialogueTreeCommand
        {
            DialogueTreeId = "dt_throw",
            Tree = SampleTree("dt_throw")
        });

        Assert.That(result.Success, Is.False, "A throwing handler yields a failed dispatcher result");
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(0),
            "A failure must not publish a success CommandExecutedEvent");
        Assert.That(recorder.Update, Is.Null);
    }

    // === Create: publishes its own command event, never invokes update/delete subscribers ===

    [Test]
    public async Task SuccessfulCreate_PublishesItsOwnEvent_NeverInvokesUpdateDeleteSubscribers()
    {
        var createStub = new FlaggingCommandHandler();
        var (dispatcher, bus, recorder) = WireUp(createStub);

        CommandResult result = await dispatcher.DispatchAsync(new CreateDialogueTreeCommand
        {
            Tree = SampleTree("dt_create")
        });

        Assert.That(result.Success, Is.True);
        Assert.That(createStub.Mutated, Is.True);
        Assert.That(bus.PublishedEvents, Has.Count.EqualTo(1), "Create publishes its own command event");
        Assert.That(bus.PublishedEvents[0], Is.InstanceOf<CommandExecutedEvent<CreateDialogueTreeCommand>>());
        Assert.That(recorder.Update, Is.Null, "Create must not invoke the update subscriber");
        Assert.That(recorder.Delete, Is.Null, "Create must not invoke the delete subscriber");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Test doubles (fixture-local)
    // ═══════════════════════════════════════════════════════════════════

    private sealed class EventRecorder
        : IEventHandler<CommandExecutedEvent<UpdateDialogueTreeCommand>>,
          IEventHandler<CommandExecutedEvent<DeleteDialogueTreeCommand>>,
          IEventHandlerMarker
    {
        public CommandExecutedEvent<UpdateDialogueTreeCommand>? Update { get; private set; }
        public CommandExecutedEvent<DeleteDialogueTreeCommand>? Delete { get; private set; }

        public Task HandleAsync(CommandExecutedEvent<UpdateDialogueTreeCommand> @event, CancellationToken cancellationToken = default)
        {
            Update = @event;
            return Task.CompletedTask;
        }

        public Task HandleAsync(CommandExecutedEvent<DeleteDialogueTreeCommand> @event, CancellationToken cancellationToken = default)
        {
            Delete = @event;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A stub command handler for the real dialogue command types. It records that it ran via
    /// <see cref="Mutated"/> (ordering evidence only) and returns either success or an explicit
    /// failure result — the dispatcher is never mocked into behaving incorrectly.
    /// </summary>
    private sealed class FlaggingCommandHandler : ICommandHandler<UpdateDialogueTreeCommand>,
        ICommandHandler<DeleteDialogueTreeCommand>,
        ICommandHandler<CreateDialogueTreeCommand>
    {
        public bool Fail { get; init; }
        public bool Mutated { get; private set; }

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

        public Task<CommandResult> HandleAsync(CreateDialogueTreeCommand command, CancellationToken cancellationToken = default)
        {
            Mutated = true;
            return Task.FromResult(CommandResult.Ok());
        }
    }

    private sealed class ThrowingCommandHandler : ICommandHandler<UpdateDialogueTreeCommand>
    {
        public Task<CommandResult> HandleAsync(UpdateDialogueTreeCommand command, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
