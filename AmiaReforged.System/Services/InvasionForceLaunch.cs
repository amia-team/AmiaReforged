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

[ServiceBinding(typeof(InvasionForceLaunch))]
public class InvasionForceLaunch
{
    private readonly InvasionService _invasionService;
    private readonly Invasions _invasions;
    
    public InvasionForceLaunch(InvasionService invasionService, Invasions invasions)
    {
       _invasionService = invasionService; 
       _invasions = invasions;
    }
    
    [ScriptHandler("invasion_force")]
    public async void InvasionForcer(CallInfo info)
    {
      Location Location = NWScript.GetItemActivatedTargetLocation();
      uint Area = NWScript.GetAreaFromLocation(Location);
      uint Waypoint = new();
      string AreaResRef = NWScript.GetResRef(Area);
      InvasionRecord invasionRecord = new();
      invasionRecord.AreaZone = "N/A";
      invasionRecord.InvasionPercent = 0; 
      invasionRecord.RealmChaos = 0; 
      List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
      InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef); 
      if(invasionRecordTemp != null)
      {
        invasionRecord = invasionRecordTemp;
        Waypoint = GetInvasionWaypoint(Area);
        string AreaName = NWScript.GetName(Area);
        string CreatureName = NWScript.GetLocalString(Waypoint,"invasionname");
        string overflow = NWScript.GetLocalString(Waypoint,"overflow");

        if(invasionRecord.RealmChaos > 100)
        {
          invasionRecord.RealmChaos = 0; 
          await _invasionService.UpdateInvasionArea(invasionRecord);
          _invasions.AbyssalInvasion(Waypoint);
          invasionRecord.InvasionPercent = 1; 
          invasionRecord.RealmChaos = 0; 
          await _invasionService.UpdateInvasionArea(invasionRecord);
        }
        else
        {
          _invasions.InvasionGeneric(Waypoint,NWScript.GetLocalString(Waypoint,"creaturetype1"),
          NWScript.GetLocalString(Waypoint,"creaturetype2"),NWScript.GetLocalString(Waypoint,"creaturetype3"),
          NWScript.GetLocalString(Waypoint,"creaturetype4"),NWScript.GetLocalString(Waypoint,"creaturetype5"),
          NWScript.GetLocalString(Waypoint,"lieutentant"),NWScript.GetLocalString(Waypoint,"boss"),
          CreatureName,NWScript.GetLocalString(Waypoint,"overflow"),0);   
          invasionRecord.InvasionPercent = 1; 
          invasionRecord.RealmChaos += 5; 
          await _invasionService.UpdateInvasionArea(invasionRecord);
        }   
    
        NWScript.SendMessageToAllDMs("Invasion Forced");
      }
      else
      {
       NWScript.SendMessageToAllDMs("No Invasion Present to Force");
      }
    }

    public uint GetInvasionWaypoint(uint area)
    {
        int count = 1; 
        uint waypoint = NWScript.GetWaypointByTag("Invasion"+count.ToString());
        uint finalWaypoint = new(); 
        
        while(NWScript.GetIsObjectValid(waypoint)==1)
        {

          if(NWScript.GetArea(waypoint)==area)
          {
            finalWaypoint = waypoint;
            break;
          }  
          count++;
          waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(count));
        }

        return finalWaypoint;
    }
}