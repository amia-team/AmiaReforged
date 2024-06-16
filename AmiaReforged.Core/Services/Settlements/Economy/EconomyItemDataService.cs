using AmiaReforged.Core.Models.Settlement;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services.Settlements.Economy;

[ServiceBinding(typeof(EconomyItemDataService))]
public class EconomyItemDataService
{
    private readonly DatabaseContextFactory _ctxFactory;

    public EconomyItemDataService(DatabaseContextFactory ctxFactory)
    {
        _ctxFactory = ctxFactory;
    }

    /// <summary>
    /// Provides all stored economy items with their related material and quality populated.
    /// </summary>
    /// <returns>
    /// economyItems - A list of all economy items.
    /// </returns>
    public async Task<List<EconomyItem>> GetEconomyItems()
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        List<EconomyItem> economyItems = await ctx.EconomyItems
            .Include(m => m.Material)
            .Include(q => q.Quality)
            .ToListAsync();

        return economyItems;
    }

    public async Task<bool> ItemExists(string itemName)
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        return await ctx.EconomyItems
            .AnyAsync(e => e.Name == itemName);
    }

    public async Task UpdateItem(EconomyItem economyItem)
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        ctx.EconomyItems.Update(economyItem);
        await ctx.SaveChangesAsync();
    }

    public async Task AddItem(EconomyItem economyItem)
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        ctx.EconomyItems.Add(economyItem);
        await ctx.SaveChangesAsync();
    }

    public async Task<EconomyItem?> GetByName(string? itemName)
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        return await ctx.EconomyItems
            .FirstOrDefaultAsync(e => e.Name == itemName);
    }

}