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
        uint PLC = NWScript.GetLocalObject(Player,"pcplc");
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

        await _persistPLCService.AddPersistPLC(newPLC); 
        //NWScript.DestroyObject(PLC);
        NWScript.SendMessageToAllDMs(NWScript.GetName(PLC));
        NWScript.SendMessageToAllDMs(NWScript.GetResRef(PLC));

    }

    [ScriptHandler("js_persist_del")]
    public async void JobSystemPLCPersisDelete(CallInfo info)
    {
        uint PLC = NWScript.OBJECT_SELF;
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

        await _persistPLCService.DeletePersistPLC(newPLC);

    }




}