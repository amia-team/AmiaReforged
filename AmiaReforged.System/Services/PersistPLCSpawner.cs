using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using System.Numerics;
using NWN.Core;
using AmiaReforged.System;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;

namespace AmiaReforged.System.Services;


[ServiceBinding(typeof(PersistPLCSpawner))]
public class PersistPLCSpawner
{
    private readonly SchedulerService _schedulerService;
    private readonly PersistPLCService _persistPLCService;
    private readonly PersistPLC _persistplc;


    public PersistPLCSpawner(SchedulerService schedulerService,PersistPLCService persistPLCService, PersistPLC persistplc)
    {
       _schedulerService = schedulerService;
       _schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
       _persistPLCService = persistPLCService; 
       _persistplc = persistplc;
    }

    public async void Run()
    {
        if ((NWScript.GetLocalInt(NWScript.GetModule(), "PersistPLCLaunched") == 1))
        {
          return;
        }
        
        List<PersistPLC> invasions = await _persistPLCService.GetAllPersistPLCRecords();
        int count = invasions.Count; 

        int i;

        for(i=0;i<count;i++)
        {
            PersistPLC temp = invasions[i];
            uint tempPLC = temp.PLC;
            string resRef = NWScript.GetResRef(tempPLC);
            Vector3 vector = NWScript.Vector(temp.X,temp.Y,temp.Z);
            Location location = NWScript.Location(temp.Area, vector,temp.Orientation);
            uint tempObject = NWScript.CreateObject(64,resRef,location);
            NWScript.SetName(tempObject,NWScript.GetName(tempPLC));
            NWScript.SetDescription(tempObject,NWScript.GetDescription(tempPLC));
            NWScript.SetUseableFlag(tempObject,1);
            NWScript.SetPlotFlag(tempObject,0);
        }

        NWScript.SetLocalInt(NWScript.GetModule(), "PersistPLCLaunched",1); 
    }

}