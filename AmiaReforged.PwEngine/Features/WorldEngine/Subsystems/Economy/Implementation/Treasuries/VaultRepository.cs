using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Microsoft.EntityFrameworkCore;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;

[ServiceBinding(typeof(IVaultRepository))]
public sealed class VaultRepository : IVaultRepository
{
    private readonly IDbContextFactory<PwEngineContext> _contextFactory;

    public VaultRepository(IDbContextFactory<PwEngineContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<Vault> GetOrCreateAsync(Guid ownerCharacterId, string areaResRef, CancellationToken ct = default)
    {
        await using PwEngineContext db = _contextFactory.CreateDbContext();

        Vault? vault = await db.Vaults
            .FirstOrDefaultAsync(v => v.OwnerCharacterId == ownerCharacterId && v.AreaResRef == areaResRef, ct);

        if (vault != null)
        {
            return vault;
        }

        vault = new Vault
        {
            OwnerCharacterId = ownerCharacterId,
            AreaResRef = areaResRef,
            Balance = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await db.Vaults.AddAsync(vault, ct);
        await db.SaveChangesAsync(ct);
        return vault;
    }

    public async Task<int> GetBalanceAsync(Guid ownerCharacterId, string areaResRef, CancellationToken ct = default)
    {
        await using PwEngineContext db = _contextFactory.CreateDbContext();
        Vault? vault = await db.Vaults
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.OwnerCharacterId == ownerCharacterId && v.AreaResRef == areaResRef, ct);
        return vault?.Balance ?? 0;
    }

    public async Task<int> DepositAsync(Guid ownerCharacterId, string areaResRef, int amount, CancellationToken ct = default)
    {
        if (amount <= 0) return await GetBalanceAsync(ownerCharacterId, areaResRef, ct);

        await using PwEngineContext db = _contextFactory.CreateDbContext();
        Vault? vault = await db.Vaults
            .FirstOrDefaultAsync(v => v.OwnerCharacterId == ownerCharacterId && v.AreaResRef == areaResRef, ct);

        if (vault == null)
        {
            vault = new Vault
            {
                OwnerCharacterId = ownerCharacterId,
                AreaResRef = areaResRef,
                Balance = 0,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            await db.Vaults.AddAsync(vault, ct);
        }

        checked
        {
            vault.Balance += amount;
        }
        vault.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return vault.Balance;
    }

    public async Task<int> WithdrawAsync(Guid ownerCharacterId, string areaResRef, int amount, CancellationToken ct = default)
    {
        if (amount <= 0) return await GetBalanceAsync(ownerCharacterId, areaResRef, ct);

        await using PwEngineContext db = _contextFactory.CreateDbContext();
        Vault? vault = await db.Vaults
            .FirstOrDefaultAsync(v => v.OwnerCharacterId == ownerCharacterId && v.AreaResRef == areaResRef, ct);

        if (vault == null || vault.Balance <= 0)
        {
            return 0;
        }

        int toWithdraw = Math.Min(vault.Balance, amount);
        vault.Balance -= toWithdraw;
        vault.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return toWithdraw;
    }
}

