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
using System.Runtime.InteropServices;

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
      NwObject AnvilPC = info.ObjectSelf;
      Location Location = NWScript.GetLocation(oPC);
      uint Area = NWScript.GetAreaFromLocation(Location);
      string AreaResRef = NWScript.GetResRef(Area);
      Location StoredLocation = NWScript.GetLocalLocation(oPC,AreaResRef); 
      uint StoredArea = NWScript.GetAreaFromLocation(StoredLocation);
      InvasionRecord invasionRecord = new();
      invasionRecord.AreaZone = "N/A";
      invasionRecord.InvasionPercent = 0; 
      invasionRecord.RealmChaos = 0; 
      List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
      InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef); 
      int PatrolValue = 5; 
      int rewardCount = 1;
      uint JobJournal = NWScript.GetItemPossessedBy(oPC,"js_jobjournal");
      if(NWScript.GetIsObjectValid(JobJournal)==1)
      {
        NWScript.SendMessageToPC(oPC,"Journal found, script fired"); 
        string PrimaryJob     = NWScript.GetLocalString(JobJournal,"primaryjob");
        string SecondaryJob   = NWScript.GetLocalString(JobJournal,"secondaryjob");
        if(PrimaryJob == "Soldier")
        {
          PatrolValue += 10;
          rewardCount = 3;
          NWScript.SendMessageToPC(oPC,"*Your soldier job makes you exceptional at patrols*");
        }
        else if(SecondaryJob == "Soldier")
        {
          PatrolValue += 5;
          rewardCount = 1; 
          NWScript.SendMessageToPC(oPC,"*Your soldier job makes you good at patrols*"); 
        }
      }
      else
      {
          NWScript.SendMessageToPC(oPC,"*You are moderately good at patrols*"); 
      }

      if(invasionRecordTemp != null)
      {
        invasionRecord = invasionRecordTemp;
        if((NWScript.GetDistanceBetweenLocations(StoredLocation,Location) >= 20.0) || (NWScript.GetIsObjectValid(StoredArea) != 1))
        {
          NWScript.SetLocalLocation(oPC,AreaResRef,Location);
          int temp = invasionRecord.InvasionPercent - PatrolValue; 
          if(temp < 0)
          {
            temp = 0;
          }

          if(invasionRecord.InvasionPercent > 80)
          {
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is crawling with enemies and escalation is immediate!"); 
           SpawnEnemies(oPC,4);
           Reward(oPC,AnvilPC,rewardCount);
          }
          else if(invasionRecord.InvasionPercent > 60)
          {
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is crawling with enemies and will escalate any day now!");
           SpawnEnemies(oPC,3);
           Reward(oPC,AnvilPC,rewardCount);
          }
          else if(invasionRecord.InvasionPercent > 40)
          {  
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is crawling with enemies and will escalate if left alone too long!");
           SpawnEnemies(oPC,2);
           Reward(oPC,AnvilPC,rewardCount);
          }
          else if(invasionRecord.InvasionPercent > 20)
          {    
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " has many enemies sneaking around!");  
           SpawnEnemies(oPC,1);
           Reward(oPC,AnvilPC,rewardCount);
          }
          else if(invasionRecord.InvasionPercent > 10)
          {  
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " has a few enemies sneaking around!");
           Reward(oPC,AnvilPC,rewardCount);
          }
          else if(invasionRecord.InvasionPercent > 0)
          {   
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is relatively peaceful!");
           Reward(oPC,AnvilPC,rewardCount);
          }
          else
          {
           NWScript.SendMessageToPC(oPC,NWScript.GetName(Area) + " is completely peaceful!");  
          }
          
          invasionRecord.InvasionPercent = temp; 
          await _invasionService.UpdateInvasionArea(invasionRecord);
          
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
        int waypointcount = 1;
        uint waypoint = NWScript.GetWaypointByTag("Invasion" + waypointcount.ToString());
        uint waypointSpawn = new(); 
        uint Area = NWScript.GetArea(oPC);
        Random random = new Random();
        int ran;
        string spawn = ""; 
        Vector3 ranLocPositon = NWScript.GetPositionFromLocation(NWScript.GetLocation(oPC)); 
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
          waypointcount++;
          waypoint = NWScript.GetWaypointByTag("Invasion" + waypointcount.ToString());  
        }

        int i; 
        for(i=0;i<count;i++)
        {
          ran = random.Next(1, 5);
          switch(ran)
          {
            case 1: spawn = NWScript.GetLocalString(waypointSpawn,"creaturetype1"); break;  
            case 2: spawn = NWScript.GetLocalString(waypointSpawn,"creaturetype2"); break;  
            case 3: spawn = NWScript.GetLocalString(waypointSpawn,"creaturetype3"); break;  
            case 4: spawn = NWScript.GetLocalString(waypointSpawn,"creaturetype4"); break;  
            case 5: spawn = NWScript.GetLocalString(waypointSpawn,"creaturetype5"); break;  
          }

        NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE,spawn,NWScript.Location(Area,NWScript.Vector(xPosition + 0.5f, yPosition + 0.5f,zPosition),0.0f));
        }
        
    }

    public void Reward(uint oPC,NwObject AnvilPC, int rewardCount)
    {
        int XP = NWScript.GetXP(oPC);
        Random random = new Random(); 
        int Level = NWScript.GetLevelByPosition(1,oPC) + NWScript.GetLevelByPosition(2,oPC) + NWScript.GetLevelByPosition(3,oPC);
        
        NWScript.SendMessageToPC(oPC,"*You find something of interest on your patrol*");

        if(Level < 30)
        {
          NWScript.SetXP(oPC, XP+25);
        }
        else
        {
          NWScript.SetXP(oPC, XP+1);
        }

        int i;
        for(i=0;i<rewardCount;i++)
        {

         if(random.Next(1,10) <= 3)
         {
          switch(random.Next(7))
          { 
            case 0: Anvil.API.NwItem.Create("js_hun_mbone", (NwGameObject?)AnvilPC); break;
            case 1: Anvil.API.NwItem.Create("js_farm_appl", (NwGameObject?)AnvilPC); break;
            case 2: Anvil.API.NwItem.Create("js_farm_pota", (NwGameObject?)AnvilPC); break;
            case 3: Anvil.API.NwItem.Create("js_farm_oats", (NwGameObject?)AnvilPC); break;
            case 4: Anvil.API.NwItem.Create("js_farm_toba", (NwGameObject?)AnvilPC); break;
            case 5: Anvil.API.NwItem.Create("js_hun_sbone", (NwGameObject?)AnvilPC); break;
            case 6: Anvil.API.NwItem.Create("js_hun_lbone", (NwGameObject?)AnvilPC); break;
          }
         }
         else
         {
          
          switch(random.Next(2))
          {
            case 0: Anvil.API.NwItem.Create("js_sold_fang", (NwGameObject?)AnvilPC); break;
            case 1: Anvil.API.NwItem.Create("js_sold_claw", (NwGameObject?)AnvilPC); break;
          }
         }
        }

    }
}