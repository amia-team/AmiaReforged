using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Admin;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinRentals;

/// <summary>
/// Repository implementation for managing dreamcoin rentals.
/// </summary>
[ServiceBinding(typeof(IDreamcoinRentalRepository))]
public sealed class DreamcoinRentalRepository(IDbContextFactory<PwEngineContext> factory) : IDreamcoinRentalRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task<DreamcoinRental?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            return await ctx.DreamcoinRentals
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dreamcoin rental {Id}", id);
            throw;
        }
    }

    public async Task<List<DreamcoinRental>> GetByPlayerCdKeyAsync(string cdKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            return await ctx.DreamcoinRentals
                .AsNoTracking()
                .Where(r => r.PlayerCdKey == cdKey)
                .OrderByDescending(r => r.CreatedUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load dreamcoin rentals for CD Key {CdKey}", cdKey);
            throw;
        }
    }

    public async Task<List<DreamcoinRental>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            return await ctx.DreamcoinRentals
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.CreatedUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load active dreamcoin rentals");
            throw;
        }
    }

    public async Task<List<DreamcoinRental>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            return await ctx.DreamcoinRentals
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load all dreamcoin rentals");
            throw;
        }
    }

    public async Task<List<DreamcoinRental>> GetRentalsDueForPaymentAsync(DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            return await ctx.DreamcoinRentals
                .AsNoTracking()
                .Where(r => r.IsActive && r.NextDueDateUtc <= asOfDate)
                .OrderBy(r => r.NextDueDateUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load rentals due for payment as of {Date}", asOfDate);
            throw;
        }
    }

    public async Task<List<DreamcoinRental>> GetDelinquentRentalsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            return await ctx.DreamcoinRentals
                .AsNoTracking()
                .Where(r => r.IsActive && r.IsDelinquent)
                .OrderByDescending(r => r.NextDueDateUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load delinquent rentals");
            throw;
        }
    }

    public async Task AddAsync(DreamcoinRental rental, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            ctx.DreamcoinRentals.Add(rental);
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add dreamcoin rental for CD Key {CdKey}", rental.PlayerCdKey);
            throw;
        }
    }

    public async Task UpdateAsync(DreamcoinRental rental, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            ctx.DreamcoinRentals.Update(rental);
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update dreamcoin rental {Id}", rental.Id);
            throw;
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            DreamcoinRental? rental = await ctx.DreamcoinRentals.FindAsync([id], cancellationToken);
            if (rental != null)
            {
                ctx.DreamcoinRentals.Remove(rental);
                await ctx.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete dreamcoin rental {Id}", id);
            throw;
        }
    }

    public async Task MarkDelinquentAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            DreamcoinRental? rental = await ctx.DreamcoinRentals.FindAsync([id], cancellationToken);
            if (rental != null)
            {
                rental.IsDelinquent = true;
                await ctx.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to mark dreamcoin rental {Id} as delinquent", id);
            throw;
        }
    }

    public async Task MarkPaidAsync(int id, DateTime paymentDate, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            DreamcoinRental? rental = await ctx.DreamcoinRentals.FindAsync([id], cancellationToken);
            if (rental != null)
            {
                rental.IsActive = true;
                rental.IsDelinquent = false;
                rental.LastPaymentUtc = paymentDate;
                // Set next due date to the 1st of the next month
                rental.NextDueDateUtc = new DateTime(paymentDate.Year, paymentDate.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                    .AddMonths(1);
                await ctx.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to mark dreamcoin rental {Id} as paid", id);
            throw;
        }
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            DreamcoinRental? rental = await ctx.DreamcoinRentals.FindAsync([id], cancellationToken);
            if (rental != null)
            {
                rental.IsActive = false;
                await ctx.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to deactivate dreamcoin rental {Id}", id);
            throw;
        }
    }

    public async Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = await factory.CreateDbContextAsync(cancellationToken);
            DreamcoinRental? rental = await ctx.DreamcoinRentals.FindAsync([id], cancellationToken);
            if (rental != null)
            {
                rental.IsActive = true;
                // Reset next due date to the 1st of next month
                DateTime now = DateTime.UtcNow;
                rental.NextDueDateUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                await ctx.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to reactivate dreamcoin rental {Id}", id);
            throw;
        }
    }
}
