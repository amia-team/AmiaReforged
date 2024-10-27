using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using NWN.Core;
using AmiaReforged.System;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(InvasionLevelChecker))]
public class InvasionLevelChecker
{
    private readonly InvasionService _invasionService;
    private readonly Invasions _invasions;
    
    public InvasionLevelChecker(InvasionService invasionService, Invasions invasions)
    {
       _invasionService = invasionService; 
       _invasions = invasions;
    }
    
    [ScriptHandler("invasion_checker")]
    public async void InvasionChecker(CallInfo info)
    {
      Location Location = NWScript.GetItemActivatedTargetLocation();
      uint Area = NWScript.GetAreaFromLocation(Location);
      string AreaResRef = NWScript.GetResRef(Area);
      string AreaSet = "No Invasion Set";
      InvasionRecord invasionRecord = new();
      invasionRecord.AreaZone = "N/A";
      invasionRecord.InvasionPercent = 0; 
      invasionRecord.RealmChaos = 0; 
      List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
      InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef); 
      if(invasionRecordTemp != null)
      {
        invasionRecord = invasionRecordTemp;
        AreaSet = "Invasion Set";
      }
      NWScript.SendMessageToAllDMs("Area: " + AreaResRef + " | Set: " + AreaSet + " | Invasion Percent: " + invasionRecord.InvasionPercent.ToString() + " | Realm Chaos: "  + invasionRecord.RealmChaos.ToString());
    }
}