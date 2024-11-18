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
    private List<uint> serverAreas; 
    private List<string> serverAreaNames; 


    public PersistPLCSpawner(SchedulerService schedulerService,PersistPLCService persistPLCService)
    {
       _schedulerService = schedulerService;
       _schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
       _persistPLCService = persistPLCService; 
    }

    public async void GatherAreas()
    {
        int count = 0;
        uint tempWP = NWScript.GetObjectByTag("is_area",count);

        while(NWScript.GetIsObjectValid(tempWP)==1)
        {
          serverAreas.Add(NWScript.GetArea(tempWP));
          serverAreaNames.Add(NWScript.GetName(tempWP));
          count++;
          tempWP = NWScript.GetObjectByTag("is_area",count); 
        }

    }

    public async void Run()
    {
        if ((NWScript.GetLocalInt(NWScript.GetModule(), "PersistPLCLaunched") == 1))
        {
          return;
        }

        GatherAreas(); 
        
        List<PersistPLC> invasions = await _persistPLCService.GetAllPersistPLCRecords();
        int count = invasions.Count; 

        int i;

        for(i=0;i<count;i++)
        {
            PersistPLC temp = invasions[i];
            uint tempPLC = temp.PLC;
            string areaName = NWScript.GetName(temp.Area);
            string realName = serverAreaNames.Find(x => x.Contains(areaName)); 
            uint realArea = serverAreas[Int32.Parse(realName)];
            string resRef = NWScript.GetResRef(tempPLC);
            Vector3 vector = NWScript.Vector(temp.X,temp.Y,temp.Z);
            Location location = NWScript.Location(realArea, vector,temp.Orientation);
            uint tempObject = NWScript.CreateObject(64,resRef,location);
            NWScript.SetName(tempObject,areaName);
            NWScript.SetDescription(tempObject,NWScript.GetDescription(tempPLC));
            NWScript.SetUseableFlag(tempObject,1);
            NWScript.SetPlotFlag(tempObject,0);
        }

        NWScript.SetLocalInt(NWScript.GetModule(), "PersistPLCLaunched",1); 
    }

}