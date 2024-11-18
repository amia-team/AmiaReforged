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

[ServiceBinding(typeof(JobSystemPLCPersis))]
public class JobSystemPLCPersis
{
    private readonly PersistPLCService _persistPLCService;
    private readonly PersistPLC _persistplc;


    public JobSystemPLCPersis(PersistPLCService persistPLCService, PersistPLC persistplc)
    {
        _persistPLCService = persistPLCService;
        _persistplc = persistplc;
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

        newPLC.Area = NWScript.GetArea(PLC); 
        newPLC.PLC = PLC; 
        newPLC.X = vectorLocation.X;
        newPLC.Y = vectorLocation.Y; 
        newPLC.Z = vectorLocation.Z; 
        newPLC.Orientation = facing;

        await _persistPLCService.AddPersistPLC(newPLC); 

    }

    [ScriptHandler("js_persist_del")]
    public async void JobSystemPLCPersisDelete(CallInfo info)
    {
        uint PLC = NWScript.OBJECT_SELF;
        PersistPLC newPLC = new PersistPLC(); 
        Location location = NWScript.GetLocation(PLC);
        Vector3 vectorLocation = NWScript.GetPositionFromLocation(location); 
        float facing = NWScript.GetFacing(PLC);

        newPLC.Area = NWScript.GetArea(PLC); 
        newPLC.PLC = PLC; 
        newPLC.X = vectorLocation.X;
        newPLC.Y = vectorLocation.Y; 
        newPLC.Z = vectorLocation.Z; 
        newPLC.Orientation = facing;

        await _persistPLCService.DeletePersistPLC(newPLC);

    }




}