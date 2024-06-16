using AmiaReforged.Core.Models.Settlement;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services.Settlements;

[ServiceBinding(typeof(SettlementDataService))]
public class SettlementDataService
{
    private readonly DatabaseContextFactory _ctxFactory;

    public SettlementDataService(DatabaseContextFactory ctxFactory)
    {
        _ctxFactory = ctxFactory;
    }
    
    public async Task<List<Settlement>> GetAllSettlements()
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        List<Settlement> settlements = await ctx.Settlements
            .Include(s => s.Stockpile)
            .ThenInclude(s => s.ItemData)
            .ToListAsync();

        return settlements;
    }

    public async Task<Settlement?> GetSettlementById(int id)
    {
        await using AmiaDbContext ctx = _ctxFactory.CreateDbContext();

        Settlement? settlement = await ctx.Settlements
            .Include(s => s.Stockpile)
            .ThenInclude(s => s.ItemData)
            .FirstOrDefaultAsync(s => s.Id == id);

        return settlement;
    }
}