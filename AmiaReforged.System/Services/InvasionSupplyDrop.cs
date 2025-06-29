using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(InvasionSupplyDrop))]
public class InvasionSupplyDrop
{
    private readonly Invasions _invasions;
    private readonly InvasionService _invasionService;

    public InvasionSupplyDrop(InvasionService invasionService, Invasions invasions)
    {
        _invasionService = invasionService;
        _invasions = invasions;
    }

    [ScriptHandler(scriptName: "i_invasion_drop")]
    public async void InvasionDrop(CallInfo info)
    {
        Location Location = NWScript.GetItemActivatedTargetLocation();
        uint oPC = NWScript.GetItemActivator();
        uint Area = NWScript.GetAreaFromLocation(Location);
        string AreaName = NWScript.GetName(Area);
        uint Waypoint = new();
        string AreaResRef = NWScript.GetResRef(Area);
        int supplyCrate = NWScript.GetLocalInt(Area, sVarName: "supplycrate");
        InvasionRecord invasionRecord = new();
        invasionRecord.AreaZone = "N/A";
        invasionRecord.InvasionPercent = 0;
        invasionRecord.RealmChaos = 0;
        List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
        InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef);
        if (invasionRecordTemp != null)
        {
            if (supplyCrate < 2)
            {
                invasionRecord = invasionRecordTemp;
                Waypoint = GetInvasionWaypoint(Area);
                invasionRecord.InvasionPercent += 10;
                NWScript.SetLocalInt(Area, sVarName: "supplycrate", supplyCrate + 1);
                await _invasionService.UpdateInvasionArea(invasionRecord);
                NWScript.SendMessageToPC(oPC, szMessage: "*Supplies dropped! Some of the locals appear interested!*");
                NWScript.SendMessageToAllDMs("Supplies Dropped: " + AreaName + "by " + NWScript.GetName(oPC));
                NWScript.CreateObject(64, sTemplate: "invasion_crate", Location);
                NWScript.SetLocalString(NWScript.GetModule(), sVarName: "staffMessage",
                    "Supplies Dropped: " + AreaName + " by " + NWScript.GetName(oPC));
                NWScript.ExecuteScript(sScript: "webhook_staff");
            }
            else
            {
                NWScript.SendMessageToPC(oPC, szMessage: "*The area appears saturated with supplies already!*");
            }
        }
        else
        {
            NWScript.SendMessageToPC(oPC, szMessage: "*Supplies appear to do nothing. You can gather them back up.*");
            NWScript.CreateObject(2, sTemplate: "js_sco_supplycrt", Location);
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