using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(ElyonBossSpawner))]


public class ElyonBossSpawner
{
    private readonly SchedulerService _schedulerService;

    public ElyonBossSpawner(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService; 
        _schedulerService.ScheduleRepeating(LaunchBoss, TimeSpan.FromMinutes(GenerateSpawnTime()));  
    }

    private int GenerateSpawnChance()
    {
     Random random = new Random();
     int Chance = random.Next(1, 100);
     return Chance;  
    }

    private int GenerateSpawnTime()
    {
    int MaxSpawnTime = 320; // Minutes
    Random random = new Random();
    int Time = random.Next(1, MaxSpawnTime);
    return Time;
    }

    private int GenerateRandomWaypont()
    {
    int MaxWP = 4; // Waypoints
    Random random = new Random();
    int WP = random.Next(1, MaxWP);
    return WP;
    }

    public void SummonElyonBoss(uint Waypoint)
    {
    uint WaypointArea = NWScript.GetArea(Waypoint);
    string ResRef = NWScript.GetLocalString(Waypoint,"resRef");
    IntPtr location = NWScript.GetLocation(Waypoint);
    uint Boss = NWScript.CreateObject(1, ResRef, location);

    int Loot = 15; // Loot drops
    Random random = new Random();
    int LootDropNumber = random.Next(1, Loot);
    string LootDropResRef = "None";

    switch(LootDropNumber)
    {
        case 1: LootDropResRef = "Test"; break;
        case 2: LootDropResRef = "Test"; break;
        case 3: LootDropResRef = "Test"; break;
        case 4: LootDropResRef = "Test"; break;
        case 5: LootDropResRef = "Test"; break;
        case 6: LootDropResRef = "Test"; break;
        case 7: LootDropResRef = "Test"; break;
        case 8: LootDropResRef = "Test"; break;
        case 9: LootDropResRef = "Test"; break;
        case 10: LootDropResRef = "Test"; break;
        case 11: LootDropResRef = "Test"; break;
        case 12: LootDropResRef = "Test"; break;
        case 13: LootDropResRef = "Test"; break;
        case 14: LootDropResRef = "Test"; break;
        case 15: LootDropResRef = "Test"; break;
    }

    uint LootDrop = NWScript.CreateItemOnObject(LootDropResRef,Boss);
    NWScript.SetDroppableFlag(LootDrop,1);
    }

    private void LaunchBoss()
    { 
        int SpawnCheck = GenerateSpawnChance(); 
        int RandomWaypoint = GenerateRandomWaypont(); 

        uint Waypoint = NWScript.GetWaypointByTag("GlobalBossSpawn" + Convert.ToString(RandomWaypoint)); 
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string ResRef = NWScript.GetLocalString(Waypoint,"resRef");
        string CreatureName = NWScript.GetLocalString(Waypoint,"creatureName");
        string AreaName = NWScript.GetName(WaypointArea);

        if((NWScript.GetLocalInt(NWScript.GetModule(),"ElyonBossFired").Equals(0)) && ((1 <= SpawnCheck) && (SpawnCheck <= 25)))
        {
        NWScript.SetLocalString(NWScript.GetModule(),"announcerMessage","``` All adventurers begin to hear murmurs and rumors from locals about a terrifying creature loose on the isles. You quickly recieve a message from the Guilds to confirm this fact. The message is simple: WARNING! Extremely dangerous " + CreatureName + " is rampaging in " + AreaName + "! Avoid at all costs! ```");
        NWScript.SetLocalInt(NWScript.GetModule(),"ElyonBossFired",1);
        NWScript.ExecuteScript("webhook_announce");
        }
        else if(NWScript.GetLocalInt(NWScript.GetModule(),"ElyonBossFired").Equals(0))
        {
        NWScript.SetLocalString(NWScript.GetModule(),"announcerMessage","``` *All is quiet on Amia and her sister isles* ```");
        NWScript.SetLocalInt(NWScript.GetModule(),"ElyonBossFired",1);
        NWScript.ExecuteScript("webhook_announce");
        }
    }
}
