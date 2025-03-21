using System.Numerics;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(Invasions))]
public class Invasions
{
    private List<uint> _waypointMasterList = new();
    private List<uint> _waypointOverflowMasterList = new();

    public void InvasionGeneric(uint waypoint, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5, string lieutenant, string boss,
        string invasionName, string overflow, int alternateMessage)
    {
        // Make sure the Waypoint lists are cleaned
        _waypointMasterList = new();
        _waypointOverflowMasterList = new();
        //

        GenerateSpawnWaypointList(waypoint);

        int totalMobClusters = Convert.ToInt32(_waypointMasterList.Count() * 0.75);
        int totalLieutenants = _waypointMasterList.Count() - totalMobClusters - 1;
        uint area = NWScript.GetArea(waypoint);
        string message;

        if (alternateMessage == 1)
            message = "News quickly spreads of a portal appearing in " + NWScript.GetName(area) +
                      ". Demon's are pouring out and must be stopped before it is too late! Only the most experienced adventurers should respond!";
        else
            message = "News quickly spreads of an amassing army of " + invasionName + " in " + NWScript.GetName(area) +
                      ". They must be stopped before it is too late!";

        NWScript.SendMessageToAllDMs("Total Count: " + _waypointMasterList.Count() + " | TotalMobClusters: " +
                                     totalMobClusters + " | TotalLieutents: " + totalLieutenants + " | Boss: 1");

        SpawnMobs(area, totalMobClusters, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5);
        SpawnLieutenants(area, totalLieutenants, lieutenant);
        SpawnBoss(area, boss);
        MassMessage(message, invasionName, NWScript.GetName(area), alternateMessage);

        Random random = new();

        // Overflow Invasions
        if (random.Next(12) <= 3 && overflow != "")
        {
            uint overflowWayPoint = NWScript.GetWaypointByTag(overflow);
            if (NWScript.GetIsObjectValid(overflowWayPoint) == 1)
            {
                uint areaOverflow = NWScript.GetArea(overflowWayPoint);
                string messageOverflow = "A surprise raid from the " + invasionName + " has snuck into " +
                                         NWScript.GetName(areaOverflow) +
                                         ". They are spreading and must be stopped!";
                GenerateSpawnWaypointListOverflow(overflowWayPoint);
                InvasionOverflow(areaOverflow, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
                    creaturetype5, lieutenant);
                MassMessage(messageOverflow, invasionName, NWScript.GetName(areaOverflow), 2);
            }
        }
    }

    public void AbyssalInvasion(uint waypoint)
    {
        uint Area = NWScript.GetArea(waypoint);
        string areaName = NWScript.GetName(Area);
        string creaturetype1 = "ab_uce_heaer";
        string creaturetype2 = "ab_uce_runner";
        string creaturetype3 = "ds_demon_4";
        string creaturetype4 = "ab_succubussneak";
        string creaturetype5 = "";
        string lieutentant = "balorlieutentant";
        string boss = "demoninvaboss";
        string overflow = "";

        InvasionGeneric(waypoint, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, invasionName: "Demonic Forces", overflow, 1);
    }

    public void GenerateSpawnWaypointList(uint waypoint)
    {
        List<uint> waypointMasterList = new();
        string waypointTag = NWScript.GetTag(waypoint);
        int count = 1;
        uint spawnWaypoint = NWScript.GetWaypointByTag(waypointTag + "s" + count);
        while (NWScript.GetIsObjectValid(spawnWaypoint) == 1)
        {
            waypointMasterList.Add(spawnWaypoint);
            count++;
            spawnWaypoint = NWScript.GetWaypointByTag(waypointTag + "s" + count);
        }

        _waypointMasterList = waypointMasterList;
    }

    public void GenerateSpawnWaypointListOverflow(uint waypoint)
    {
        List<uint> waypointOverflowMasterList = new();
        string waypointTag = NWScript.GetTag(waypoint);
        int count = 1;
        uint spawnWaypoint = NWScript.GetWaypointByTag(waypointTag + "s" + count);
        while (NWScript.GetIsObjectValid(spawnWaypoint) == 1)
        {
            waypointOverflowMasterList.Add(spawnWaypoint);
            count++;
            spawnWaypoint = NWScript.GetWaypointByTag(waypointTag + "s" + count);
        }

        _waypointOverflowMasterList = waypointOverflowMasterList;
    }

    public IntPtr GrabSpawnLocationInArea()
    {
        int size = _waypointMasterList.Count();
        Random random = new();
        int ran = random.Next(size);
        uint waypoint = _waypointMasterList[ran];
        _waypointMasterList.Remove(waypoint);
        IntPtr location = NWScript.GetLocation(waypoint);
        return location;
    }

    public IntPtr GrabSpawnLocationInOverflowArea()
    {
        int size = _waypointOverflowMasterList.Count();
        Random random = new();
        int ran = random.Next(size);
        uint waypoint = _waypointOverflowMasterList[ran];
        _waypointOverflowMasterList.Remove(waypoint);
        IntPtr location = NWScript.GetLocation(waypoint);
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
            Thread.Sleep(50);
            IntPtr randomLocation = GrabSpawnLocationInOverflowArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation);
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y;
            float zPosition = ranLocPositon.Z;

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, lieutentant, randomLocation);

            Vector3 randomPosN = NWScript.Vector(xPosition, yPosition + 1.0f, zPosition);
            Vector3 randomPosS = NWScript.Vector(xPosition, yPosition - 1.0f, zPosition);
            Vector3 randomPosE = NWScript.Vector(xPosition + 1.0f, yPosition, zPosition);
            Vector3 randomPosW = NWScript.Vector(xPosition - 1.0f, yPosition, zPosition);
            Vector3 randomPosNe = NWScript.Vector(xPosition + 1.0f, yPosition + 1.0f, zPosition);
            Vector3 randomPosNw = NWScript.Vector(xPosition - 1.0f, yPosition + 1.0f, zPosition);
            Vector3 randomPosSe = NWScript.Vector(xPosition + 1.0f, yPosition - 1.0f, zPosition);
            Vector3 randomPosSw = NWScript.Vector(xPosition - 1.0f, yPosition - 1.0f, zPosition);

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
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f, zPosition);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f, zPosition);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f, zPosition);

            CreatePlc(NWScript.Location(area, randomPLCPosNw, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSe, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSw, facing));
        }
    }

    private static uint CreatePlc(IntPtr objectLocation)
    {
        Random rnd = new();
        int random = rnd.Next(11);
        uint plc = 0;

        switch (random)
        {
            case 0:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasioncorpse1",
                    objectLocation);
                break;
            case 1:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasionmisc3", objectLocation);
                break;
            case 2:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasioncorpse3",
                    objectLocation);
                break;
            case 3:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasioncorpse5",
                    objectLocation);
                break;
            case 4:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasionmisc5", objectLocation);
                break;
            case 5:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasiondebris1",
                    objectLocation);
                break;
            case 6:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasioncamp", objectLocation);
                break;
            case 7:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasiondebris1",
                    objectLocation);
                break;
            case 8:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasioncamp", objectLocation);
                break;
            case 9:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasionmisc3", objectLocation);
                break;
            case 10:
                plc = NWScript.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, sTemplate: "invasionmisc3", objectLocation);
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
            Thread.Sleep(50);
            IntPtr randomLocation = GrabSpawnLocationInArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation);
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y;
            float zPosition = ranLocPositon.Z;

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1, randomLocation);

            Vector3 randomPosN = NWScript.Vector(xPosition, yPosition + 1.0f, zPosition);
            Vector3 randomPosS = NWScript.Vector(xPosition, yPosition - 1.0f, zPosition);
            Vector3 randomPosE = NWScript.Vector(xPosition + 1.0f, yPosition, zPosition);
            Vector3 randomPosW = NWScript.Vector(xPosition - 1.0f, yPosition, zPosition);
            Vector3 randomPosNe = NWScript.Vector(xPosition + 1.0f, yPosition + 1.0f, zPosition);
            Vector3 randomPosNw = NWScript.Vector(xPosition - 1.0f, yPosition + 1.0f, zPosition);
            Vector3 randomPosSe = NWScript.Vector(xPosition + 1.0f, yPosition - 1.0f, zPosition);
            Vector3 randomPosSw = NWScript.Vector(xPosition - 1.0f, yPosition - 1.0f, zPosition);

            if (creaturetype1 != "")
            {
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1,
                    NWScript.Location(area, randomPosN, facing));
            }
            
            if (creaturetype2 != "")
            {
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                    NWScript.Location(area, randomPosS, facing));
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype2,
                    NWScript.Location(area, randomPosNw, facing));
            }
            if (creaturetype3 != "")
            {
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                    NWScript.Location(area, randomPosE, facing));
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype3,
                    NWScript.Location(area, randomPosSe, facing));
            }
            if (creaturetype4 != "")
            {
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                    NWScript.Location(area, randomPosW, facing));
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype4,
                    NWScript.Location(area, randomPosSw, facing));
            }
            if (creaturetype5 != "")
            {
                NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype5,
                    NWScript.Location(area, randomPosNe, facing));
            }
            countMobs++;

            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f, zPosition);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f, zPosition);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f, zPosition);

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
            Thread.Sleep(50);
            IntPtr randomLocation = GrabSpawnLocationInArea();
            Vector3 ranLocPositon = NWScript.GetPositionFromLocation(randomLocation);
            float xPosition = ranLocPositon.X;
            float yPosition = ranLocPositon.Y;
            float zPosition = ranLocPositon.Z;
            uint objectCreature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, lieutentant, randomLocation);
            // PLC Spawning
            Vector3 randomPLCPosNw = NWScript.Vector(xPosition - 1.5f, yPosition + 1.5f, zPosition);
            Vector3 randomPLCPosSe = NWScript.Vector(xPosition + 1.5f, yPosition - 1.5f, zPosition);
            Vector3 randomPLCPosSw = NWScript.Vector(xPosition - 1.5f, yPosition - 1.5f, zPosition);
            CreatePlc(NWScript.Location(area, randomPLCPosNw, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSe, facing));
            CreatePlc(NWScript.Location(area, randomPLCPosSw, facing));
            countLieutenant++;
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

    private static void MassMessage(string message, string CreatureName, string AreaName, int alternateMessage)
    {
        uint objectCreature = NWScript.GetFirstPC();

        while (NWScript.GetIsObjectValid(objectCreature) == 1)
        {
            NWScript.SendMessageToPC(objectCreature, szMessage: "-----");
            NWScript.SendMessageToPC(objectCreature, szMessage: "-----");
            NWScript.SendMessageToPC(objectCreature, message);
            NWScript.SendMessageToPC(objectCreature, szMessage: "-----");
            NWScript.SendMessageToPC(objectCreature, szMessage: "-----");
            objectCreature = NWScript.GetNextPC();
        }

        if (alternateMessage == 1)
            NWScript.SetLocalString(NWScript.GetModule(), sVarName: "announcerMessage",
                "``` All adventurers begin to hear murmurs and rumors from locals about a terrifying attack devastating the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! PORTAL OPENED! " +
                CreatureName + " are rampaging in " + AreaName +
                "! We recommend ONLY an extremely skilled group of adventurers respond and common folk stay clear! ```");
        else if (alternateMessage == 2)
            NWScript.SetLocalString(NWScript.GetModule(), sVarName: "announcerMessage",
                "```A surprise raid from the " +
                CreatureName + " has snuck into " + AreaName +
                ". They are spreading and must be stopped! ```");
        else
            NWScript.SetLocalString(NWScript.GetModule(), sVarName: "announcerMessage",
                "``` All adventurers begin to hear murmurs and rumors from locals about a terrifying attack happening on the isles. You quickly receive a message from the Guilds to confirm this fact. The message is simple: WARNING! " +
                CreatureName + " are rampaging in " + AreaName +
                "! We recommend an appropriately skilled group of adventurers respond and common folk stay clear! ```");

        NWScript.ExecuteScript(sScript: "webhook_announce");
    }
}