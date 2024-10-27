using System;
using System.Data.Common;
using Anvil.API;
using Anvil.Services;
using System.Numerics;
using NWN.Core;
using AmiaReforged.System;
using AmiaReforged.Core.Models;

namespace AmiaReforged.System.Services;


[ServiceBinding(typeof(Invasions))]
public class Invasions
{

    private List<uint> _waypointMasterList = new(); 

    public void InvasionGeneric(uint waypoint, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5, string lieutentant, string boss,
        string message)
    {
        
        GenerateSpawnWaypointList(waypoint); 

        int totalMobClusters = Convert.ToInt32(_waypointMasterList.Count()*0.75); 
        int totalLieutentants = _waypointMasterList.Count()-totalMobClusters;
        uint area = NWScript.GetArea(waypoint);

        //await Task.Delay(TimeSpan.FromSeconds(15));
        SpawnMobs(area, totalMobClusters, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
                creaturetype5);
        //await Task.Delay(TimeSpan.FromSeconds(15));
        SpawnLieutenants(area, totalLieutentants, lieutentant);
        //await Task.Delay(TimeSpan.FromSeconds(15));
        SpawnBoss(area, boss);
        //await Task.Delay(TimeSpan.FromSeconds(15));
        MassMessage(message);

    }

    public void InvasionBeasts(uint area)
    {
        string areaName = NWScript.GetName(area);
        string creaturetype1 = "beasthero";
        string creaturetype2 = "elitebeastarcher";
        string creaturetype3 = "beastshaman";
        string creaturetype4 = "beastmanchampion";
        string creaturetype5 = "beastmonk";
        string lieutentant = "beastguard";
        string boss = "invasionbeastbs";
        string message = "News quickly spreads of an amassing army of Beastmen in " + areaName +
                         ". They must be stopped before it is too late!";

        InvasionGeneric(area, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }


    public void InvasionGoblins(uint area)
    {
        string areaName = NWScript.GetName(area);
        string creaturetype1 = "ds_yellowfang_5";
        string creaturetype2 = "ds_yellowfang_1";
        string creaturetype3 = "ds_yellowfang_2";
        string creaturetype4 = "ds_yellowfang_1";
        string creaturetype5 = "ds_yellowfang_2";
        string lieutentant = "ds_yellowfang_4";
        string boss = "ds_yellowfang_6";
        string message = "News quickly spreads of an amassing army of Goblins in " + areaName +
                         ". They must be stopped before it is too late!";

        InvasionGeneric(area, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }


    public void InvasionTrolls(uint area)
    {
        string areaName = NWScript.GetName(area);
        string creaturetype1 = "mountainguard";
        string creaturetype2 = "mountainguard";
        string creaturetype3 = "mounttroll";
        string creaturetype4 = "mounttroll";
        string creaturetype5 = "mounttroll";
        string lieutentant = "bigmounttroll";
        string boss = "invasiontrollbs";
        string message = "News quickly spreads of an amassing army of Trolls in " + areaName +
                         ". They must be stopped before it is too late!";

        InvasionGeneric(area, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }


    public void InvasionOrcs(uint area)
    {
        string areaName = NWScript.GetName(area);
        string creaturetype1 = "af_ds_ork";
        string creaturetype2 = "arelithorc001";
        string creaturetype3 = "arelithorc";
        string creaturetype4 = "arelithorc";
        string creaturetype5 = "orcbasher";
        string lieutentant = "orcboss001";
        string boss = "chosenofkilma002";
        string message = "News quickly spreads of an amassing army of Orcs in " + areaName +
                         ". They must be stopped before it is too late!";

        InvasionGeneric(area, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }

    public void GenerateSpawnWaypointList(uint waypoint)
    {
        List<uint> waypointMasterList = new List<uint>();
        string waypointTag = NWScript.GetTag(waypoint);
        int count = 1; 
        uint spawnWaypoint = NWScript.GetWaypointByTag(waypointTag+"s"+count.ToString());
        while(NWScript.GetIsObjectValid(spawnWaypoint)==1)
        {
            waypointMasterList.Add(spawnWaypoint);
            count++; 
            spawnWaypoint = NWScript.GetWaypointByTag(waypointTag+"s"+count.ToString()); 
        }
        _waypointMasterList = waypointMasterList; 
    }

    public IntPtr GrabSpawnLocationInArea()
    {  
        int size = _waypointMasterList.Count(); 
        Random random = new Random();
        int ran = random.Next(0,size);
        uint waypoint = _waypointMasterList[ran];
        _waypointMasterList.Remove(waypoint); 
        IntPtr location = NWScript.GetLocation(waypoint);
        return location; 
    }

    private static uint CreatePlc(IntPtr objectLocation)
    {
        Random rnd = new Random();
        int random = rnd.Next(11);
        uint plc = 0;

        switch (random)
        {
            case 0:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasioncorpse1", objectLocation);
                break;
            case 1:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasionmisc3", objectLocation);
                break;
            case 2:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasioncorpse3", objectLocation);
                break;
            case 3:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasioncorpse5", objectLocation);
                break;
            case 4:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasionmisc5", objectLocation);
                break;
            case 5:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasiondebris1", objectLocation);
                break;
            case 6:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasioncamp", objectLocation);
                break;
            case 7:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasiondebris1", objectLocation);
                break;
            case 8:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasioncamp", objectLocation);
                break;
            case 9:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasionmisc3", objectLocation);
                break;
            case 10:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "invasionmisc3", objectLocation);
                break;
        }

        return plc;
    }

    private void SpawnMobs(uint area, int totalMobClusters, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5)
    {
        int countMobs = 0;
        // const float zPosition = 0.0f;
        const float facing = 0.0f;
        
        while (countMobs < totalMobClusters)
        {
            IntPtr randomLocation = GrabSpawnLocationInArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation); 
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y; 

            uint creature1 = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1, randomLocation);

            if (NWScript.GetIsObjectValid(creature1) != 1) continue;

            Vector3 randomPosN = NWScript.Vector(xPosition, yPosition + 1.0f);
            Vector3 randomPosS = NWScript.Vector(xPosition, yPosition - 1.0f);
            Vector3 randomPosE = NWScript.Vector(xPosition + 1.0f, yPosition);
            Vector3 randomPosW = NWScript.Vector(xPosition - 1.0f, yPosition);
            Vector3 randomPosNe = NWScript.Vector(xPosition + 1.0f, yPosition + 1.0f);
            Vector3 randomPosNw = NWScript.Vector(xPosition - 1.0f, yPosition + 1.0f);
            Vector3 randomPosSe = NWScript.Vector(xPosition + 1.0f, yPosition - 1.0f);
            Vector3 randomPosSw = NWScript.Vector(xPosition - 1.0f, yPosition - 1.0f);

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                NWScript.Location(area, randomPosN, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                NWScript.Location(area, randomPosS, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                NWScript.Location(area, randomPosE, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                NWScript.Location(area, randomPosW, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                NWScript.Location(area, randomPosNe, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                NWScript.Location(area, randomPosNw, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype5,
                NWScript.Location(area, randomPosSe, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype5,
                NWScript.Location(area, randomPosSw, facing));
            countMobs++;

            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f);

            CreatePlc(NWScript.Location(area, randomPLCPosNw, facing));
            
            CreatePlc(NWScript.Location(area, randomPLCPosSe, facing));
            
            CreatePlc(NWScript.Location(area, randomPLCPosSw, facing));
        }
        
    }

    private void SpawnLieutenants(uint area, int totalLieutentants, string lieutentant)
    {
        int countLieutenant = 0;
        const float facing = 0.0f;

        while (countLieutenant < totalLieutentants)
        {
            IntPtr randomLocation = GrabSpawnLocationInArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation); 
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y; 
            uint objectCreature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, lieutentant, randomLocation);
            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f);
            CreatePlc(NWScript.Location(area, randomPLCPosNw, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSe, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSw, facing));

            if (NWScript.GetIsObjectValid(objectCreature) == 1) countLieutenant++;
        }
    }

    private void SpawnBoss(uint area, string boss)
    {
        int countBoss = 0;

        while (countBoss < 1)
        {
            IntPtr randomLocation = GrabSpawnLocationInArea();
            uint objectCreature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, boss, randomLocation);

            if (NWScript.GetIsObjectValid(objectCreature) == 1) countBoss++;
        }
    }

    private static void MassMessage(string message)
    {
        uint objectCreature = NWScript.GetFirstPC();

        while (NWScript.GetIsObjectValid(objectCreature) == 1)
        {
            NWScript.SendMessageToPC(objectCreature, "-----");
            NWScript.SendMessageToPC(objectCreature, "-----");
            NWScript.SendMessageToPC(objectCreature, message);
            NWScript.SendMessageToPC(objectCreature, "-----");
            NWScript.SendMessageToPC(objectCreature, "-----");
            objectCreature = NWScript.GetNextPC();
        }

        
    }
}