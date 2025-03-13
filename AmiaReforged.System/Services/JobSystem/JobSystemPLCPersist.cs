using System.Numerics;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(JobSystemPLCPersist))]
public class JobSystemPLCPersist
{
    private readonly PersistPLCService _persistPLCService;


    public JobSystemPLCPersist(PersistPLCService persistPLCService)
    {
        _persistPLCService = persistPLCService;
    }

    [ScriptHandler(scriptName: "js_persist_add")]
    public async void JobSystemPLCPersisSave(CallInfo info)
    {
        uint Player = NWScript.OBJECT_SELF;
        uint FactionKey = NWScript.GetItemPossessedBy(Player,
            NWScript.GetLocalString(NWScript.GetArea(Player), sVarName: "persist_faction"));

        if (NWScript.GetLocalInt(NWScript.GetArea(Player), sVarName: "block_persist") == 1)
        {
            NWScript.SendMessageToPC(Player, szMessage: "*This area does not allow persist PLC placement*");
            return;
        }

        if (NWScript.GetLocalString(NWScript.GetArea(Player), sVarName: "persist_faction") != "")
            if (NWScript.GetIsObjectValid(FactionKey) != 1)
            {
                NWScript.SendMessageToPC(Player,
                    szMessage: "*This area does not allow persist PLC placement by non faction members*");
                return;
            }

        uint PLC = NWScript.GetLocalObject(Player, sVarName: "pcplc");
        uint PLCWidget = NWScript.GetLocalObject(Player, sVarName: "plcwidget");
        NWScript.SetLocalInt(PLC, sVarName: "persist", 1);
        NWScript.SetPlotFlag(PLC, 0);
        NWScript.SetUseableFlag(PLC, 1);
        PersistPLC newPLC = new();
        Location location = NWScript.GetLocation(PLC);
        Vector3 vectorLocation = NWScript.GetPositionFromLocation(location);
        float facing = NWScript.GetFacing(PLC);
        uint Area = NWScript.GetArea(PLC);

        newPLC.AreaResRef = NWScript.GetResRef(Area);
        newPLC.PLCName = NWScript.GetName(PLC);
        newPLC.PLCResRef = NWScript.GetResRef(PLC);
        newPLC.PLCDescription = NWScript.GetDescription(PLC);
        newPLC.X = vectorLocation.X;
        newPLC.Y = vectorLocation.Y;
        newPLC.Z = vectorLocation.Z;
        newPLC.Orientation = facing;
        newPLC.Size = NWScript.GetObjectVisualTransform(PLC, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE);

        await _persistPLCService.AddPersistPLC(newPLC);
        await NwTask.SwitchToMainThread();
        NWScript.DestroyObject(PLCWidget);

        NWScript.SetLocalString(NWScript.GetModule(), sVarName: "staffMessage", "Persist PLC Laid by " +
            NWScript.GetName(Player) + " in " + NWScript.GetName(Area) + ". Info --  " + " ResRef: " +
            NWScript.GetResRef(PLC) + " , Name: " + NWScript.GetName(PLC) + " , X: " +
            NWScript.FloatToString(vectorLocation.X) + " , Y: " + NWScript.FloatToString(vectorLocation.Y) + " , Z: " +
            NWScript.FloatToString(vectorLocation.Z) + " , Facing: " + NWScript.FloatToString(NWScript.GetFacing(PLC)) +
            " , Bio: " + NWScript.GetDescription(PLC));
        NWScript.ExecuteScript(sScript: "webhook_staff");
    }

    [ScriptHandler(scriptName: "js_persist_del")]
    public async void JobSystemPLCPersisDelete(CallInfo info)
    {
        uint PLC = NWScript.OBJECT_SELF;

        if (NWScript.GetLocalInt(PLC, sVarName: "persist") == 0) return;

        Location location = NWScript.GetLocation(PLC);
        Vector3 vectorLocation = NWScript.GetPositionFromLocation(location);
        uint Area = NWScript.GetArea(PLC);
        uint Killer = NWScript.GetLastKiller();

        Predicate<PersistPLC> searcharea = p => { return p.AreaResRef == NWScript.GetResRef(Area); };
        Predicate<PersistPLC> searchx = p => { return p.X == vectorLocation.X; };
        Predicate<PersistPLC> searchxy = p => { return p.Y == vectorLocation.Y; };
        Predicate<PersistPLC> searchxyz = p => { return p.Z == vectorLocation.Z; };

        List<PersistPLC> persistPLC = await _persistPLCService.GetAllPersistPLCRecords();
        List<PersistPLC> persistPLCArea = persistPLC.FindAll(searcharea);
        List<PersistPLC> persistPLCAreax = persistPLCArea.FindAll(searchx);
        List<PersistPLC> persistPLCAreaxy = persistPLCAreax.FindAll(searchxy);
        PersistPLC persistPLCAreaxyz = persistPLCAreax.Find(searchxyz);

        if (persistPLCAreaxyz.PLCResRef == NWScript.GetResRef(PLC) &&
            persistPLCAreaxyz.PLCName == NWScript.GetName(PLC) &&
            persistPLCAreaxyz.Orientation == NWScript.GetFacing(PLC))
        {
            await _persistPLCService.DeletePersistPLC(persistPLCAreaxyz);
            await NwTask.SwitchToMainThread();

            NWScript.SetLocalString(NWScript.GetModule(), sVarName: "staffMessage", "Persist PLC Destroyed by " +
                NWScript.GetName(Killer) + " in " + NWScript.GetName(Area) + ". Info --  " + " ResRef: " +
                NWScript.GetResRef(PLC) + " , Name: " + NWScript.GetName(PLC) + " , X: " +
                NWScript.FloatToString(vectorLocation.X) + " , Y: " + NWScript.FloatToString(vectorLocation.Y) +
                " , Z: " + NWScript.FloatToString(vectorLocation.Z) + " , Facing: " +
                NWScript.FloatToString(NWScript.GetFacing(PLC)) + " , Bio: " + NWScript.GetDescription(PLC));
            NWScript.ExecuteScript(sScript: "webhook_staff");
        }
        else
        {
            NWScript.SendMessageToAllDMs(szMessage: "Error: Record not found to delete");
        }
    }
}