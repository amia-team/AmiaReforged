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


[ServiceBinding(typeof(InvasionSpawner))]
public class InvasionSpawner
{
    private readonly SchedulerService _schedulerService;
    private readonly InvasionService _invasionService;
    private readonly Invasions _invasions;


    public InvasionSpawner(SchedulerService schedulerService,InvasionService invasionService, Invasions invasions)
    {
       _schedulerService = schedulerService;
       _schedulerService.ScheduleRepeating(TestLaunch, TimeSpan.FromMinutes(3));
       _invasionService = invasionService; 
       _invasions = invasions;
    }

    public void TestLaunch()
    {
        uint Waypoint = NWScript.GetWaypointByTag("Invasion1");
        if ((NWScript.GetLocalInt(NWScript.GetModule(), "InvasionFired") == 1))
        {
         return;
        }
        SummonInvasion(Waypoint);
    }

    public async void CheckInvasions()
    {
        if ((NWScript.GetLocalInt(NWScript.GetModule(), "InvasionFired") == 1))
        {
          return;
        }

        int counter = 1; 
        uint Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaResRef = NWScript.GetResRef(WaypointArea);
        List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords(); 
        List<InvasionRecord> invasionSuccess = new List<InvasionRecord>(); 
        List<uint> waypointSuccess =  new List<uint>(); 
        InvasionRecord invasionRecord; 
        InvasionRecord newRecord;
        Random random = new Random();
        int ran;


        while(NWScript.GetIsObjectValid(Waypoint)==1)
        {

            if(await _invasionService.InvasionRecordExists(AreaResRef) == false)
            {
              newRecord = new InvasionRecord(); 
              newRecord.AreaZone = "AreaResRef";
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
               if(ran >= invasionRecord.InvasionPercent)
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
        await _invasionService.UpdateInvasionArea(tempInvasion);
        SummonInvasion(tempWP);
      }
    }


    public void SummonInvasion(uint Waypoint)
    {
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaName = NWScript.GetName(WaypointArea);
        string InvasionType = NWScript.GetLocalString(Waypoint, "invasionType");
        string CreatureName;

        if(InvasionType == "Beasts")
        {
          _invasions.InvasionBeasts(Waypoint); 
          CreatureName = "Beastmen";
        }
        else if(InvasionType == "Goblins")
        {
          _invasions.InvasionGoblins(Waypoint); 
          CreatureName = "Goblins";
        }
        else if(InvasionType == "Trolls")
        {
          _invasions.InvasionTrolls(Waypoint); 
          CreatureName = "Trolls";
        }
        else if(InvasionType == "Orcs")
        {
          _invasions.InvasionOrcs(Waypoint); 
          CreatureName = "Orcs";
        }
        else if(InvasionType == "Custom")
        {
          _invasions.InvasionGeneric(Waypoint,NWScript.GetLocalString(Waypoint,"creaturetype1"),
          NWScript.GetLocalString(Waypoint,"creaturetype2"),NWScript.GetLocalString(Waypoint,"creaturetype3"),
          NWScript.GetLocalString(Waypoint,"creaturetype4"),NWScript.GetLocalString(Waypoint,"creaturetype5"),
          NWScript.GetLocalString(Waypoint,"lieutentant"),NWScript.GetLocalString(Waypoint,"boss"),
          NWScript.GetLocalString(Waypoint,"message"));      
          CreatureName = "Unknowns";
        }
        else // Test stuff
        {
          _invasions.InvasionGoblins(Waypoint); 
          CreatureName = "Goblins";
        }

        
        NWScript.SetLocalString(NWScript.GetModule(), "announcerMessage",
        "``` All adventurers begin to hear murmurs and rumors from locals about a terrifying attack happening on the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! " +
        CreatureName + " are rampaging in " + AreaName +
        "! We recommend an appropriately skilled group of adventurers respond and common folk stay clear! ```");
        NWScript.ExecuteScript("webhook_announce");

        NWScript.SetLocalInt(NWScript.GetModule(), "InvasionFired", 1);

    }

}