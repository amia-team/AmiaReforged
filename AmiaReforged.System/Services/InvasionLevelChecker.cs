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

[ServiceBinding(typeof(Invasions))]
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
      uint oUser = NWScript.GetArea(NWScript.OBJECT_SELF);
      string AreaResRef = NWScript.GetResRef(oUser);
      List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
      InvasionRecord invasionRecord = invasions.Find(x => x.AreaZone == AreaResRef); 
      NWScript.SendMessageToAllDMs("Area: " + AreaResRef + " | Invasion Percent: " + invasionRecord.InvasionPercent.ToString() + " | Realm Chaos: "  + invasionRecord.RealmChaos.ToString());
    }



}