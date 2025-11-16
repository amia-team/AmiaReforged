using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

public class FakeCommandDispatcher : ICommandDispatcher
{
    private readonly Dictionary<(Guid, string), int> _balances = new();

    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        if (command is DepositToVaultCommand deposit)
        {
            var key = (deposit.Owner.Value, deposit.AreaResRef);
            _balances[key] = _balances.GetValueOrDefault(key, 0) + deposit.Amount;
            return Task.FromResult(CommandResult.OkWith("newBalance", _balances[key]));
        }

        if (command is WithdrawFromVaultCommand withdraw)
        {
            var key = (withdraw.Owner.Value, withdraw.AreaResRef);
            int bal = _balances.GetValueOrDefault(key, 0);
            int take = Math.Min(bal, withdraw.RequestedAmount);
            _balances[key] = bal - take;
            return Task.FromResult(take > 0
                ? CommandResult.OkWith("withdrawnAmount", take)
                : CommandResult.Fail("No funds available"));
        }

        return Task.FromResult(CommandResult.Ok());
    }

    public Task<BatchCommandResult> DispatchBatchAsync<TCommand>(IEnumerable<TCommand> commands, BatchExecutionOptions? options = null, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        // Not used in these tests
        return Task.FromResult(new BatchCommandResult
        {
            Results = Array.Empty<CommandResult>(),
            TotalCount = 0,
            SuccessCount = 0,
            FailedCount = 0
        });
    }

    public Dictionary<(Guid, string), int> GetBalances() => _balances;
}

public class FakeQueryDispatcher : IQueryDispatcher
{
    private readonly FakeCommandDispatcher _commands;

    public FakeQueryDispatcher(FakeCommandDispatcher commands)
    {
        _commands = commands;
    }

    public Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default) where TQuery : IQuery<TResult>
    {
        if (query is GetVaultBalanceQuery balanceQuery)
        {
            int balance = _commands.GetBalances().GetValueOrDefault((balanceQuery.Owner.Value, balanceQuery.AreaResRef), 0);
            return Task.FromResult((TResult)(object)balance);
        }

        return Task.FromResult(default(TResult)!);
    }
}

[TestFixture]
public class ReeveFundsServiceTests
{
    [Test]
    public async Task GetHeldFundsAsync_ReturnsZero_WhenEmpty()
    {
        var commands = new FakeCommandDispatcher();
        var queries = new FakeQueryDispatcher(commands);
        var svc = new ReeveFundsService(commands, queries);

        int bal = await svc.GetHeldFundsAsync(PersonaId.FromCharacter(CharacterId.New()), "area1");
        Assert.That(bal, Is.EqualTo(0));
    }

    [Test]
    public async Task ReleaseHeldFundsAsync_WithdrawsAll_WhenRequestedZero()
    {
        var commands = new FakeCommandDispatcher();
        var queries = new FakeQueryDispatcher(commands);
        var svc = new ReeveFundsService(commands, queries);
        var persona = PersonaId.FromCharacter(CharacterId.New());

        // Deposit funds first
        await svc.DepositHeldFundsAsync(persona, "area1", 120, "test deposit");

        int granted = await svc.ReleaseHeldFundsAsync(persona, "area1", 0, async amt => true);
        Assert.That(granted, Is.EqualTo(120));
    }

    [Test]
    public async Task ReleaseHeldFundsAsync_RollsBack_WhenGrantFails()
    {
        var commands = new FakeCommandDispatcher();
        var queries = new FakeQueryDispatcher(commands);
        var svc = new ReeveFundsService(commands, queries);
        var persona = PersonaId.FromCharacter(CharacterId.New());

        // Deposit funds first
        await svc.DepositHeldFundsAsync(persona, "area1", 60, "test deposit");

        int granted = await svc.ReleaseHeldFundsAsync(persona, "area1", 50, async amt => false);
        Assert.That(granted, Is.EqualTo(0));

        int bal = await svc.GetHeldFundsAsync(persona, "area1");
        Assert.That(bal, Is.EqualTo(60));
    }
}

