using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Commands;

/// <summary>
/// Behavioral specifications for the CommandDispatcher.
/// Tests follow BDD-style naming: Context_Behavior_Outcome
/// </summary>
[TestFixture]
public class CommandDispatcherBehavior
{
    // Test doubles
    private Mock<IEventBus> _eventBusMock = null!;
    private TestCommandHandler _testHandler = null!;
    private CommandDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        _eventBusMock = new Mock<IEventBus>();
        _testHandler = new TestCommandHandler();

        // Create dispatcher with test handler
        List<ICommandHandlerMarker> handlers = new List<ICommandHandlerMarker> { _testHandler };
        _dispatcher = new CommandDispatcher(handlers, _eventBusMock.Object);
    }

    // === Successful Command Execution ===

    [Test]
    public async Task GivenValidCommand_WhenDispatched_ThenHandlerIsInvoked()
    {
        // Given: A valid test command
        TestCommand command = new TestCommand { Value = "test-data" };

        // When: The command is dispatched
        CommandResult result = await _dispatcher.DispatchAsync(command);

        // Then: The handler was invoked successfully
        Assert.That(result.Success, Is.True);
        Assert.That(_testHandler.LastHandledCommand, Is.EqualTo(command));
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenSuccessfulCommand_WhenDispatched_ThenDomainEventIsPublished()
    {
        // Given: A command that will succeed
        TestCommand command = new TestCommand { Value = "success" };

        // When: The command is dispatched
        await _dispatcher.DispatchAsync(command);

        // Then: A CommandExecutedEvent was published
        _eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.Is<CommandExecutedEvent<TestCommand>>(evt => evt.Command == command),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GivenMultipleCommands_WhenDispatchedSequentially_ThenEachHandlerIsInvokedOnce()
    {
        // Given: Three separate commands
        TestCommand command1 = new TestCommand { Value = "first" };
        TestCommand command2 = new TestCommand { Value = "second" };
        TestCommand command3 = new TestCommand { Value = "third" };

        // When: Each command is dispatched
        await _dispatcher.DispatchAsync(command1);
        await _dispatcher.DispatchAsync(command2);
        await _dispatcher.DispatchAsync(command3);

        // Then: Handler was invoked three times
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(3));
    }

    // === Failed Command Execution ===

    [Test]
    public async Task GivenFailingCommand_WhenDispatched_ThenFailureResultIsReturned()
    {
        // Given: A command that will fail
        TestCommand command = new TestCommand { Value = "fail", ShouldFail = true };

        // When: The command is dispatched
        CommandResult result = await _dispatcher.DispatchAsync(command);

        // Then: Result indicates failure with error message
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Test failure"));
    }

    [Test]
    public async Task GivenFailingCommand_WhenDispatched_ThenNoDomainEventIsPublished()
    {
        // Given: A command that will fail
        TestCommand command = new TestCommand { Value = "fail", ShouldFail = true };

        // When: The command is dispatched
        await _dispatcher.DispatchAsync(command);

        // Then: No CommandExecutedEvent was published
        _eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.IsAny<CommandExecutedEvent<TestCommand>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // === Missing Handler ===

    [Test]
    public async Task GivenUnregisteredCommand_WhenDispatched_ThenFailureResultIndicatesMissingHandler()
    {
        // Given: A command with no registered handler
        UnhandledCommand command = new UnhandledCommand { Data = "orphaned" };
        List<ICommandHandlerMarker> emptyHandlers = new List<ICommandHandlerMarker>();
        CommandDispatcher emptyDispatcher = new CommandDispatcher(emptyHandlers, _eventBusMock.Object);

        // When: The command is dispatched
        CommandResult result = await emptyDispatcher.DispatchAsync(command);

        // Then: Result indicates no handler was found
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("No handler registered"));
        Assert.That(result.ErrorMessage, Does.Contain(nameof(UnhandledCommand)));
    }

    // === Batch Execution - Default Behavior ===

    [Test]
    public async Task GivenMultipleCommands_WhenDispatchedAsBatch_ThenAllAreExecutedInOrder()
    {
        // Given: A batch of commands
        TestCommand[] commands = new[]
        {
            new TestCommand { Value = "first" },
            new TestCommand { Value = "second" },
            new TestCommand { Value = "third" }
        };

        // When: The batch is dispatched with default options
        BatchCommandResult batchResult = await _dispatcher.DispatchBatchAsync(commands);

        // Then: All commands were executed
        Assert.That(batchResult.TotalCount, Is.EqualTo(3));
        Assert.That(batchResult.SuccessCount, Is.EqualTo(3));
        Assert.That(batchResult.FailedCount, Is.EqualTo(0));
        Assert.That(batchResult.AllSucceeded, Is.True);
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(3));
    }

    [Test]
    public async Task GivenBatchWithFailure_WhenDispatchedWithStopOnFirstFailure_ThenExecutionStops()
    {
        // Given: A batch where the second command will fail
        TestCommand[] commands = new[]
        {
            new TestCommand { Value = "first" },
            new TestCommand { Value = "fail", ShouldFail = true },
            new TestCommand { Value = "third" }
        };

        // When: The batch is dispatched with StopOnFirstFailure (default)
        BatchCommandResult batchResult = await _dispatcher.DispatchBatchAsync(commands);

        // Then: Only first two commands were executed (stopped after failure)
        Assert.That(batchResult.TotalCount, Is.EqualTo(2)); // Only 2 executed before stopping
        Assert.That(batchResult.SuccessCount, Is.EqualTo(1));
        Assert.That(batchResult.FailedCount, Is.EqualTo(1));
        Assert.That(batchResult.AllSucceeded, Is.False);
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(2)); // Only first two were attempted
    }

    [Test]
    public async Task GivenBatchWithFailure_WhenDispatchedWithContinueOnFailure_ThenAllAreExecuted()
    {
        // Given: A batch where the middle command will fail
        TestCommand[] commands = new[]
        {
            new TestCommand { Value = "first" },
            new TestCommand { Value = "fail", ShouldFail = true },
            new TestCommand { Value = "third" }
        };

        // When: The batch is dispatched with ContinueOnFailure option
        BatchExecutionOptions options = BatchExecutionOptions.ContinueOnFailure();
        BatchCommandResult batchResult = await _dispatcher.DispatchBatchAsync(commands, options);

        // Then: All three commands were executed
        Assert.That(batchResult.TotalCount, Is.EqualTo(3));
        Assert.That(batchResult.SuccessCount, Is.EqualTo(2));
        Assert.That(batchResult.FailedCount, Is.EqualTo(1));
        Assert.That(batchResult.AnyFailed, Is.True);
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(3)); // All three were attempted
    }

    // === Cancellation ===

    [Test]
    public async Task GivenCancelledToken_WhenBatchIsDispatched_ThenExecutionStopsEarly()
    {
        // Given: A batch of commands and a cancelled token
        TestCommand[] commands = new[]
        {
            new TestCommand { Value = "first" },
            new TestCommand { Value = "second" },
            new TestCommand { Value = "third" }
        };
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        // When: The batch is dispatched with cancelled token
        BatchCommandResult batchResult = await _dispatcher.DispatchBatchAsync(commands, cancellationToken: cts.Token);

        // Then: No commands were executed and result indicates cancellation
        Assert.That(batchResult.Cancelled, Is.True);
        Assert.That(batchResult.Results, Is.Empty);
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(0));
    }

    // === Value Object Behavior ===

    [Test]
    public void GivenBatchResult_WhenAllSucceeded_ThenSuccessRateIs100Percent()
    {
        // Given: A batch result where all commands succeeded
        List<CommandResult> results = new List<CommandResult>
        {
            CommandResult.Ok(),
            CommandResult.Ok(),
            CommandResult.Ok()
        };

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Success rate is 100%
        Assert.That(batchResult.SuccessRate, Is.EqualTo(100.0));
    }

    [Test]
    public void GivenBatchResult_WhenHalfFailed_ThenSuccessRateIs50Percent()
    {
        // Given: A batch result with 50% failure rate
        List<CommandResult> results = new List<CommandResult>
        {
            CommandResult.Ok(),
            CommandResult.Fail("error"),
            CommandResult.Ok(),
            CommandResult.Fail("error")
        };

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Success rate is 50%
        Assert.That(batchResult.SuccessRate, Is.EqualTo(50.0));
    }

    // === Test Fixtures ===

    private sealed class TestCommand : ICommand
    {
        public string Value { get; set; } = string.Empty;
        public bool ShouldFail { get; set; }
    }

    private sealed class UnhandledCommand : ICommand
    {
        public string Data { get; set; } = string.Empty;
    }

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public TestCommand? LastHandledCommand { get; private set; }
        public int TimesHandled { get; private set; }

        public Task<CommandResult> HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            LastHandledCommand = command;
            TimesHandled++;

            return command.ShouldFail
                ? Task.FromResult(CommandResult.Fail("Test failure"))
                : Task.FromResult(CommandResult.Ok());
        }
    }
}
