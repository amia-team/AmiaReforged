using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(InvasionSpawner))]
public class InvasionSpawner
{
    private readonly Invasions _invasions;
    private readonly InvasionService _invasionService;
    private readonly SchedulerService _schedulerService;


    public InvasionSpawner(SchedulerService schedulerService, InvasionService invasionService, Invasions invasions)
    {
        _schedulerService = schedulerService;
        _schedulerService.ScheduleRepeating(InvasionOne, TimeSpan.FromMinutes(GenerateSpawnTime(1)));
        _schedulerService.ScheduleRepeating(InvasionTwo, TimeSpan.FromMinutes(GenerateSpawnTime(2)));
        _invasionService = invasionService;
        _invasions = invasions;
    }

    private int GenerateSpawnTime(int timeType)
    {
        Random random = new();
        int time = 10;
        if (timeType == 1)
            time = random.Next(10, 190);
        else if (timeType == 2) time = random.Next(200, 380);

        return time;
    }

    public void InvasionOne()
    {
        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "Invasion1Fired") == 1) return;
        NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "Invasion1Fired", 1);
        CheckInvasions();
    }

    public void InvasionTwo()
    {
        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "Invasion2Fired") == 1) return;
        NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "Invasion2Fired", 1);
        CheckInvasions();
    }

    public async void CheckInvasions()
    {
        int counter = 1;
        uint Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaResRef = NWScript.GetResRef(WaypointArea);
        List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
        List<InvasionRecord> invasionSuccess = new();
        List<uint> waypointSuccess = new();
        InvasionRecord invasionRecord = new();
        invasionRecord.AreaZone = "N/A";
        invasionRecord.InvasionPercent = 0;
        invasionRecord.RealmChaos = 0;
        InvasionRecord newRecord;
        Random random = new();
        int ran;


        while (NWScript.GetIsObjectValid(Waypoint) == 1)
        {
            if (await _invasionService.InvasionRecordExists(AreaResRef) == false)
            {
                newRecord = new InvasionRecord();
                newRecord.AreaZone = AreaResRef;
                newRecord.InvasionPercent = random.Next(5, 25);
                newRecord.RealmChaos = 1;
                await _invasionService.AddInvasionArea(newRecord);
                await NwTask.SwitchToMainThread();
            }
            else
            {
                invasionRecord = invasions.Find(x => x.AreaZone == AreaResRef);
                ran = random.Next(50, 100); // Only one with 50+ are ran
                invasionRecord.InvasionPercent += 6;
                await _invasionService.UpdateInvasionArea(invasionRecord);
                if (ran <= invasionRecord.InvasionPercent)
                {
                    // Adds the successful rolls to an array to pick from later
                    invasionSuccess.Add(invasionRecord);
                    waypointSuccess.Add(Waypoint);
                }
            }

            counter++;
            Waypoint = NWScript.GetWaypointByTag("Invasion" + Convert.ToString(counter));
            if (NWScript.GetIsObjectValid(Waypoint) == 1)
            {
                WaypointArea = NWScript.GetArea(Waypoint);
                AreaResRef = NWScript.GetResRef(WaypointArea);
            }
        }

        PickInvasionLocation(invasionSuccess, waypointSuccess);
    }

    public async void PickInvasionLocation(List<InvasionRecord> invasionSuccess, List<uint> waypointSuccess)
    {
        int invasionSuccessCount = invasionSuccess.Count;
        int waypointSuccessCount = waypointSuccess.Count;
        Random random = new();
        int ran = random.Next(0, invasionSuccessCount);

        if (invasionSuccessCount != waypointSuccessCount)
        {
            NWScript.SendMessageToAllDMs(szMessage: "ERROR. Invasion arrays do not match. Inform Dev.");
        }
        else if (invasionSuccessCount == 0 || waypointSuccessCount == 0)
        {
            // Do nothing 
        }
        else // Picks a random one out of the successes to run and resets it
        {
            InvasionRecord tempInvasion = invasionSuccess[ran];
            uint tempWP = waypointSuccess[ran];
            tempInvasion.InvasionPercent = 1;
            tempInvasion.RealmChaos += random.Next(10, 15);
            await _invasionService.UpdateInvasionArea(tempInvasion);
            SummonInvasion(tempWP, tempInvasion);
        }
    }

    public async void SummonInvasion(uint Waypoint, InvasionRecord Invasion)
    {
        uint WaypointArea = NWScript.GetArea(Waypoint);
        string AreaName = NWScript.GetName(WaypointArea);
        string CreatureName = NWScript.GetLocalString(Waypoint, sVarName: "invasionname");

        if (Invasion.RealmChaos > 100)
        {
            //Invasion.RealmChaos = 0; 
            await _invasionService.UpdateInvasionArea(Invasion);
            _invasions.AbyssalInvasion(Waypoint);
        }
        else
        {
            _invasions.InvasionGeneric(Waypoint, NWScript.GetLocalString(Waypoint, sVarName: "creaturetype1"),
                NWScript.GetLocalString(Waypoint, sVarName: "creaturetype2"),
                NWScript.GetLocalString(Waypoint, sVarName: "creaturetype3"),
                NWScript.GetLocalString(Waypoint, sVarName: "creaturetype4"),
                NWScript.GetLocalString(Waypoint, sVarName: "creaturetype5"),
                NWScript.GetLocalString(Waypoint, sVarName: "lieutenant"),
                NWScript.GetLocalString(Waypoint, sVarName: "boss"),
                CreatureName, NWScript.GetLocalString(Waypoint, sVarName: "overflow"), 0);
        }
    }
}