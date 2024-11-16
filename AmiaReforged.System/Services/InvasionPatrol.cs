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
        NwObject? AnvilPC = info.ObjectSelf;

        Location Location = NWScript.GetLocation(oPC);
        uint Area = NWScript.GetAreaFromLocation(Location);
        string AreaResRef = NWScript.GetResRef(Area);
        Location StoredLocation = NWScript.GetLocalLocation(oPC, AreaResRef);
        uint StoredArea = NWScript.GetAreaFromLocation(StoredLocation);
        InvasionRecord invasionRecord = new();
        invasionRecord.AreaZone = "N/A";
        invasionRecord.InvasionPercent = 0;
        invasionRecord.RealmChaos = 0;
        List<InvasionRecord> invasions = await _invasionService.GetAllInvasionRecords();
        InvasionRecord invasionRecordTemp = invasions.Find(x => x.AreaZone == AreaResRef);
        int PatrolValue = 4;
        int rewardCount = 1;
        await NwModule.Instance.WaitForObjectContext();
        var JobJournal = NWScript.GetItemPossessedBy(oPC, "js_jobjournal");
        if (NWScript.GetIsObjectValid(JobJournal) == 1)
        {
            string PrimaryJob = NWScript.GetLocalString(JobJournal, "primaryjob");
            string SecondaryJob = NWScript.GetLocalString(JobJournal, "secondaryjob");
            if (PrimaryJob == "Soldier")
            {
                PatrolValue += 8;
                rewardCount = 3;
                NWScript.SendMessageToPC(oPC, "*Your soldier job makes you exceptional at patrols*");
            }
            else if (SecondaryJob == "Soldier")
            {
                PatrolValue += 4;
                rewardCount = 2;
                NWScript.SendMessageToPC(oPC, "*Your soldier job makes you good at patrols*");
            }
        }

        if (invasionRecordTemp != null)
        {
            invasionRecord = invasionRecordTemp;
            if ((NWScript.GetDistanceBetweenLocations(StoredLocation, Location) >= 20.0) ||
                (NWScript.GetIsObjectValid(StoredArea) != 1))
            {
                NWScript.SetLocalLocation(oPC, AreaResRef, Location);
                int temp = invasionRecord.InvasionPercent - PatrolValue;
                if (temp < 0)
                {
                    temp = 0;
                }

                if (invasionRecord.InvasionPercent > 80)
                {
                    NWScript.SendMessageToPC(oPC,
                        NWScript.GetName(Area) + " is crawling with enemies and escalation is immediate!");
                    SpawnEnemies(oPC, 4);
                    Reward(oPC, AnvilPC, rewardCount);
                }
                else if (invasionRecord.InvasionPercent > 60)
                {
                    NWScript.SendMessageToPC(oPC,
                        NWScript.GetName(Area) + " is crawling with enemies and will escalate any day now!");
                    SpawnEnemies(oPC, 3);
                    Reward(oPC, AnvilPC, rewardCount);
                }
                else if (invasionRecord.InvasionPercent > 40)
                {
                    NWScript.SendMessageToPC(oPC,
                        NWScript.GetName(Area) + " is crawling with enemies and will escalate if left alone too long!");
                    SpawnEnemies(oPC, 2);
                    Reward(oPC, AnvilPC, rewardCount);
                }
                else if (invasionRecord.InvasionPercent > 20)
                {
                    NWScript.SendMessageToPC(oPC, NWScript.GetName(Area) + " has many enemies sneaking around!");
                    SpawnEnemies(oPC, 1);
                    Reward(oPC, AnvilPC, rewardCount);
                }
                else if (invasionRecord.InvasionPercent > 10)
                {
                    NWScript.SendMessageToPC(oPC, NWScript.GetName(Area) + " has a few enemies sneaking around!");
                    Reward(oPC, AnvilPC, rewardCount);
                }
                else if (invasionRecord.InvasionPercent > 0)
                {
                    NWScript.SendMessageToPC(oPC, NWScript.GetName(Area) + " is relatively peaceful!");
                    Reward(oPC, AnvilPC, rewardCount);
                }
                else
                {
                    NWScript.SendMessageToPC(oPC, NWScript.GetName(Area) + " is completely peaceful!");
                }

                invasionRecord.InvasionPercent = temp;
                await _invasionService.UpdateInvasionArea(invasionRecord);
            }
            else
            {
                NWScript.SendMessageToPC(oPC, "*You must patrol in a different location*");
            }
        }
        else
        {
            NWScript.SendMessageToPC(oPC, "*No need to patrol this area*");
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

        while (NWScript.GetIsObjectValid(waypoint) == 1)
        {
            if (NWScript.GetArea(waypoint) == NWScript.GetArea(oPC))
            {
                waypointSpawn = waypoint;
                break;
            }

            waypointcount++;
            waypoint = NWScript.GetWaypointByTag("Invasion" + waypointcount.ToString());
        }

        int i;
        for (i = 0; i < count; i++)
        {
            ran = random.Next(1, 5);
            switch (ran)
            {
                case 1:
                    spawn = NWScript.GetLocalString(waypointSpawn, "creaturetype1");
                    break;
                case 2:
                    spawn = NWScript.GetLocalString(waypointSpawn, "creaturetype2");
                    break;
                case 3:
                    spawn = NWScript.GetLocalString(waypointSpawn, "creaturetype3");
                    break;
                case 4:
                    spawn = NWScript.GetLocalString(waypointSpawn, "creaturetype4");
                    break;
                case 5:
                    spawn = NWScript.GetLocalString(waypointSpawn, "creaturetype5");
                    break;
            }

            NWScript.CreateObject(NWScript.OBJECT_TYPE_CREATURE, spawn,
                NWScript.Location(Area, NWScript.Vector(xPosition + 0.5f, yPosition + 0.5f, zPosition), 0.0f));
        }
    }

    public void Reward(uint oPC, NwObject AnvilPC, int rewardCount)
    {
        int XP = NWScript.GetXP(oPC);
        Random random = new Random();
        int Level = NWScript.GetLevelByPosition(1, oPC) + NWScript.GetLevelByPosition(2, oPC) +
                    NWScript.GetLevelByPosition(3, oPC);

        NWScript.SendMessageToPC(oPC, "*You find something of interest on your patrol*");

        if (Level < 30)
        {
            NWScript.SetXP(oPC, XP + 25);
        }
        else
        {
            NWScript.SetXP(oPC, XP + 1);
        }

        int i;
        for (i = 0; i < rewardCount; i++)
        {
            int temp = random.Next(1, 101);

            if (temp == 50)
            {
                
                if (NWScript.GetSkillRank(0, oPC, NWScript.TRUE) >= 1) // HAS AE
                {
                    switch (random.Next(37))
                    {
                        case 0:
                            Anvil.API.NwItem.Create("js_sold_losta1", (NwGameObject?)AnvilPC);
                            break;
                        case 1:
                            Anvil.API.NwItem.Create("js_sold_losta2", (NwGameObject?)AnvilPC);
                            break;
                        case 2:
                            Anvil.API.NwItem.Create("js_sold_losta3", (NwGameObject?)AnvilPC);
                            break;
                        case 3:
                            Anvil.API.NwItem.Create("js_sold_losta4", (NwGameObject?)AnvilPC);
                            break;
                        case 4:
                            Anvil.API.NwItem.Create("js_sold_losta5", (NwGameObject?)AnvilPC);
                            break;
                        case 5:
                            Anvil.API.NwItem.Create("js_sold_losta6", (NwGameObject?)AnvilPC);
                            break;
                        case 6:
                            Anvil.API.NwItem.Create("js_sold_losta7", (NwGameObject?)AnvilPC);
                            break;
                        case 7:
                            Anvil.API.NwItem.Create("js_sold_losta8", (NwGameObject?)AnvilPC);
                            break;
                        case 8:
                            Anvil.API.NwItem.Create("js_sold_losta9", (NwGameObject?)AnvilPC);
                            break;
                        case 9:
                            Anvil.API.NwItem.Create("js_sold_losta10", (NwGameObject?)AnvilPC);
                            break;
                        case 10:
                            Anvil.API.NwItem.Create("js_sold_losta11", (NwGameObject?)AnvilPC);
                            break;
                        case 11:
                            Anvil.API.NwItem.Create("js_sold_losta12", (NwGameObject?)AnvilPC);
                            break;
                        case 12:
                            Anvil.API.NwItem.Create("js_sold_losta13", (NwGameObject?)AnvilPC);
                            break;
                        case 13:
                            Anvil.API.NwItem.Create("js_sold_losta14", (NwGameObject?)AnvilPC);
                            break;
                        case 14:
                            Anvil.API.NwItem.Create("js_sold_losta15", (NwGameObject?)AnvilPC);
                            break;
                        case 15:
                            Anvil.API.NwItem.Create("js_sold_losta16", (NwGameObject?)AnvilPC);
                            break;
                        case 16:
                            Anvil.API.NwItem.Create("js_sold_losta17", (NwGameObject?)AnvilPC);
                            break;
                        case 17:
                            Anvil.API.NwItem.Create("js_sold_losta18", (NwGameObject?)AnvilPC);
                            break;
                        case 18:
                            Anvil.API.NwItem.Create("js_sold_losta19", (NwGameObject?)AnvilPC);
                            break;
                        case 19:
                            Anvil.API.NwItem.Create("js_sold_losta20", (NwGameObject?)AnvilPC);
                            break;
                        case 20:
                            Anvil.API.NwItem.Create("js_sold_losta21", (NwGameObject?)AnvilPC);
                            break;
                        case 21:
                            Anvil.API.NwItem.Create("js_sold_losta22", (NwGameObject?)AnvilPC);
                            break;
                        case 22:
                            Anvil.API.NwItem.Create("js_sold_losta23", (NwGameObject?)AnvilPC);
                            break;
                        case 23:
                            Anvil.API.NwItem.Create("js_sold_losta24", (NwGameObject?)AnvilPC);
                            break;
                        case 24:
                            Anvil.API.NwItem.Create("js_sold_losta25", (NwGameObject?)AnvilPC);
                            break;
                        case 25:
                            Anvil.API.NwItem.Create("js_sold_losta26", (NwGameObject?)AnvilPC);
                            break;
                        case 26:
                            Anvil.API.NwItem.Create("js_sold_losta27", (NwGameObject?)AnvilPC);
                            break;
                        case 27:
                            Anvil.API.NwItem.Create("js_sold_losta28", (NwGameObject?)AnvilPC);
                            break;
                        case 28:
                            Anvil.API.NwItem.Create("js_sold_losta29", (NwGameObject?)AnvilPC);
                            break;
                        case 29:
                            Anvil.API.NwItem.Create("js_sold_losta30", (NwGameObject?)AnvilPC);
                            break;
                        case 30:
                            Anvil.API.NwItem.Create("js_sold_losta31", (NwGameObject?)AnvilPC);
                            break;
                        case 31:
                            Anvil.API.NwItem.Create("js_sold_losta32", (NwGameObject?)AnvilPC);
                            break;
                        case 32:
                            Anvil.API.NwItem.Create("js_sold_losta33", (NwGameObject?)AnvilPC);
                            break;
                        case 33:
                            Anvil.API.NwItem.Create("js_sold_losta34", (NwGameObject?)AnvilPC);
                            break;
                        case 34:
                            Anvil.API.NwItem.Create("js_sold_losta35", (NwGameObject?)AnvilPC);
                            break;
                        case 35:
                            Anvil.API.NwItem.Create("js_sold_losta36", (NwGameObject?)AnvilPC);
                            break;
                        case 36:
                            Anvil.API.NwItem.Create("js_sold_losta37", (NwGameObject?)AnvilPC);
                            break;
                    }
                }
                else // DOES NOT HAVE AE
                {
                    switch (random.Next(21))
                    {
                        case 0:
                            Anvil.API.NwItem.Create("js_sold_losta1", (NwGameObject?)AnvilPC);
                            break;
                        case 1:
                            Anvil.API.NwItem.Create("js_sold_losta2", (NwGameObject?)AnvilPC);
                            break;
                        case 2:
                            Anvil.API.NwItem.Create("js_sold_losta3", (NwGameObject?)AnvilPC);
                            break;
                        case 3:
                            Anvil.API.NwItem.Create("js_sold_losta4", (NwGameObject?)AnvilPC);
                            break;
                        case 4:
                            Anvil.API.NwItem.Create("js_sold_losta5", (NwGameObject?)AnvilPC);
                            break;
                        case 5:
                            Anvil.API.NwItem.Create("js_sold_losta6", (NwGameObject?)AnvilPC);
                            break;
                        case 6:
                            Anvil.API.NwItem.Create("js_sold_losta7", (NwGameObject?)AnvilPC);
                            break;
                        case 7:
                            Anvil.API.NwItem.Create("js_sold_losta11", (NwGameObject?)AnvilPC);
                            break;
                        case 8:
                            Anvil.API.NwItem.Create("js_sold_losta12", (NwGameObject?)AnvilPC);
                            break;
                        case 9:
                            Anvil.API.NwItem.Create("js_sold_losta13", (NwGameObject?)AnvilPC);
                            break;
                        case 10:
                            Anvil.API.NwItem.Create("js_sold_losta15", (NwGameObject?)AnvilPC);
                            break;
                        case 11:
                            Anvil.API.NwItem.Create("js_sold_losta16", (NwGameObject?)AnvilPC);
                            break;
                        case 12:
                            Anvil.API.NwItem.Create("js_sold_losta17", (NwGameObject?)AnvilPC);
                            break;
                        case 13:
                            Anvil.API.NwItem.Create("js_sold_losta19", (NwGameObject?)AnvilPC);
                            break;
                        case 14:
                            Anvil.API.NwItem.Create("js_sold_losta20", (NwGameObject?)AnvilPC);
                            break;
                        case 15:
                            Anvil.API.NwItem.Create("js_sold_losta21", (NwGameObject?)AnvilPC);
                            break;
                        case 16:
                            Anvil.API.NwItem.Create("js_sold_losta22", (NwGameObject?)AnvilPC);
                            break;
                        case 17:
                            Anvil.API.NwItem.Create("js_sold_losta23", (NwGameObject?)AnvilPC);
                            break;
                        case 18:
                            Anvil.API.NwItem.Create("js_sold_losta24", (NwGameObject?)AnvilPC);
                            break;
                        case 19:
                            Anvil.API.NwItem.Create("js_sold_losta31", (NwGameObject?)AnvilPC);
                            break;
                        case 20:
                            Anvil.API.NwItem.Create("js_sold_losta37", (NwGameObject?)AnvilPC);
                            break;

                    }
                }
            }
            else if (temp <= 30)
            {
                switch (random.Next(21))
                {
                    case 0:
                        Anvil.API.NwItem.Create("js_hun_mbone", (NwGameObject?)AnvilPC);
                        break;
                    case 1:
                        Anvil.API.NwItem.Create("js_alch_kit10", (NwGameObject?)AnvilPC);
                        break;
                    case 2:
                        Anvil.API.NwItem.Create("js_alch_pore", (NwGameObject?)AnvilPC);
                        break;
                    case 3:
                        Anvil.API.NwItem.Create("js_jew_diam", (NwGameObject?)AnvilPC);
                        break;
                    case 4:
                        Anvil.API.NwItem.Create("js_jew_emer", (NwGameObject?)AnvilPC);
                        break;
                    case 5:
                        Anvil.API.NwItem.Create("js_jew_ruby", (NwGameObject?)AnvilPC);
                        break;
                    case 6:
                        Anvil.API.NwItem.Create("js_jew_sapp", (NwGameObject?)AnvilPC);
                        break;
                    case 7:
                        Anvil.API.NwItem.Create("js_bre_fili", (NwGameObject?)AnvilPC);
                        break;
                    case 8:
                        Anvil.API.NwItem.Create("js_bre_tual", (NwGameObject?)AnvilPC);
                        break;
                    case 9:
                        Anvil.API.NwItem.Create("js_che_saus", (NwGameObject?)AnvilPC);
                        break;
                    case 10:
                        Anvil.API.NwItem.Create("js_che_roro", (NwGameObject?)AnvilPC);
                        break;
                    case 11:
                        Anvil.API.NwItem.Create("js_che_mepi", (NwGameObject?)AnvilPC);
                        break;
                    case 12:
                        Anvil.API.NwItem.Create("js_che_brea", (NwGameObject?)AnvilPC);
                        break;
                    case 13:
                        Anvil.API.NwItem.Create("js_sco_drus", (NwGameObject?)AnvilPC);
                        break;
                    case 14:
                        Anvil.API.NwItem.Create("js_venomgland", (NwGameObject?)AnvilPC);
                        break;
                    case 15:
                        Anvil.API.NwItem.Create("js_tai_scbrd1", (NwGameObject?)AnvilPC);
                        break;
                    case 16:
                        Anvil.API.NwItem.Create("js_tai_scbrd2", (NwGameObject?)AnvilPC);
                        break;
                    case 17:
                        Anvil.API.NwItem.Create("js_tai_quiver1", (NwGameObject?)AnvilPC);
                        break;
                    case 18:
                        Anvil.API.NwItem.Create("js_tai_bpack1", (NwGameObject?)AnvilPC);
                        break;
                    case 19:
                        Anvil.API.NwItem.Create("js_arca_spiderl", (NwGameObject?)AnvilPC);
                        break;
                    case 20:
                        Anvil.API.NwItem.Create("js_hun_sbone", (NwGameObject?)AnvilPC);
                        break;
                    case 21:
                        Anvil.API.NwItem.Create("js_hun_lbone", (NwGameObject?)AnvilPC);
                        break;
                }
            }
            else
            {
                switch (random.Next(2))
                {
                    case 0:
                        Anvil.API.NwItem.Create("js_sold_fang", (NwGameObject?)AnvilPC);
                        break;
                    case 1:
                        Anvil.API.NwItem.Create("js_sold_claw", (NwGameObject?)AnvilPC);
                        break;
                }
            }
        }
    }
}