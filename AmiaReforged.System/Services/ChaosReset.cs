using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(ChaosReset))]
public class ChaosReset
{
    private readonly Invasions _invasions;
    private readonly InvasionService _invasionService;

    public ChaosReset(InvasionService invasionService, Invasions invasions)
    {
        _invasionService = invasionService;
        _invasions = invasions;
    }

    [ScriptHandler(scriptName: "chaos_reset")]
    public async void ChaosResetter(CallInfo info)
    {
        uint Creature = NWScript.OBJECT_SELF;
        Location Location = NWScript.GetLocation(Creature);
        uint Area = NWScript.GetAreaFromLocation(Location);
        string AreaResRef = NWScript.GetResRef(Area);
        InvasionRecord invasionRecord = new();
        invasionRecord.AreaZone = "N/A";
        invasionRecord.InvasionPercent = 0;
        invasionRecord.RealmChaos = 0;
        List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
        InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef);
        if (invasionRecordTemp != null)
        {
            invasionRecord = invasionRecordTemp;
            invasionRecord.RealmChaos = 0;
            await _invasionService.UpdateInvasionArea(invasionRecord);
        }
    }
}