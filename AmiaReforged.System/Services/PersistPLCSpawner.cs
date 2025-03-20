using System.Numerics;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(PersistPLCSpawner))]
public class PersistPLCSpawner
{
    private readonly PersistPLCService _persistPLCService;
    private readonly SchedulerService _schedulerService;
    private readonly List<string> serverAreaResref;
    private readonly List<NwArea> serverAreas;


    public PersistPLCSpawner(SchedulerService schedulerService, PersistPLCService persistPLCService)
    {
        _schedulerService = schedulerService;
        _persistPLCService = persistPLCService;
        
        serverAreas = NwModule.Instance.Areas.Where(a => a.Objects.Any(w => w.Tag == "is_area")).ToList();
        serverAreaResref = serverAreas.Select(a => a.ResRef).ToList();
        
        _schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
    }
    
    private async void Run()
    {
        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched") == 1) return;
        
        List<PersistPLC> persistPlc = await _persistPLCService.GetAllPersistPLCRecords();
        await NwTask.SwitchToMainThread();
        int count = persistPlc.Count;

        int i;
        for (i = 0; i < count; i++)
        {
            PersistPLC temp = persistPlc[i];
            string tempPlcName = temp.PLCName ?? throw new ArgumentNullException($"temp.PLCName");
            string areaResRef = temp.AreaResRef;
            int realResRefIndex = serverAreaResref.FindIndex(x => x.Contains(areaResRef));
            if (realResRefIndex == -1 || realResRefIndex >= serverAreas.Count) continue;

            uint realArea = serverAreas[realResRefIndex];
            string plcResRef = temp.PLCResRef;
            Vector3 vector = NWScript.Vector(temp.X, temp.Y, temp.Z);
            
            Location? location = NWScript.Location(realArea, vector, temp.Orientation);
            if(location is null) continue;
            
            uint tempObject = NWScript.CreateObject(64, plcResRef, location);
            NWScript.SetLocalInt(tempObject, sVarName: "persist", 1);
            NWScript.SetName(tempObject, tempPlcName);
            NWScript.SetDescription(tempObject, temp.PLCDescription);
            NWScript.SetUseableFlag(tempObject, 1);
            NWScript.SetPlotFlag(tempObject, 0);
            NWScript.SetObjectVisualTransform(tempObject, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, temp.Size);
        }


        NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched", 1);
    }
}