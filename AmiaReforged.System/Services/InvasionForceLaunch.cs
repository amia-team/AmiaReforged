using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(InvasionForceLaunch))]
public class InvasionForceLaunch
{
    private readonly Invasions _invasions;
    private readonly InvasionService _invasionService;

    public InvasionForceLaunch(InvasionService invasionService, Invasions invasions)
    {
        _invasionService = invasionService;
        _invasions = invasions;
    }

    [ScriptHandler(scriptName: "invasion_force")]
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
        if (invasionRecordTemp != null)
        {
            invasionRecord = invasionRecordTemp;
            Waypoint = GetInvasionWaypoint(Area);
            string AreaName = NWScript.GetName(Area);
            string CreatureName = NWScript.GetLocalString(Waypoint, sVarName: "invasionname");
            string overflow = NWScript.GetLocalString(Waypoint, sVarName: "overflow");

            if (invasionRecord.RealmChaos > 100)
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
                _invasions.InvasionGeneric(Waypoint, NWScript.GetLocalString(Waypoint, sVarName: "creaturetype1"),
                    NWScript.GetLocalString(Waypoint, sVarName: "creaturetype2"),
                    NWScript.GetLocalString(Waypoint, sVarName: "creaturetype3"),
                    NWScript.GetLocalString(Waypoint, sVarName: "creaturetype4"),
                    NWScript.GetLocalString(Waypoint, sVarName: "creaturetype5"),
                    NWScript.GetLocalString(Waypoint, sVarName: "lieutenant"),
                    NWScript.GetLocalString(Waypoint, sVarName: "boss"),
                    CreatureName, NWScript.GetLocalString(Waypoint, sVarName: "overflow"), 0);
                invasionRecord.InvasionPercent = 1;
                invasionRecord.RealmChaos += 5;
                await _invasionService.UpdateInvasionArea(invasionRecord);
            }

            NWScript.SendMessageToAllDMs(szMessage: "Invasion Forced");
        }
        else
        {
            NWScript.SendMessageToAllDMs(szMessage: "No Invasion Present to Force");
        }
    }

    public uint GetInvasionWaypoint(uint area)
    {
        int count = 1;
        uint waypoint = NWScript.GetWaypointByTag("Invasion" + count);
        uint finalWaypoint = new();

        while (NWScript.GetIsObjectValid(waypoint) == 1)
        {
            if (NWScript.GetArea(waypoint) == area)
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