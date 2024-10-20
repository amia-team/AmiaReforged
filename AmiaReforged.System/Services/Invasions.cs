using System.Numerics;
using NWN.Core;
using AmiaReforged.System;

namespace AmiaReforged.System.Services;


[ServiceBinding(typeof(Invasions))]
public class Invasions
{
    public void InvasionGeneric(uint area, int size, int random, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5, string lieutentant, string boss,
        string message)
    {
        const int totalPlc = 150;
        Random rnd = new();
        int totalMobs = Convert.ToInt32(rnd.Next(random) + size);
        int totalLieutentants = Convert.ToInt32(rnd.Next(random / 2) + size * 0.75);

        SpawnPlc(area, totalPlc);

        NWScript.DelayCommand(15.0f,
            () => SpawnMobs(area, totalMobs, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
                creaturetype5));
        NWScript.DelayCommand(30.0f, () => SpawnLieutenants(area, totalLieutentants, lieutentant));
        NWScript.DelayCommand(45.0f, () => SpawnBoss(area, boss));
        NWScript.DelayCommand(60.0f, () => MassMessage(message));
    }

    public void InvasionBeasts(uint area, int size, int random)
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

        InvasionGeneric(area, size, random, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }


    public void InvasionGoblins(uint area, int size, int random)
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

        InvasionGeneric(area, size, random, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }


    public void InvasionTrolls(uint area, int size, int random)
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

        InvasionGeneric(area, size, random, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }


    public void InvasionOrcs(uint area, int size, int random)
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

        InvasionGeneric(area, size, random, creaturetype1, creaturetype2, creaturetype3, creaturetype4,
            creaturetype5, lieutentant, boss, message);
    }

    public static IntPtr GenerateRandomLocation(uint area)
    {
        // const float zPosition = 0.0f;
        const float facing = 0.0f;


        // Determining the width and height of the area in tiles
        int widthInTiles = NWScript.GetAreaSize(NWScript.AREA_WIDTH, area);
        int heightInTiles = NWScript.GetAreaSize(NWScript.AREA_HEIGHT, area);
        int widthInMeters = widthInTiles * 10;
        int heightInMeters = heightInTiles * 10;

        //Generate a random position in the area
        float xPosition = Convert.ToSingle(NWScript.Random(widthInMeters * 10) / 10.0);
        float yPosition = Convert.ToSingle(NWScript.Random(heightInMeters * 10) / 10.0);
        Vector3 randomPositon = NWScript.Vector(xPosition, yPosition);
        IntPtr randomLocation = NWScript.Location(area, randomPositon, facing);
        return randomLocation;
    }

    private void SpawnPlc(uint area, int totalPLC)
    {
        int countPlc = 0;
        while (countPlc < totalPLC)
        {
            IntPtr randomLocation = GenerateRandomLocation(area);
            uint objectPlc = CreatePlc(randomLocation);

            if (NWScript.GetIsObjectValid(objectPlc) == NWScript.TRUE) countPlc++;
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

    private static void SpawnMobs(uint area, int totalMobs, string creaturetype1, string creaturetype2,
        string creaturetype3, string creaturetype4, string creaturetype5)
    {
        int countMobs = 0;
        // const float zPosition = 0.0f;
        const float facing = 0.0f;

        while (countMobs < totalMobs)
        {
            // Determining the width and height of the area in tiles
            int widthInTiles = NWScript.GetAreaSize(NWScript.AREA_WIDTH, area);
            int heightInTiles = NWScript.GetAreaSize(NWScript.AREA_HEIGHT, area);
            int widthInMeters = widthInTiles * 10;
            int heightInMeters = heightInTiles * 10;

            //Generate a random position in the area
            float xPosition = Convert.ToSingle(NWScript.Random(widthInMeters * 10) / 10.0);
            float yPosition = Convert.ToSingle(NWScript.Random(heightInMeters * 10) / 10.0);
            Vector3 randomPosition = NWScript.Vector(xPosition, yPosition);
            IntPtr randomLocation = NWScript.Location(area, randomPosition, facing);


            uint creature1 = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, creaturetype1, randomLocation);

            if (NWScript.GetIsObjectValid(creature1) != NWScript.TRUE) continue;

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
        }
    }

    private void SpawnLieutenants(uint area, int totalLieutentants, string lieutentant)
    {
        int countLieutenant = 0;

        while (countLieutenant < totalLieutentants)
        {
            IntPtr randomLocation = GenerateRandomLocation(area);
            uint objectCreature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, lieutentant, randomLocation);

            if (NWScript.GetIsObjectValid(objectCreature) == NWScript.TRUE) countLieutenant++;
        }
    }

    private void SpawnBoss(uint area, string boss)
    {
        int countBoss = 0;

        while (countBoss < 1)
        {
            IntPtr randomLocation = GenerateRandomLocation(area);
            uint objectCreature = NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, boss, randomLocation);

            if (NWScript.GetIsObjectValid(objectCreature) == NWScript.TRUE) countBoss++;
        }
    }

    private static void MassMessage(string message)
    {
        uint objectCreature = NWScript.GetFirstPC();

        while (NWScript.GetIsObjectValid(objectCreature) == NWScript.TRUE)
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