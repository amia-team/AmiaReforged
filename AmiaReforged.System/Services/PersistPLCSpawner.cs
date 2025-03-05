using System.Numerics;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(PersistPLCSpawner))]
public class PersistPLCSpawner
{
    private readonly PersistPLCService _persistPLCService;
    private readonly SchedulerService _schedulerService;
    private readonly List<string> serverAreaResref;
    private readonly List<uint> serverAreas;


    public PersistPLCSpawner(SchedulerService schedulerService, PersistPLCService persistPLCService)
    {
        _schedulerService = schedulerService;
        _persistPLCService = persistPLCService;
        serverAreas = new();
        serverAreaResref = new();
        _schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
    }

    public void GatherAreas()
    {
        int count = 0;
        uint tempWP = NWScript.GetObjectByTag(sTag: "is_area", count);

        while (NWScript.GetIsObjectValid(tempWP) == 1)
        {
            uint tempArea = NWScript.GetArea(tempWP);
            serverAreas.Add(tempArea);
            serverAreaResref.Add(NWScript.GetResRef(tempArea));
            count++;
            tempWP = NWScript.GetObjectByTag(sTag: "is_area", count);
        }
    }

    public async void Run()
    {
        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched") == 1) return;

        GatherAreas();

        List<PersistPLC> persistPLC = await _persistPLCService.GetAllPersistPLCRecords();
        int count = persistPLC.Count;

        int i;
        for (i = 0; i < count; i++)
        {
            PersistPLC temp = persistPLC[i];
            string tempPLCName = temp.PLCName;
            string areaResRef = temp.AreaResRef;
            int realResRefIndex = serverAreaResref.FindIndex(x => x.Contains(areaResRef));
            if (realResRefIndex == -1 || realResRefIndex >= serverAreas.Count) continue;

            uint realArea = serverAreas[realResRefIndex];
            string PLCResRef = temp.PLCResRef;
            Vector3 vector = NWScript.Vector(temp.X, temp.Y, temp.Z);
            Location location = NWScript.Location(realArea, vector, temp.Orientation);
            uint tempObject = NWScript.CreateObject(64, PLCResRef, location);
            NWScript.SetLocalInt(tempObject, sVarName: "persist", 1);
            NWScript.SetName(tempObject, tempPLCName);
            NWScript.SetDescription(tempObject, temp.PLCDescription);
            NWScript.SetUseableFlag(tempObject, 1);
            NWScript.SetPlotFlag(tempObject, 0);
            NWScript.SetObjectVisualTransform(tempObject, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, temp.Size);
        }


        NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched", 1);
    }
}