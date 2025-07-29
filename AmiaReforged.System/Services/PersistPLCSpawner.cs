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
    private readonly List<NwArea> _serverAreas;


    public PersistPlcSpawner(SchedulerService schedulerService, PersistPLCService persistPlcService)
    {
        _persistPlcService = persistPlcService;

        _serverAreas = NwModule.Instance.Areas.Where(a => a.Objects.Any(w => w.Tag == "is_area")).ToList();

        schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
    }

    private async void Run()
    {
        if (NWScript.GetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched") == 1) return;

        List<PersistPLC> persistPlc = await _persistPlcService.GetAllPersistPLCRecords();
        await NwTask.SwitchToMainThread();

        foreach (PersistPLC plc in persistPlc)
        {
            NwArea? plcArea = _serverAreas.FirstOrDefault(a => a.ResRef == plc.AreaResRef);
            if(plcArea == null) continue;
            Vector3 plcVector = new(plc.X, plc.Y, plc.Z);
            Location? plcLocation = NWScript.Location(plcArea, plcVector, plc.Orientation);
            
            if (plcLocation == null) continue;

            NwPlaceable? worldObject = NwPlaceable.Create(plc.PLCResRef, plcLocation);
            
            if(worldObject == null) continue;
            
            NWScript.SetLocalInt(worldObject, sVarName: "persist", 1);
            NWScript.SetName(worldObject, plc.PLCName);
            NWScript.SetDescription(worldObject, plc.PLCDescription);
            NWScript.SetUseableFlag(worldObject, 1);
            NWScript.SetPlotFlag(worldObject, 0);
            NWScript.SetObjectVisualTransform(worldObject, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, plc.Size);
        }

        NWScript.SetLocalInt(NWScript.GetModule(), sVarName: "PersistPLCLaunched", 1);
    }
}