using System.Numerics;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

// [ServiceBinding(typeof(PersistPLCSpawner))]
public class PersistPlcSpawner
{
    private readonly PersistPLCService _persistPlcService;
    private readonly List<string> _serverAreaResref;
    private readonly List<NwArea> _serverAreas;


    public PersistPlcSpawner(SchedulerService schedulerService, PersistPLCService persistPlcService)
    {
        _persistPlcService = persistPlcService;
        
        _serverAreas = NwModule.Instance.Areas.Where(a => a.Objects.Any(w => w.Tag == "is_area")).ToList();
        _serverAreaResref = _serverAreas.Select(a => a.ResRef).ToList();
        
        schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
    }
    
    private async void Run()
    {
        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched") == 1) return;
        
        List<PersistPLC> persistPlc = await _persistPlcService.GetAllPersistPLCRecords();
        await NwTask.SwitchToMainThread();
        int count = persistPlc.Count;

        int i;
        for (i = 0; i < count; i++)
        {
            PersistPLC temp = persistPlc[i];
            string tempPlcName = temp.PLCName ?? throw new ArgumentNullException($"temp.PLCName");
            string areaResRef = temp.AreaResRef;
            int realResRefIndex = _serverAreaResref.FindIndex(x => x.Contains(areaResRef));
            if (realResRefIndex == -1 || realResRefIndex >= _serverAreas.Count) continue;

            uint realArea = _serverAreas[realResRefIndex];
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