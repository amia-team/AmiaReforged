using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(ICoinhouseRepository))]
public class PersistentCoinhouseRepository(PwContextFactory factory) : ICoinhouseRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddNewCoinhouse(CoinHouse newCoinhouse)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.CoinHouses.Add(newCoinhouse);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public CoinHouse? GetCoinhouseByTag(CoinhouseTag tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.FirstOrDefault(x => x.Tag == tag);
    }

    public async Task<CoinhouseAccountDto?> GetAccountForAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouseAccount? account = await ctx.CoinHouseAccounts
            .Include(x => x.CoinHouse)
            .Include(x => x.AccountHolders)
            .Include(x => x.Receipts)
            .FirstOrDefaultAsync(
                a => a.Id == id ||
                     (a.AccountHolders != null && a.AccountHolders.Any(x => x.HolderId == id)),
                cancellationToken);

        return account?.ToDto();
    }

    public async Task SaveAccountAsync(CoinhouseAccountDto account, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouseAccount? existing = await ctx.CoinHouseAccounts
            .Include(a => a.AccountHolders)
            .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

        if (existing is null)
        {
            CoinHouseAccount entity = account.ToEntity();
            ctx.CoinHouseAccounts.Add(entity);
        }
        else
        {
            existing.UpdateFrom(account);
            SyncAccountHolders(ctx, existing, account.Holders);
        }

        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoinhouseAccountDto>> GetAccountsForHolderAsync(
        Guid holderId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        List<CoinHouseAccount> accounts = await ctx.CoinHouseAccounts
            .Include(x => x.CoinHouse)
            .Include(x => x.AccountHolders)
            .Where(x => x.AccountHolders != null && x.AccountHolders.Any(h => h.HolderId == holderId))
            .ToListAsync(cancellationToken);

        return accounts.Select(static a => a.ToDto()).ToList();
    }

    public CoinHouse? GetSettlementCoinhouse(SettlementId settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        // Implicit conversion from SettlementId to int
        CoinHouse? coinhouse = ctx.CoinHouses.FirstOrDefault(x => x.Settlement == settlementId);

        return coinhouse;
    }

    public async Task<CoinhouseDto?> GetByTagAsync(CoinhouseTag tag, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        // Implicit conversion from CoinhouseTag to string
        CoinHouse? coinhouse = await ctx.CoinHouses
            .FirstOrDefaultAsync(x => x.Tag == tag, cancellationToken);

        return coinhouse?.ToDto();
    }

    public async Task<CoinhouseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouse? coinhouse = await ctx.CoinHouses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return coinhouse?.ToDto();
    }

    public bool TagExists(CoinhouseTag tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.Any(x => x.Tag == tag);
    }

    public bool SettlementHasCoinhouse(SettlementId settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.Any(x => x.Settlement == settlementId);
    }

    private static void SyncAccountHolders(
        PwEngineContext ctx,
        CoinHouseAccount entity,
        IReadOnlyList<CoinhouseAccountHolderDto> holderDtos)
    {
        entity.AccountHolders ??= new List<CoinHouseAccountHolder>();

        Dictionary<long, CoinHouseAccountHolder> existingById = entity.AccountHolders
            .Where(h => h.Id != 0)
            .ToDictionary(h => h.Id);

        HashSet<long> retainedIds = new();

        foreach (CoinhouseAccountHolderDto holderDto in holderDtos)
        {
            if (holderDto.Id is { } id && id != 0 && existingById.TryGetValue(id, out CoinHouseAccountHolder existing))
            {
                existing.FirstName = holderDto.FirstName;
                existing.LastName = holderDto.LastName;
                existing.Type = holderDto.Type;
                existing.Role = holderDto.Role;
                existing.HolderId = holderDto.HolderId;
                retainedIds.Add(id);
            }
            else
            {
                CoinHouseAccountHolder newHolder = holderDto.ToEntity(entity.Id);
                entity.AccountHolders.Add(newHolder);

                if (newHolder.Id != 0)
                {
                    retainedIds.Add(newHolder.Id);
                }
            }
        }

        List<CoinHouseAccountHolder> toRemove = entity.AccountHolders
            .Where(h => h.Id != 0 && !retainedIds.Contains(h.Id))
            .ToList();

        foreach (CoinHouseAccountHolder holder in toRemove)
        {
            entity.AccountHolders.Remove(holder);
            ctx.CoinHouseAccountHolders.Remove(holder);
        }
    }
}
