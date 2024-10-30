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
    private List<uint> _waypointOverflowMasterList = new(); 

    public void InvasionGeneric(uint waypoint, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5, string lieutentant, string boss,
        string invasionName, string overflow)
    {
        
        GenerateSpawnWaypointList(waypoint); 

        int totalMobClusters = Convert.ToInt32(_waypointMasterList.Count()*0.75); 
        int totalLieutentants = _waypointMasterList.Count()-totalMobClusters-1;
        uint area = NWScript.GetArea(waypoint);   
        string message = "News quickly spreads of an amassing army of " + invasionName + " in " + NWScript.GetName(area) +
                         ". They must be stopped before it is too late!";

        NWScript.SendMessageToAllDMs("Total Count: " + _waypointMasterList.Count().ToString() + " | TotalMobClusters: " + totalMobClusters.ToString() + " | TotalLieutents: " + totalLieutentants.ToString());

        SpawnMobs(area, totalMobClusters, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
                creaturetype5);
        SpawnLieutenants(area, totalLieutentants, lieutentant);
        SpawnBoss(area, boss);
        MassMessage(message,invasionName,NWScript.GetName(area));

        Random random = new Random();

        // Overflow Invasions
        if(random.Next(1,12) <= 12)
        {
         uint overflowWayPoint = NWScript.GetWaypointByTag(overflow);
         uint areaOverflow = NWScript.GetArea(overflowWayPoint);   
         string messageOverflow = "A surprise raid from the " + invasionName + " has snuck into " + NWScript.GetName(areaOverflow) +
                         ". They are spreading and must be stopped!";
         GenerateSpawnWaypointListOverflow(overflowWayPoint); 
         InvasionOverflow(areaOverflow, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
                creaturetype5, lieutentant);
         MassMessage(messageOverflow,invasionName,NWScript.GetName(areaOverflow));          
        }    

    }

    public void InvasionBeasts(uint waypoint)
    {
        uint Area = NWScript.GetArea(waypoint);
        string areaName = NWScript.GetName(Area);
        string creaturetype1 = "beasthero";
        string creaturetype2 = "elitebeastarcher";
        string creaturetype3 = "beastshaman";
        string creaturetype4 = "beastmanchampion";
        string creaturetype5 = "beastmonk";
        string lieutentant = "beastguard";
        string boss = "invasionbeastbs";
        string message = "News quickly spreads of an amassing army of Beastmen in " + areaName +
                         ". They must be stopped before it is too late!";
        string overflow = NWScript.GetLocalString(waypoint,"overflow");

        InvasionGeneric(waypoint, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message, overflow);
    }
    public void InvasionTrolls(uint waypoint)
    {
        uint Area = NWScript.GetArea(waypoint);
        string areaName = NWScript.GetName(Area);
        string creaturetype1 = "mountainguard";
        string creaturetype2 = "mountainguard";
        string creaturetype3 = "mounttroll";
        string creaturetype4 = "mounttroll";
        string creaturetype5 = "mounttroll";
        string lieutentant = "bigmounttroll";
        string boss = "invasiontrollbs";
        string message = "News quickly spreads of an amassing army of Trolls in " + areaName +
                         ". They must be stopped before it is too late!";
        string overflow = NWScript.GetLocalString(waypoint,"overflow");

        InvasionGeneric(waypoint, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message, overflow);
    }


    public void InvasionOrcs(uint waypoint)
    {
        uint Area = NWScript.GetArea(waypoint);
        string areaName = NWScript.GetName(Area);
        string creaturetype1 = "af_ds_ork";
        string creaturetype2 = "arelithorc001";
        string creaturetype3 = "arelithorc";
        string creaturetype4 = "arelithorc";
        string creaturetype5 = "orcbasher";
        string lieutentant = "orcboss001";
        string boss = "chosenofkilma002";
        string message = "News quickly spreads of an amassing army of Orcs in " + areaName +
                         ". They must be stopped before it is too late!";
        string overflow = NWScript.GetLocalString(waypoint,"overflow");

        InvasionGeneric(waypoint, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message, overflow);
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
    public void GenerateSpawnWaypointListOverflow(uint waypoint)
    {
        List<uint> waypointOverflowMasterList = new List<uint>();
        string waypointTag = NWScript.GetTag(waypoint);
        int count = 1; 
        uint spawnWaypoint = NWScript.GetWaypointByTag(waypointTag+"s"+count.ToString());
        while(NWScript.GetIsObjectValid(spawnWaypoint)==1)
        {
            waypointOverflowMasterList.Add(spawnWaypoint);
            count++; 
            spawnWaypoint = NWScript.GetWaypointByTag(waypointTag+"s"+count.ToString()); 
        }
        _waypointOverflowMasterList = waypointOverflowMasterList; 
    }

    public IntPtr GrabSpawnLocationInArea()
    {  
        int size = _waypointMasterList.Count(); 
        Random random = new Random();
        int ran = random.Next(size);
        uint waypoint = _waypointMasterList[ran];
        _waypointMasterList.Remove(waypoint); 
        IntPtr location = NWScript.GetLocation(waypoint);
        return location; 
    }
    public IntPtr GrabSpawnLocationInOverflowArea()
    {  
        int size = _waypointOverflowMasterList.Count(); 
        Random random = new Random();
        int ran = random.Next(size);
        uint waypoint = _waypointOverflowMasterList[ran];
        _waypointOverflowMasterList.Remove(waypoint); 
        IntPtr location = NWScript.GetLocation(waypoint);
        NWScript.SendMessageToAllDMs("Overflow Grab Launched: " + ran.ToString());
        return location; 
    }
    
    public void InvasionOverflow(uint area, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5, string lieutentant)
    {

       int countMobs = 0;
        // const float zPosition = 0.0f;
        const float facing = 0.0f;
        int runMax = _waypointOverflowMasterList.Count();
        
        while (countMobs < runMax)
        {
            NWScript.SendMessageToAllDMs("Invasion Overflow ran ");
            IntPtr randomLocation = GrabSpawnLocationInOverflowArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation); 
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y; 
            float zPosition = ranLocPositon.Z;  

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, lieutentant, randomLocation);

            Vector3 randomPosN = NWScript.Vector(xPosition, yPosition + 1.0f,zPosition);
            Vector3 randomPosS = NWScript.Vector(xPosition, yPosition - 1.0f,zPosition);
            Vector3 randomPosE = NWScript.Vector(xPosition + 1.0f, yPosition,zPosition);
            Vector3 randomPosW = NWScript.Vector(xPosition - 1.0f, yPosition,zPosition);
            Vector3 randomPosNe = NWScript.Vector(xPosition + 1.0f, yPosition + 1.0f,zPosition);
            Vector3 randomPosNw = NWScript.Vector(xPosition - 1.0f, yPosition + 1.0f,zPosition);
            Vector3 randomPosSe = NWScript.Vector(xPosition + 1.0f, yPosition - 1.0f,zPosition);
            Vector3 randomPosSw = NWScript.Vector(xPosition - 1.0f, yPosition - 1.0f,zPosition);

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1,
                NWScript.Location(area, randomPosN, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                NWScript.Location(area, randomPosS, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                NWScript.Location(area, randomPosE, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                NWScript.Location(area, randomPosW, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype5,
                NWScript.Location(area, randomPosNe, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1,
                NWScript.Location(area, randomPosNw, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                NWScript.Location(area, randomPosSe, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype5,
                NWScript.Location(area, randomPosSw, facing));
            countMobs++;

            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f,zPosition);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f,zPosition);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f,zPosition);

            CreatePlc(NWScript.Location(area, randomPLCPosNw, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSe, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSw, facing));
        }


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
            float zPosition = ranLocPositon.Z;  

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1, randomLocation);

            Vector3 randomPosN = NWScript.Vector(xPosition, yPosition + 1.0f,zPosition);
            Vector3 randomPosS = NWScript.Vector(xPosition, yPosition - 1.0f,zPosition);
            Vector3 randomPosE = NWScript.Vector(xPosition + 1.0f, yPosition,zPosition);
            Vector3 randomPosW = NWScript.Vector(xPosition - 1.0f, yPosition,zPosition);
            Vector3 randomPosNe = NWScript.Vector(xPosition + 1.0f, yPosition + 1.0f,zPosition);
            Vector3 randomPosNw = NWScript.Vector(xPosition - 1.0f, yPosition + 1.0f,zPosition);
            Vector3 randomPosSe = NWScript.Vector(xPosition + 1.0f, yPosition - 1.0f,zPosition);
            Vector3 randomPosSw = NWScript.Vector(xPosition - 1.0f, yPosition - 1.0f,zPosition);

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1,
                NWScript.Location(area, randomPosN, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                NWScript.Location(area, randomPosS, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                NWScript.Location(area, randomPosE, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                NWScript.Location(area, randomPosW, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype5,
                NWScript.Location(area, randomPosNe, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                NWScript.Location(area, randomPosNw, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                NWScript.Location(area, randomPosSe, facing));
            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                NWScript.Location(area, randomPosSw, facing));
            countMobs++;

            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f,zPosition);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f,zPosition);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f,zPosition);

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
            NWScript.SendMessageToAllDMs("Lie: " + countLieutenant.ToString());
            IntPtr randomLocation = GrabSpawnLocationInArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation); 
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y; 
            float zPosition = ranLocPositon.Z;  
            uint objectCreature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, lieutentant, randomLocation);
            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f,zPosition);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f,zPosition);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f,zPosition);
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

    private static void MassMessage(string message, string CreatureName, string AreaName)
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

        NWScript.SetLocalString(NWScript.GetModule(), "announcerMessage",
        "``` All adventurers begin to hear murmurs and rumors from locals about a terrifying attack happening on the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! " +
        CreatureName + " are rampaging in " + AreaName +
        "! We recommend an appropriately skilled group of adventurers respond and common folk stay clear! ```");
        NWScript.ExecuteScript("webhook_announce");

        
    }
}