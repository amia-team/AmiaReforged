using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Creates a new coinhouse record. Fails if the tag already exists.
/// </summary>
public record CreateCoinhouseCommand : ICommand
{
    public required CoinHouse Coinhouse { get; init; }
}

/// <summary>
/// Updates an existing coinhouse record (tag is immutable). Fails if unknown.
/// </summary>
public record UpdateCoinhouseCommand : ICommand
{
    public required string Tag { get; init; }
    public required CoinHouse Coinhouse { get; init; }
}

/// <summary>
/// Deletes a coinhouse record and its accounts, holders, and transactions.
/// Fails if unknown.
/// </summary>
public record DeleteCoinhouseCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateCoinhouseCommand>))]
public sealed class CreateCoinhouseHandler : ICommandHandler<CreateCoinhouseCommand>
{
    private readonly PwContextFactory _contextFactory;

    public CreateCoinhouseHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(CreateCoinhouseCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();

        bool exists = await context.CoinHouses.AnyAsync(c => c.Tag == command.Coinhouse.Tag, cancellationToken);
        if (exists)
            return CommandResult.Fail($"A coinhouse with tag '{command.Coinhouse.Tag}' already exists");

        context.CoinHouses.Add(command.Coinhouse);
        await context.SaveChangesAsync(cancellationToken);

        return CommandResult.OkWith("Tag", command.Coinhouse.Tag);
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateCoinhouseCommand>))]
public sealed class UpdateCoinhouseHandler : ICommandHandler<UpdateCoinhouseCommand>
{
    private readonly PwContextFactory _contextFactory;

    public UpdateCoinhouseHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(UpdateCoinhouseCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        CoinHouse? existing = await context.CoinHouses
            .FirstOrDefaultAsync(c => c.Tag == command.Tag, cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No coinhouse with tag '{command.Tag}'");

        // Tag is immutable — update mutable fields only.
        existing.Settlement = command.Coinhouse.Settlement;
        existing.EngineId = command.Coinhouse.EngineId;
        existing.StoredGold = command.Coinhouse.StoredGold;
        existing.PersonaIdString = command.Coinhouse.PersonaIdString;

        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteCoinhouseCommand>))]
public sealed class DeleteCoinhouseHandler : ICommandHandler<DeleteCoinhouseCommand>
{
    private readonly PwContextFactory _contextFactory;

    public DeleteCoinhouseHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(DeleteCoinhouseCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        CoinHouse? existing = await context.CoinHouses
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Tag == command.Tag, cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No coinhouse with tag '{command.Tag}'");

        // Remove associated accounts, holders, and transactions.
        if (existing.Accounts is { Count: > 0 })
        {
            foreach (CoinHouseAccount account in existing.Accounts)
            {
                List<CoinHouseAccountHolder> holders = await context.CoinHouseAccountHolders
                    .Where(h => h.AccountId == account.Id)
                    .ToListAsync(cancellationToken);
                context.CoinHouseAccountHolders.RemoveRange(holders);

                List<CoinHouseTransaction> transactions = await context.CoinHouseTransactions
                    .Where(t => t.CoinHouseAccountId == account.Id)
                    .ToListAsync(cancellationToken);
                context.CoinHouseTransactions.RemoveRange(transactions);
            }

            context.CoinHouseAccounts.RemoveRange(existing.Accounts);
        }

        context.CoinHouses.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}
