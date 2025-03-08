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
        Random random = new();
        int Chance = random.Next(1, 100);
        return Chance;
    }

    private int GenerateSpawnTime()
    {
        int MaxSpawnTime = 320; // Minutes
        Random random = new();
        int Time = random.Next(1, MaxSpawnTime);
        return Time;
    }

    private int GenerateRandomWaypont()
    {
        int MaxWP = 30; // Waypoints
        Random random = new();
        int WP = random.Next(1, MaxWP);
        return WP;
    }

    public void SummonElyonBoss(uint Waypoint)
    {
        string ResRef = NWScript.GetLocalString(Waypoint, sVarName: "resRef");
        IntPtr location = NWScript.GetLocation(Waypoint);
        uint Boss = NWScript.CreateObject(1, ResRef, location);
    }

    private void MassWhisper(uint Waypoint)
    {
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string CreatureName = NWScript.GetLocalString(Waypoint, sVarName: "creatureName");
        string AreaName = NWScript.GetName(WaypointArea);

        uint oPC = NWScript.GetFirstPC();

        while (NWScript.GetIsObjectValid(oPC) == 1)
        {
            NWScript.SendMessageToPC(oPC, szMessage: "-----");
            NWScript.SendMessageToPC(oPC, szMessage: "-----");
            NWScript.SendMessageToPC(oPC,
                " All adventurers begin to hear murmurs and rumors from locals about a terrifying creature loose on the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! Extremely dangerous " +
                CreatureName + " is rampaging in " + AreaName +
                "! We recommend engaging only with a group of skilled adventurers! ");
            NWScript.SendMessageToPC(oPC, szMessage: "-----");
            NWScript.SendMessageToPC(oPC, szMessage: "-----");

            oPC = NWScript.GetNextPC();
        }
    }

    private void LaunchBoss()
    {
        int SpawnCheck = GenerateSpawnChance();
        int RandomWaypoint = GenerateRandomWaypont();

        uint Waypoint = NWScript.GetWaypointByTag("GlobalBossSpawn" + Convert.ToString(RandomWaypoint));
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string CreatureName = NWScript.GetLocalString(Waypoint, sVarName: "creatureName");
        string AreaName = NWScript.GetName(WaypointArea);

        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "ElyonBossFired").Equals(0) &&
            1 <= SpawnCheck && SpawnCheck <= 25)
        {
            NWScript.SetLocalString(NWScript.GetModule(), sVarName: "announcerMessage",
                "``` All adventurers begin to hear murmurs and rumors from locals about a terrifying creature loose on the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! Extremely dangerous " +
                CreatureName + " is rampaging in " + AreaName +
                "! We recommend engaging only with a group of skilled adventurers! ```");
            NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "ElyonBossFired", 1);
            NWScript.ExecuteScript(sScript: "webhook_announce");
            SummonElyonBoss(Waypoint);
            MassWhisper(Waypoint);
        }
        else if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "ElyonBossFired").Equals(0))
        {
            NWScript.SetLocalString(NWScript.GetModule(), sVarName: "announcerMessage",
                sValue: "``` *There are no reports of mythical creatures rampaging on Amia or her sister isles* ```");
            NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "ElyonBossFired", 1);
            NWScript.ExecuteScript(sScript: "webhook_announce");
        }
    }
}