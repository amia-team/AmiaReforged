using AmiaReforged.Core.Models.Settlement;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services.Settlements;

[ServiceBinding(typeof(StockpileDataService))]
public class StockpileDataService
{
    private readonly DatabaseContextFactory _contextFactory;

    public StockpileDataService(DatabaseContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Stockpile?> GetStockpileFromSettlement(long settlementId)
    {
        await using AmiaDbContext dbContext = _contextFactory.CreateDbContext();

        return await dbContext.Stockpiles
            .Include(s => s.ItemData)
            .FirstOrDefaultAsync(s => s.Id == settlementId);
    }
}