using System;
using System.Data.Common;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using System.Numerics;
using AmiaReforged.System;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using NWN.Core.NWNX;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AmiaReforged.Core.UserInterface;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(JobSystemPLCPersist))]
public class JobSystemPLCPersist
{
    private readonly PersistPLCService _persistPLCService;


    public JobSystemPLCPersist(PersistPLCService persistPLCService)
    {
        _persistPLCService = persistPLCService;
    }

    [ScriptHandler("js_persist_add")]
    public async void JobSystemPLCPersisSave(CallInfo info)
    {
        uint Player = NWScript.OBJECT_SELF; 
        uint FactionKey = NWScript.GetItemPossessedBy(Player,NWScript.GetLocalString(NWScript.GetArea(Player), "persist_faction"));
        
        if ((NWScript.GetLocalInt(NWScript.GetArea(Player), "block_persist") == 1))
        {
          NWScript.SendMessageToPC(Player,"*This area does not allow persist PLC placement*");
          return;
        }
        else if((NWScript.GetLocalString(NWScript.GetArea(Player), "persist_faction") != ""))
        {
          if(NWScript.GetIsObjectValid(FactionKey)!=1)
          {
           NWScript.SendMessageToPC(Player,"*This area does not allow persist PLC placement by non faction members*");
           return;
          }
        }

        uint PLC = NWScript.GetLocalObject(Player,"pcplc");
        uint PLCWidget = NWScript.GetLocalObject(Player,"plcwidget");
        NWScript.SetLocalInt(PLC,"persist",1);
        NWScript.SetUseableFlag(PLC,1);
        NWScript.SetPlotFlag(PLC,0);
        PersistPLC newPLC = new PersistPLC(); 
        Location location = NWScript.GetLocation(PLC);
        Vector3 vectorLocation = NWScript.GetPositionFromLocation(location); 
        float facing = NWScript.GetFacing(PLC);
        uint Area =  NWScript.GetArea(PLC); 

        newPLC.AreaResRef = NWScript.GetResRef(Area); 
        newPLC.PLCName = NWScript.GetName(PLC); 
        newPLC.PLCResRef = NWScript.GetResRef(PLC); 
        newPLC.PLCDescription = NWScript.GetDescription(PLC); 
        newPLC.X = vectorLocation.X;
        newPLC.Y = vectorLocation.Y; 
        newPLC.Z = vectorLocation.Z; 
        newPLC.Orientation = facing;
        newPLC.Size = NWScript.GetObjectVisualTransform(PLC,NWScript.OBJECT_VISUAL_TRANSFORM_SCALE);

        await _persistPLCService.AddPersistPLC(newPLC); 
        NWScript.DestroyObject(PLCWidget);

        NWScript.SetLocalString(NWScript.GetModule(),"staffMessage","Persist PLC Laid by " + NWScript.GetName(Player) + " in " + NWScript.GetName(Area) + ". Info --  " + " ResRef: " + 
        NWScript.GetResRef(PLC) + " , Name: " +  NWScript.GetName(PLC) + " , X: " +  NWScript.FloatToString(vectorLocation.X) + " , Y: " + NWScript.FloatToString(vectorLocation.Y) + " , Z: "  +  NWScript.FloatToString(vectorLocation.Z) + " , Facing: "  + NWScript.FloatToString(NWScript.GetFacing(PLC)) + " , Bio: " +  NWScript.GetDescription(PLC));
        NWScript.ExecuteScript("webhook_staff");
    }

    [ScriptHandler("js_persist_del")]
    public async void JobSystemPLCPersisDelete(CallInfo info)
    {
        uint PLC = NWScript.OBJECT_SELF;

        if ((NWScript.GetLocalInt(PLC, "persist") == 0))
        {
          return;
        }

        Location location = NWScript.GetLocation(PLC);
        Vector3 vectorLocation = NWScript.GetPositionFromLocation(location); 
        uint Area =  NWScript.GetArea(PLC); 
        uint Killer = NWScript.GetLastKiller(); 

       Predicate<PersistPLC> searcharea = (PersistPLC p) => {return p.AreaResRef == NWScript.GetResRef(Area);};
       Predicate<PersistPLC> searchx = (PersistPLC p) => {return p.X == vectorLocation.X;};
       Predicate<PersistPLC> searchxy = (PersistPLC p) => {return p.Y == vectorLocation.Y;};
       Predicate<PersistPLC> searchxyz = (PersistPLC p) => {return p.Z == vectorLocation.Z;};

       List<PersistPLC> persistPLC = await _persistPLCService.GetAllPersistPLCRecords();
       List<PersistPLC> persistPLCArea = persistPLC.FindAll(searcharea);
       List<PersistPLC> persistPLCAreax = persistPLCArea.FindAll(searchx);
       List<PersistPLC> persistPLCAreaxy = persistPLCAreax.FindAll(searchxy);
       PersistPLC persistPLCAreaxyz = persistPLCAreax.Find(searchxyz);

       if((persistPLCAreaxyz.PLCResRef == NWScript.GetResRef(PLC)) && (persistPLCAreaxyz.PLCName == NWScript.GetName(PLC)) && (persistPLCAreaxyz.Orientation == NWScript.GetFacing(PLC)))  
       {
        await _persistPLCService.DeletePersistPLC(persistPLCAreaxyz);
        
        NWScript.SetLocalString(NWScript.GetModule(),"staffMessage","Persist PLC Destroyed by " + NWScript.GetName(Killer) + " in " + NWScript.GetName(Area) + ". Info --  " + " ResRef: " + 
        NWScript.GetResRef(PLC) + " , Name: " +  NWScript.GetName(PLC) + " , X: " +  NWScript.FloatToString(vectorLocation.X) + " , Y: " + NWScript.FloatToString(vectorLocation.Y) + " , Z: "  +  NWScript.FloatToString(vectorLocation.Z) + " , Facing: "  + NWScript.FloatToString(NWScript.GetFacing(PLC)) + " , Bio: " +  NWScript.GetDescription(PLC));
        NWScript.ExecuteScript("webhook_staff");
       }
       else
       {
        NWScript.SendMessageToAllDMs("Error: Record not found to delete"); 
       }

    }


}