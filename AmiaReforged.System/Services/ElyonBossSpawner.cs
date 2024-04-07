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
        _schedulerService.ScheduleRepeating(LaunchBoss, TimeSpan.FromMinutes(2));  //GenerateSpawnTime()
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

    uint LootDrop = NWScript.CreateItemOnObject("elyon_epiclt_" + Convert.ToString(LootDropNumber),Boss);
    NWScript.SetDroppableFlag(LootDrop,1);
    }

    private void LaunchBoss()
    { 
        int SpawnCheck = 10; // GenerateSpawnChance()
        int RandomWaypoint = GenerateRandomWaypont(); 

        uint Waypoint = NWScript.GetWaypointByTag("GlobalBossSpawn" + Convert.ToString(RandomWaypoint)); 
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string ResRef = NWScript.GetLocalString(Waypoint,"resRef");
        string CreatureName = NWScript.GetLocalString(Waypoint,"creatureName");
        string AreaName = NWScript.GetName(WaypointArea);

        if((NWScript.GetLocalInt(NWScript.GetModule(),"ElyonBossFired").Equals(0)) && ((1 <= SpawnCheck) && (SpawnCheck <= 25)))
        {
        NWScript.SetLocalString(NWScript.GetModule(),"announcerMessage","*All adventurers recieve an urgent bombardment of sendings from the Guild!* WARNING! Extremely dangerous " + CreatureName + " is rampaging in " + AreaName + "! BE CAUTIOUS!");
        NWScript.SetLocalInt(NWScript.GetModule(),"ElyonBossFired",1);
        NWScript.ExecuteScript("webhook_announce");
        }
        else if(NWScript.GetLocalInt(NWScript.GetModule(),"ElyonBossFired").Equals(0))
        {
        NWScript.SetLocalString(NWScript.GetModule(),"announcerMessage","ElyonBossSpawner will NOT fire!");
        NWScript.SetLocalInt(NWScript.GetModule(),"ElyonBossFired",1);
        NWScript.ExecuteScript("webhook_announce");
        }
    }
}
