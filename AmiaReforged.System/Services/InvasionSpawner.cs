using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using NWN.Core;
using AmiaReforged.System;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;

namespace AmiaReforged.System.Services;


[ServiceBinding(typeof(InvasionSpawner))]
public class InvasionSpawner
{
    private readonly SchedulerService _schedulerService;
    private readonly InvasionService _invasionService;
    private readonly Invasions _invasions;


    public InvasionSpawner(SchedulerService schedulerService,InvasionService invasionService, Invasions invasions)
    {
       _schedulerService = schedulerService;
       _schedulerService.ScheduleRepeating(InvasionOne, TimeSpan.FromMinutes(3));
       _schedulerService.ScheduleRepeating(InvasionTwo, TimeSpan.FromMinutes(10));
       _invasionService = invasionService; 
       _invasions = invasions;
    }

    public void InvasionOne()
    {
        if ((NWScript.GetLocalInt(NWScript.GetModule(), "Invasion1Fired") == 1))
        {
          return;
        }
        NWScript.SetLocalInt(NWScript.GetModule(), "Invasion1Fired", 1);
        CheckInvasions();
    }
    public void InvasionTwo()
    {
        if ((NWScript.GetLocalInt(NWScript.GetModule(), "Invasion2Fired") == 1))
        {
          return;
        }
        NWScript.SetLocalInt(NWScript.GetModule(), "Invasion2Fired", 1);
        CheckInvasions();
    }

    public async void CheckInvasions()
    {

        int counter = 1; 
        uint Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaResRef = NWScript.GetResRef(WaypointArea);
        List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords(); 
        List<InvasionRecord> invasionSuccess = new List<InvasionRecord>(); 
        List<uint> waypointSuccess =  new List<uint>(); 
        InvasionRecord invasionRecord = new(); 
        invasionRecord.AreaZone = "N/A";
        invasionRecord.InvasionPercent = 0; 
        invasionRecord.RealmChaos = 0; 
        InvasionRecord newRecord;
        Random random = new Random();
        int ran;


        while(NWScript.GetIsObjectValid(Waypoint)==1)
        {

            if(await _invasionService.InvasionRecordExists(AreaResRef) == false)
            {
              newRecord = new InvasionRecord(); 
              newRecord.AreaZone = AreaResRef;
              newRecord.InvasionPercent = random.Next(5,25);
              newRecord.RealmChaos = 1;
              await _invasionService.AddInvasionArea(newRecord);
            }
            else
            {
               invasionRecord = invasions.Find(x => x.AreaZone == AreaResRef);
               ran = random.Next(50, 100); // Only one with 50+ are ran
               invasionRecord.InvasionPercent += 5; 
               await _invasionService.UpdateInvasionArea(invasionRecord);
               if(ran <= invasionRecord.InvasionPercent)
               { 
                // Adds the successful rolls to an array to pick from later
                invasionSuccess.Add(invasionRecord);
                waypointSuccess.Add(Waypoint);
               }
            }

           counter++;
           Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
           if(NWScript.GetIsObjectValid(Waypoint)==1)
           { 
             WaypointArea = NWScript.GetArea(Waypoint);
             AreaResRef = NWScript.GetResRef(WaypointArea);
           }
        }

        PickInvasionLocation(invasionSuccess,waypointSuccess);
        
    }

    public async void PickInvasionLocation(List<InvasionRecord> invasionSuccess,List<uint> waypointSuccess)
    {
      int invasionSuccessCount = invasionSuccess.Count;
      int waypointSuccessCount = waypointSuccess.Count;
      Random random = new Random();
      int ran = random.Next(0, invasionSuccessCount);

      if(invasionSuccessCount != waypointSuccessCount)
      {
        NWScript.SendMessageToAllDMs("ERROR. Invasion arrays do not match. Inform Dev.");
      }
      else if(invasionSuccessCount == 0 || waypointSuccessCount == 0)
      {
        // Do nothing 
      }
      else // Picks a random one out of the successes to run and resets it
      {
        var tempInvasion = invasionSuccess[ran];
        var tempWP = waypointSuccess[ran];
        tempInvasion.InvasionPercent = 1; 
        tempInvasion.RealmChaos += random.Next(5,10); 
        await _invasionService.UpdateInvasionArea(tempInvasion);
        SummonInvasion(tempWP);
      }
    }

    public void SummonInvasion(uint Waypoint)
    {
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaName = NWScript.GetName(WaypointArea);
        string CreatureName = NWScript.GetLocalString(Waypoint,"invasionname");

        _invasions.InvasionGeneric(Waypoint,NWScript.GetLocalString(Waypoint,"creaturetype1"),
        NWScript.GetLocalString(Waypoint,"creaturetype2"),NWScript.GetLocalString(Waypoint,"creaturetype3"),
        NWScript.GetLocalString(Waypoint,"creaturetype4"),NWScript.GetLocalString(Waypoint,"creaturetype5"),
        NWScript.GetLocalString(Waypoint,"lieutentant"),NWScript.GetLocalString(Waypoint,"boss"),
        CreatureName,NWScript.GetLocalString(Waypoint,"overflow"));         
    }


}