using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Settlements;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Settlements.Services.ResourceManagement;


[ServiceBinding(typeof(SettlementCacheService))]
public class SettlementCacheService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private readonly SettlementDataService _settlementData;

    public IReadOnlyDictionary<int, Stockpile> Stockpiles { get; private set; }

    public SettlementCacheService(SettlementDataService settlementData)
    {
        _settlementData = settlementData;        
        // Initialize the settlement system.
        InitializeSettlementSystem();
        
        Log.Info("Settlement system initialized.");
    }

    private async void InitializeSettlementSystem()
    {
        Log.Info("Initializing settlement system...");
        
        await CacheStockpiles();

        await NwTask.SwitchToMainThread();
    }

    private async Task CacheStockpiles()
    {
        // get all of the settlements 
        // add their stockpiles to the dictionary, keying off their name
        Dictionary<int, Stockpile>? stockpiles = new();
        IEnumerable<Settlement> settlements = await _settlementData.GetAllSettlements();
        
        foreach (Settlement settlement in settlements)
        {
            //TODO: Remove logging before release
            Log.Info($"Caching stockpile for {settlement.Name}");
            
            stockpiles.TryAdd(settlement.Id, settlement.Stockpile);
        }
        
        Stockpiles = stockpiles;
    }
}