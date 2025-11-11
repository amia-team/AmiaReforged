using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Integration;

/// <summary>
/// Integration tests demonstrating the complete command dispatch flow.
/// These tests verify that all components work together correctly.
/// </summary>
[TestFixture]
public class CommandDispatchIntegrationBehavior
{
    // === End-to-End Command Execution ===

    [Test]
    public async Task GivenCompleteSystem_WhenCommandIsExecuted_ThenFlowCompletesSuccessfully()
    {
        // Given: A complete system with all components
        Mock<IEventBus> eventBusMock = new Mock<IEventBus>();
        DepositGoldCommandHandler depositHandler = new DepositGoldCommandHandler();
        List<ICommandHandlerMarker> handlers = new List<ICommandHandlerMarker> { depositHandler };
        CommandDispatcher dispatcher = new CommandDispatcher(handlers, eventBusMock.Object);

        DepositGoldCommand command = new DepositGoldCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 1000,
            Reason = "test deposit"
        };

        // When: Command is dispatched through the system
        CommandResult result = await dispatcher.DispatchAsync(command);

        // Then: The complete flow executes successfully
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Contains.Key("NewBalance"));
        Assert.That((int)result.Data["NewBalance"], Is.EqualTo(1000));

        // And: Domain event was published
        eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.Is<CommandExecutedEvent<DepositGoldCommand>>(evt =>
                    evt.Command == command &&
                    evt.Result.Success),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GivenCompleteSystem_WhenMultipleCommandsExecuted_ThenEachHandledIndependently()
    {
        // Given: A system with multiple command types
        Mock<IEventBus> eventBusMock = new Mock<IEventBus>();
        DepositGoldCommandHandler depositHandler = new DepositGoldCommandHandler();
        WithdrawGoldCommandHandler withdrawHandler = new WithdrawGoldCommandHandler();
        List<ICommandHandlerMarker> handlers = new List<ICommandHandlerMarker> { depositHandler, withdrawHandler };
        CommandDispatcher dispatcher = new CommandDispatcher(handlers, eventBusMock.Object);

        DepositGoldCommand depositCommand = new DepositGoldCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 1000,
            Reason = "deposit"
        };

        WithdrawGoldCommand withdrawCommand = new WithdrawGoldCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = 500,
            Reason = "withdrawal"
        };

        // When: Different commands are dispatched
        CommandResult depositResult = await dispatcher.DispatchAsync(depositCommand);
        CommandResult withdrawResult = await dispatcher.DispatchAsync(withdrawCommand);

        // Then: Each command is handled correctly
        Assert.That(depositResult.Success, Is.True);
        Assert.That(withdrawResult.Success, Is.True);
        Assert.That((int)depositResult.Data["NewBalance"], Is.EqualTo(1000));
        Assert.That((int)withdrawResult.Data["NewBalance"], Is.EqualTo(500));

        // And: Events were published for both
        eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.IsAny<CommandExecutedEvent<DepositGoldCommand>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.IsAny<CommandExecutedEvent<WithdrawGoldCommand>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // === End-to-End Query Execution ===

    [Test]
    public async Task GivenCompleteSystem_WhenQueryIsExecuted_ThenResultIsReturned()
    {
        // Given: A complete query system
        GetBalanceQueryHandler balanceHandler = new GetBalanceQueryHandler();
        List<IQueryHandlerMarker> handlers = new List<IQueryHandlerMarker> { balanceHandler };
        QueryDispatcher dispatcher = new QueryDispatcher(handlers);

        GetBalanceQuery query = new GetBalanceQuery { AccountId = Guid.NewGuid() };

        // When: Query is dispatched
        BalanceResult result = await dispatcher.DispatchAsync<GetBalanceQuery, BalanceResult>(query);

        // Then: Result is returned with expected data
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Balance, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.Currency, Is.EqualTo("gp"));
    }

    // === Batch Operations ===

    [Test]
    public async Task GivenBatchOfCommands_WhenExecuted_ThenResultsAreAggregated()
    {
        // Given: A system and multiple commands
        Mock<IEventBus> eventBusMock = new Mock<IEventBus>();
        DepositGoldCommandHandler handler = new DepositGoldCommandHandler();
        List<ICommandHandlerMarker> handlers = new List<ICommandHandlerMarker> { handler };
        CommandDispatcher dispatcher = new CommandDispatcher(handlers, eventBusMock.Object);

        DepositGoldCommand[] commands = new[]
        {
            new DepositGoldCommand { AccountId = Guid.NewGuid(), Amount = 100, Reason = "batch 1" },
            new DepositGoldCommand { AccountId = Guid.NewGuid(), Amount = 200, Reason = "batch 2" },
            new DepositGoldCommand { AccountId = Guid.NewGuid(), Amount = 300, Reason = "batch 3" }
        };

        // When: Batch is executed
        BatchCommandResult batchResult = await dispatcher.DispatchBatchAsync(commands);

        // Then: All commands succeeded and events were published
        Assert.That(batchResult.AllSucceeded, Is.True);
        Assert.That(batchResult.TotalCount, Is.EqualTo(3));
        Assert.That(batchResult.SuccessCount, Is.EqualTo(3));

        eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.IsAny<CommandExecutedEvent<DepositGoldCommand>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    // === Error Handling ===

    [Test]
    public async Task GivenInvalidCommand_WhenExecuted_ThenValidationFailureIsHandled()
    {
        // Given: A system with validation
        Mock<IEventBus> eventBusMock = new Mock<IEventBus>();
        WithdrawGoldCommandHandler handler = new WithdrawGoldCommandHandler();
        List<ICommandHandlerMarker> handlers = new List<ICommandHandlerMarker> { handler };
        CommandDispatcher dispatcher = new CommandDispatcher(handlers, eventBusMock.Object);

        WithdrawGoldCommand command = new WithdrawGoldCommand
        {
            AccountId = Guid.NewGuid(),
            Amount = -100, // Invalid: negative amount
            Reason = "invalid withdrawal"
        };

        // When: Invalid command is dispatched
        CommandResult result = await dispatcher.DispatchAsync(command);

        // Then: Validation failure is returned and no event is published
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("negative"));

        eventBusMock.Verify(
            bus => bus.PublishAsync(
                It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // === Test Fixtures - Realistic Domain Commands ===

    private sealed class DepositGoldCommand : ICommand
    {
        public Guid AccountId { get; set; }
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    private sealed class WithdrawGoldCommand : ICommand
    {
        public Guid AccountId { get; set; }
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    private sealed class GetBalanceQuery : IQuery<BalanceResult>
    {
        public Guid AccountId { get; set; }
    }

    private sealed class BalanceResult
    {
        public int Balance { get; set; }
        public string Currency { get; set; } = "gp";
    }

    // === Test Handlers - Simulating Real Behavior ===

    private sealed class DepositGoldCommandHandler : ICommandHandler<DepositGoldCommand>
    {
        private readonly Dictionary<Guid, int> _accounts = new();

        public Task<CommandResult> HandleAsync(DepositGoldCommand command, CancellationToken cancellationToken = default)
        {
            if (!_accounts.ContainsKey(command.AccountId))
            {
                _accounts[command.AccountId] = 0;
            }

            _accounts[command.AccountId] += command.Amount;

            return Task.FromResult(CommandResult.Ok(new Dictionary<string, object>
            {
                ["NewBalance"] = _accounts[command.AccountId],
                ["Deposited"] = command.Amount
            }));
        }
    }

    private sealed class WithdrawGoldCommandHandler : ICommandHandler<WithdrawGoldCommand>
    {
        private readonly Dictionary<Guid, int> _accounts = new();

        public Task<CommandResult> HandleAsync(WithdrawGoldCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Amount < 0)
            {
                return Task.FromResult(CommandResult.Fail("Amount cannot be negative"));
            }

            if (!_accounts.ContainsKey(command.AccountId))
            {
                _accounts[command.AccountId] = 1000; // Starting balance for test
            }

            if (_accounts[command.AccountId] < command.Amount)
            {
                return Task.FromResult(CommandResult.Fail("Insufficient funds"));
            }

            _accounts[command.AccountId] -= command.Amount;

            return Task.FromResult(CommandResult.Ok(new Dictionary<string, object>
            {
                ["NewBalance"] = _accounts[command.AccountId],
                ["Withdrawn"] = command.Amount
            }));
        }
    }

    private sealed class GetBalanceQueryHandler : IQueryHandler<GetBalanceQuery, BalanceResult>
    {
        public Task<BalanceResult> HandleAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
        {
            // Simulate database lookup
            BalanceResult result = new BalanceResult
            {
                Balance = Random.Shared.Next(0, 10000),
                Currency = "gp"
            };

            return Task.FromResult(result);
        }
    }
}

