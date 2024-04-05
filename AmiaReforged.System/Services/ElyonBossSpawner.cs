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

    private void LaunchBoss()
    { 
        int SpawnCheck = GenerateSpawnChance();

        if((NWScript.GetLocalInt(NWScript.GetModule(),"ElyonBossFired").Equals(0)) && ((1 <= SpawnCheck) && (SpawnCheck <= 25)))
        {
        NWScript.SetLocalString(NWScript.GetModule(),"announcerMessage","ElyonBossSpawner FIRED! Should be between 1 to 320 minutes after Test Launch text.");
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