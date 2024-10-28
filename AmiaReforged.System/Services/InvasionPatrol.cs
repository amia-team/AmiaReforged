using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using NWN.Core;
using System.Numerics;
using AmiaReforged.System;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Models;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using NWN.Core.NWNX;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(InvasionPatrol))]
public class InvasionPatrol
{
    private readonly InvasionService _invasionService;
    private readonly Invasions _invasions;
    
    public InvasionPatrol(InvasionService invasionService, Invasions invasions)
    {
       _invasionService = invasionService; 
       _invasions = invasions;
    }
    
    [ScriptHandler("invasion_patrol")]
    public async void InvasionPatrolCheck(CallInfo info)
    {
      uint oPC = NWScript.OBJECT_SELF;
      Location Location = NWScript.GetLocation(oPC);
      uint Area = NWScript.GetAreaFromLocation(Location);
      string AreaResRef = NWScript.GetResRef(Area);
      InvasionRecord invasionRecord = new();
      invasionRecord.AreaZone = "N/A";
      invasionRecord.InvasionPercent = 0; 
      invasionRecord.RealmChaos = 0; 
      List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
      InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef); 
      int PatrolValue = 10; 
      uint JobJournal = NWScript.GetItemPossessedBy(oPC,"jobjournal");
      if(NWScript.GetIsObjectValid(JobJournal)==1)
      {
        string PrimaryJob     = NWScript.GetLocalString(JobJournal,"primaryjob");
        string SecondaryJob   = NWScript.GetLocalString(JobJournal,"secondaryjob");
        if(PrimaryJob == "Scoundrel")
        {
          PatrolValue += 10;
          NWScript.SendMessageToPC(oPC,"*Your soldier job makes you exceptional at patrols*");
        }
        else if(SecondaryJob == "Scoundrel")
        {
          PatrolValue += 5;
          NWScript.SendMessageToPC(oPC,"*Your soldier job makes you good at patrols*"); 
        }
      }

      if(invasionRecordTemp != null)
      {
        invasionRecord = invasionRecordTemp;
        if(NWScript.GetDistanceBetweenLocations(NWScript.GetLocalLocation(oPC,AreaResRef),Location) >= 10.0)
        {
          NWScript.SetLocalLocation(oPC,AreaResRef,Location);
          int temp = invasionRecord.InvasionPercent - PatrolValue; 
          if(temp < 0)
          {
            temp = 0;
          }
          invasionRecord.InvasionPercent = temp; 

          if(temp > 80)
          {
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is crawling with enemies and escalation is immediate!"); 
           SpawnEnemies(oPC,4);
          }
          else if(temp > 60)
          {
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is crawling with enemies and will escalate any day now!");
           SpawnEnemies(oPC,3);
          }
          else if(temp > 40)
          {  
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is crawling with enemies and will escalate if left alone too long!");
           SpawnEnemies(oPC,2);
          }
          else if(temp > 20)
          {    
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " has many enemies sneaking around!");  
           SpawnEnemies(oPC,1);
          }
          else if(temp > 10)
          {  
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " has a few enemies sneaking around!");
          }
          else if(temp == 0)
          {   
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is rather peaceful!");
          }
          
        }
        else
        {
         NWScript.SendMessageToPC(oPC,"*You must patrol in a different location*");
        }
      }
      else
      {
        NWScript.SendMessageToPC(oPC,"*No need to patrol this area*");
      }
    }

    public void SpawnEnemies(uint oPC, int count)
    {
        uint waypoint = NWScript.GetWaypointByTag("Invasion" + count.ToString());
        uint waypointSpawn; 
        uint Area = NWScript.GetArea(oPC);
        Random random = new Random();
        int ran;
        string spawn = ""; 
        Vector3 ranLocPositon = NWScript.GetPositionFromLocation(NWScript.GetLocation(Area)); 
        float xPosition = ranLocPositon.X;
        float yPosition = ranLocPositon.Y; 
        float zPosition = ranLocPositon.Z;  

        while(NWScript.GetIsObjectValid(waypoint) == 1)
        {
          if(NWScript.GetArea(waypoint) == NWScript.GetArea(oPC))
          {
            waypointSpawn = waypoint;
            break;
          }
          count++;
          waypoint = NWScript.GetWaypointByTag("Invasion" + count.ToString());  
        }

        int i; 
        for(i=0;i<count;i++)
        {
          ran = random.Next(0, 5);
          switch(ran)
          {
            case 1: spawn = NWScript.GetLocalString(oPC,"creaturetype1"); break;  
            case 2: spawn = NWScript.GetLocalString(oPC,"creaturetype2"); break;  
            case 3: spawn = NWScript.GetLocalString(oPC,"creaturetype3"); break;  
            case 4: spawn = NWScript.GetLocalString(oPC,"creaturetype4"); break;  
            case 5: spawn = NWScript.GetLocalString(oPC,"creaturetype5"); break;  
          }

        NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE,spawn,NWScript.Location(Area,NWScript.Vector(xPosition + 0.5f, yPosition + 0.5f,zPosition),0.0f));
        }
    }
}