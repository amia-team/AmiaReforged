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
    private List<string> serverAreaResref; 


    public PersistPLCSpawner(SchedulerService schedulerService,PersistPLCService persistPLCService)
    {
       _schedulerService = schedulerService;
       _persistPLCService = persistPLCService; 
       serverAreas = new List<uint>();
       serverAreaResref = new List<string>();
       _schedulerService.ScheduleRepeating(Run, TimeSpan.FromMinutes(1));
    }

    public void GatherAreas()
    {
        int count = 0;
        uint tempWP = NWScript.GetObjectByTag("is_area",count);

        while(NWScript.GetIsObjectValid(tempWP)==1)
        {
          uint tempArea = NWScript.GetArea(tempWP);
          serverAreas.Add(tempArea);
          serverAreaResref.Add(NWScript.GetResRef(tempArea));
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

        List<PersistPLC> persistPLC = await _persistPLCService.GetAllPersistPLCRecords();
        int count = persistPLC.Count; 

        int i;
        for(i=0;i<count;i++)
        {
            PersistPLC temp = persistPLC[i];
            string tempPLCName = temp.PLCName;
            string areaResRef = temp.AreaResRef;
            int realResRefIndex = serverAreaResref.FindIndex(x => x.Contains(areaResRef)); 
            if (realResRefIndex == -1)
            {
                continue;
            }
            uint realArea = serverAreas[realResRefIndex];
            string PLCResRef = temp.PLCResRef;
            Vector3 vector = NWScript.Vector(temp.X,temp.Y,temp.Z);
            Location location = NWScript.Location(realArea, vector,temp.Orientation);
            uint tempObject = NWScript.CreateObject(64,PLCResRef,location);           
            NWScript.SetLocalInt(tempObject,"persist",1);
            NWScript.SetName(tempObject,tempPLCName);
            NWScript.SetDescription(tempObject,temp.PLCDescription);
            NWScript.SetUseableFlag(tempObject,1);
            NWScript.SetPlotFlag(tempObject,0);
            NWScript.SetObjectVisualTransform(tempObject,NWScript.OBJECT_VISUAL_TRANSFORM_SCALE,temp.Size);     
        }

       

        NWScript.SetLocalInt(NWScript.GetModule(), "PersistPLCLaunched",1); 
    }

}