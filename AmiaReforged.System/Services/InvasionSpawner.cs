using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using NWN.Core;
using AmiaReforged.System;
using AmiaReforged.Core;

namespace AmiaReforged.System.Services;



[ServiceBinding(typeof(InvasionSpawner))]
public class InvasionSpawner
{
    private readonly SchedulerService _schedulerService;
    private readonly InvasionService _invasionService;
    private readonly Invasions _invasions;


    public InvasionSpawner(SchedulerService schedulerService, Invasions invasions)
    {
       _schedulerService = schedulerService;
       _schedulerService.ScheduleRepeating(TestLaunch, TimeSpan.FromMinutes(10));
       _invasionService = invasionService; 
       _invasions = invasions;
    }

    public void TestLaunch()
    {
        uint Waypoint = NWScript.GetWaypointByTag("invasiongoblin");
        SummonInvasion(Waypoint);
    }

    public void CheckInvasions()
    {
        int counter = 1; 
        uint Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaResRef = NWScript.GetResRef(WaypointArea);
        List<InvasionRecord> invasions = _invasionService.GetAllInvasionRecords(); 
        InvasionRecord invasionRecord; 
        Random random = new Random();
        int ran;

        while(NWScript.GetIsObjectValid(Waypoint))
        {

            if(_invasionService.InvasionRecordExists(AreaResRef) == false)
            {
               _invasionService.AddInvasionArea(new InvasionRecord(AreaResRef,5));
            }
            else
            {
               invasionRecord = invasions.Find(x => x.AreaZone == AreaResRef);
               ran = random.Next(50, 100);
               if(ran >= invasionRecord.InvasionPercent)
               {
                SummonInvasion(Waypoint);
                break; 
               }
            }

           counter++;
           Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
           if(NWScript.GetIsObjectValid(Waypoint))
           { 
             WaypointArea = NWScript.GetArea(Waypoint);
             AreaResRef = NWScript.GetResRef(WaypointArea);
           }
        }
        
    }


    public void SummonInvasion(uint Waypoint)
    {
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaName = NWScript.GetAreaName(WaypointArea);
        string InvasionType = NWScript.GetLocalString(Waypoint, "invasionType");
        int CreatureName;

        if(InvasionType == "Beasts")
        {
          _invasions.InvasionBeasts(WaypointArea,11,8); 
          CreatureName = "Beastmen";
        }
        else if(InvasionType == "Goblins")
        {
          _invasions.InvasionGoblins(WaypointArea,11,8); 
          CreatureName = "Goblins";
        }
        else if(InvasionType == "Trolls")
        {
          _invasions.InvasionTrolls(WaypointArea,11,8); 
          CreatureName = "Trolls";
        }
        else if(InvasionType == "Orcs")
        {
          _invasions.InvasionOrcs(WaypointArea,11,8); 
          CreatureName = "Orcs";
        }
        else if(InvasionType == "Custom")
        {
          _invasions.InvasionGeneric(WaypointArea,11,8,NWScript.GetLocalString(Waypoint,"creaturetype1"),
          NWScript.GetLocalString(Waypoint,"creaturetype2"),NWScript.GetLocalString(Waypoint,"creaturetype3"),
          NWScript.GetLocalString(Waypoint,"creaturetype4"),NWScript.GetLocalString(Waypoint,"creaturetype5"),
          NWScript.GetLocalString(Waypoint,"lieutentant"),NWScript.GetLocalString(Waypoint,"boss"),
          NWScript.GetLocalString(Waypoint,"message"));      
          CreatureName = "Unknowns";
        }
        else // Test stuff
        {
          _invasions.InvasionGoblins(WaypointArea,11,8); 
          CreatureName = "Goblins";
        }

        
        NWScript.SetLocalString(NWScript.GetModule(), "announcerMessage",
        "``` All adventurers begin to hear murmurs and rumors from locals about a terrifying attack happening on the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! " +
        CreatureName + " are rampaging in " + AreaName +
        "! We recommend an appropriately skilled group of adventurers respond and common folk stay clear! ```");
        NWScript.ExecuteScript("webhook_announce");

    }

}