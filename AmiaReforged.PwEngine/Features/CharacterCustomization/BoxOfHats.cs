using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.CharacterCustomization;

[ServiceBinding(typeof(BoxOfHats))]
public class BoxOfHats
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly BoxOfStyle _masksAndHats;
    private const string BoxOfHatsTag = "hatchanger";
    private const string BoxOfMasksTag = "maskchanger";

    private const string PcKeyResRef = "ds_pckey";

    private const string HatBoxPcKeyLocalObject = "hat_box";
    private const string MaskBoxPcKeyLocalObject = "mask_box";
    private const string IsSetUpLocalInt = "is_setup";
    private const string SelectedRaceLocalInt = "selected_race";
    private const string MaskVfxLocalInt = "mask_vfx";
    private const string HatVfxLocalInt = "hat_vfx";

    private const string HatVfxTag = "hat_vfx";
    private const string MaskVfxTag = "mask_vfx";


    private readonly Dictionary<int, string> _raceLabels = new()
    {
        { 0, "Dwarf Size" },
        { 1, "Elf Size" },
        { 2, "Gnome Size" },
        { 3, "Hin Size" },
        { 4, "Half-Elf Size" },
        { 5, "Half-Orc Size" },
        { 6, "Human Size" }
    };

    public BoxOfHats(BoxOfStyle masksAndHats)
    {
        NwModule.Instance.OnItemUse += HandleBoxOfMasks;
        NwModule.Instance.OnClientEnter += ApplyVfx;
        _masksAndHats = masksAndHats;
    }

    private void ApplyVfx(ModuleEvents.OnClientEnter obj)
    {
        NwCreature? creature = obj.Player.LoginCreature;

        if (creature is null)
        {
            Log.Info("Null critter");
            return;
        }

        Effect? existingMask = creature.ActiveEffects.FirstOrDefault(e => e.Tag == MaskVfxTag);
        if (existingMask is not null)
        {
            Log.Info("Had a mask.");
            creature.RemoveEffect(existingMask);
        }

        Effect? existingHat = creature.ActiveEffects.FirstOrDefault(e => e.Tag == HatVfxTag);
        if (existingMask is not null)
        {
            Log.Info("Had a hat.");
            creature.RemoveEffect(existingHat!);
        }

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == PcKeyResRef);

        if (pcKey is null)
        {
            Log.Info("Null PC key");
            return;
        }

        int maskVfx = NWScript.GetLocalInt(pcKey, MaskVfxLocalInt);
        int hatVfx = NWScript.GetLocalInt(pcKey, HatVfxLocalInt);

        if (maskVfx != 0)
        {
            VisualEffectTableEntry mask = NwGameTables.VisualEffectTable.GetRow(maskVfx);
            Log.Info($"Mask being set to {maskVfx}");
            Effect maskEffect = Effect.VisualEffect(mask);
            maskEffect.Tag = HatVfxTag;

            creature.ApplyEffect(EffectDuration.Permanent, maskEffect);
        }

        if (hatVfx != 0)
        {
            VisualEffectTableEntry hat = NwGameTables.VisualEffectTable.GetRow(hatVfx);
            Log.Info($"Hat being set to {hatVfx}");

            Effect hatEffect = Effect.VisualEffect(hat);
            hatEffect.Tag = HatVfxTag;

            creature.ApplyEffect(EffectDuration.Permanent, hatEffect);
        }
    }

    private void HandleBoxOfMasks(OnItemUse obj)
    {
        if (obj.Item.Tag != BoxOfHatsTag && obj.Item.Tag != BoxOfMasksTag) return;

        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        NwItem? pcKey = obj.UsedBy.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null) return;

        bool isSetUp = NWScript.GetLocalInt(obj.Item, IsSetUpLocalInt) == NWScript.TRUE;

        switch (obj.Item.Tag)
        {
            case BoxOfHatsTag:
                if (!isSetUp)
                {
                    NWScript.SetLocalObject(pcKey, HatBoxPcKeyLocalObject, obj.Item);
                    player.ActionStartConversation(player.LoginCreature!, "hatracesel_conv", true, false);
                    return;
                }

                NWScript.SetLocalObject(pcKey, HatBoxPcKeyLocalObject, obj.Item);
                player.ActionStartConversation(player.LoginCreature!, "hatchanger_conv", true, false);
                return;

            case BoxOfMasksTag:
                if (!isSetUp)
                {
                    NWScript.SetLocalObject(pcKey, MaskBoxPcKeyLocalObject, obj.Item);
                    player.ActionStartConversation(player.LoginCreature!, "maskracesel_conv", true, false);
                    return;
                }

                NWScript.SetLocalObject(pcKey, MaskBoxPcKeyLocalObject, obj.Item);
                player.ActionStartConversation(player.LoginCreature!, "maskchanger_conv", true, false);
                return;
            default:
                return;
        }
    }

    [ScriptHandler(scriptName: "hat_race_select")]
    public void HatRaceSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;
        int selectedRace = NWScript.GetLocalInt(player, "race");

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }


        uint pcKey = NWScript.GetItemPossessedBy(player, PcKeyResRef);

        uint boxOfHats = NWScript.GetLocalObject(pcKey, HatBoxPcKeyLocalObject);
        NWScript.SetLocalInt(boxOfHats, SelectedRaceLocalInt, selectedRace);
        NWScript.SetLocalInt(boxOfHats, IsSetUpLocalInt, NWScript.TRUE);

        NWScript.SetName(boxOfHats, $"Box of {_raceLabels[selectedRace]} Hats");
    }

    [ScriptHandler(scriptName: "mask_race_select")]
    public void MaskRaceSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;

        int selectedRace = NWScript.GetLocalInt(player, "race");

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }

        uint pcKey = NWScript.GetItemPossessedBy(player, PcKeyResRef);

        uint boxOfMasks = NWScript.GetLocalObject(pcKey, MaskBoxPcKeyLocalObject);

        NWScript.SetLocalInt(boxOfMasks, SelectedRaceLocalInt, selectedRace);
        NWScript.SetLocalInt(boxOfMasks, IsSetUpLocalInt, NWScript.TRUE);

        NWScript.SetName(boxOfMasks, $"Box of {_raceLabels[selectedRace]} Masks");
    }

    [ScriptHandler(scriptName: "hat_select")]
    public void HatAppearanceSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;

        int selectedHat = NWScript.GetLocalInt(player, "hat");

        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }

        NwCreature? playerCreature = player.ToNwObjectSafe<NwCreature>();

        if (playerCreature is null)
        {
            return;
        }


        WearableHat hatEnum = (WearableHat)selectedHat;
        NwItem? pcKey = playerCreature.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null)
        {
            return;
        }


        Effect? existingHat = playerCreature.ActiveEffects.FirstOrDefault(e => e.Tag == HatVfxTag);
        if (existingHat is not null)
        {
            playerCreature.RemoveEffect(existingHat);
        }

        // The hat was already removed, so return if their selection was "none"
        if (hatEnum == WearableHat.None)
        {
            return;
        }

        uint hatbox = NWScript.GetLocalObject(pcKey, HatBoxPcKeyLocalObject);
        int selectedRace = NWScript.GetLocalInt(hatbox, SelectedRaceLocalInt);
        int gender = NWScript.GetGender(player);

        int? vfx = gender == NWScript.GENDER_MALE
            ? _masksAndHats.HatsForRace[selectedRace].maleHats.GetValueOrDefault(hatEnum)
            : _masksAndHats.HatsForRace[selectedRace].femaleHats.GetValueOrDefault(hatEnum);

        if (vfx is null)
        {
            NWScript.SendMessageToPC(playerCreature,
                $"BUG REPORT: Could not select hat from {selectedHat}. Screenshot this and send this to staff on Discord or on the Forums");
            NWScript.SetLocalInt(pcKey, HatVfxLocalInt, 0);

            return;
        }

        NWScript.SetLocalInt(pcKey, HatVfxLocalInt, (int)vfx);

        VisualEffectTableEntry hatEntry = NwGameTables.VisualEffectTable.GetRow((int)vfx);
        Effect hatVfx = Effect.VisualEffect(hatEntry);
        hatVfx.Tag = HatVfxTag;

        playerCreature.ApplyEffect(EffectDuration.Permanent, hatVfx);
    }

    [ScriptHandler(scriptName: "mask_select")]
    public void MaskAppearanceSelection(CallInfo info)
    {
        uint player = info.ObjectSelf;
        int mask = NWScript.GetLocalInt(player, "mask");
        if (NWScript.GetIsObjectValid(player) != NWScript.TRUE)
        {
            return;
        }

        NwCreature? playerCreature = player.ToNwObjectSafe<NwCreature>();

        if (playerCreature is null)
        {
            return;
        }

        WearableMask maskEnum = (WearableMask)mask;

        NwItem? pcKey = playerCreature.Inventory.Items.FirstOrDefault(i => i.Tag == PcKeyResRef);
        if (pcKey is null)
        {
            return;
        }

        Effect? existingMask = playerCreature.ActiveEffects.FirstOrDefault(e => e.Tag == MaskVfxTag);
        if (existingMask is not null)
        {
            playerCreature.RemoveEffect(existingMask);
        }

        if (maskEnum == WearableMask.None)
        {
            return;
        }

        uint maskBox = NWScript.GetLocalObject(pcKey, MaskBoxPcKeyLocalObject);
        int gender = NWScript.GetGender(player);
        int race = NWScript.GetLocalInt(maskBox, SelectedRaceLocalInt);

        int? vfx = gender == NWScript.GENDER_MALE
            ? _masksAndHats.MasksForRace[race].MaleMasks.GetValueOrDefault(maskEnum)
            : _masksAndHats.MasksForRace[race].FemaleMasks.GetValueOrDefault(maskEnum);
        if (vfx is null)
        {
            NWScript.SetLocalInt(pcKey, MaskVfxLocalInt, 0);

            NWScript.SendMessageToPC(playerCreature,
                $"BUG REPORT: Could not select hat from {mask}. Screenshot this and send this to staff on Discord or on the Forums");
            return;
        }

        NWScript.SetLocalInt(pcKey, MaskVfxLocalInt, (int)vfx);

        VisualEffectTableEntry maskEntry = NwGameTables.VisualEffectTable.GetRow((int)vfx);
        Effect maskVfx = Effect.VisualEffect(maskEntry);
        maskVfx.Tag = MaskVfxTag;

        playerCreature.ApplyEffect(EffectDuration.Permanent, maskVfx);
    }
}

[ServiceBinding(typeof(BoxOfStyle))]
public sealed class BoxOfStyle
{
    public Dictionary<int, RaceHats> HatsForRace { get; }
    public Dictionary<int, RaceMasks> MasksForRace { get; }


    public BoxOfStyle()
    {
        HatsForRace = new Dictionary<int, RaceHats>
        {
            { 0, _dwarfHats },
            { 1, _elfHats },
            { 2, _hinHats },
            { 3, _hinHats },
            { 4, _humanHats },
            { 5, _halfOrcHats },
            { 6, _humanHats }
        };

        MasksForRace = new Dictionary<int, RaceMasks>
        {
            { 0, _dwarfMasks },
            { 1, _elfMasks },
            { 2, _hinMasks },
            { 3, _hinMasks },
            { 4, _humanMasks },
            { 5, _halfOrcMasks },
            { 6, _humanMasks },
        };
    }


    private readonly RaceHats _dwarfHats = new(new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1411 },
            { WearableHat.CircletGoldEmerald, 1459 },
            { WearableHat.CircletGoldSapphire, 1447 },
            { WearableHat.CircletGoldRuby, 1435 },
            { WearableHat.CircletGoldDiamond, 1471 },
            { WearableHat.CircletGoldOrnate, 1423 },
            { WearableHat.CircletSilverPlain, 1483 },
            { WearableHat.CircletSilverEmerald, 1531 },
            { WearableHat.CircletSilverSapphire, 1519 },
            { WearableHat.CircletSilverRuby, 1507 },
            { WearableHat.CircletSilverDiamond, 1543 },
            { WearableHat.CircletSilverOrnate, 1495 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1246 },
            { WearableHat.CrownGold, 1298 },
            { WearableHat.CrownKistone0, 1312 },
            { WearableHat.CrownKistone1, 1326 },
            { WearableHat.CrownKistone2, 1340 },
            { WearableHat.CrownKistone3, 1354 },
            { WearableHat.CrownKistone4, 1368 },
            { WearableHat.CrownKistone5, 1382 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1284 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1610 },
            { WearableHat.TricornePlainBlack, 1658 },
            { WearableHat.TricornePlainBeige, 1706 },
            { WearableHat.TricornePirateBrown, 1634 },
            { WearableHat.TricornePirateBlack, 1682 },
            { WearableHat.TricornePirateBeige, 1730 },
            { WearableHat.BandanaBrown, 1826 },
            { WearableHat.BandanaBlue, 1802 },
            { WearableHat.BandanaBlack, 1778 },
            { WearableHat.BandanaRed, 1754 },
            { WearableHat.VikingCapBrownGrey1, 1850 },
            { WearableHat.VikingCapBrownGrey2, 1874 },
            { WearableHat.VikingCapGrey1, 1898 },
            { WearableHat.VikingCapGrey2, 1922 },
            { WearableHat.VikingCapBlackGrey1, 1946 },
            { WearableHat.VikingCapBlackGrey2, 1970 },
            { WearableHat.VikingCapBrownGold1, 1994 },
            { WearableHat.VikingCapBrownGold2, 2018 }
        }, new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1410 },
            { WearableHat.CircletGoldEmerald, 1458 },
            { WearableHat.CircletGoldSapphire, 1446 },
            { WearableHat.CircletGoldRuby, 1434 },
            { WearableHat.CircletGoldDiamond, 1470 },
            { WearableHat.CircletGoldOrnate, 1422 },
            { WearableHat.CircletSilverPlain, 1482 },
            { WearableHat.CircletSilverEmerald, 1530 },
            { WearableHat.CircletSilverSapphire, 1518 },
            { WearableHat.CircletSilverRuby, 1506 },
            { WearableHat.CircletSilverDiamond, 1542 },
            { WearableHat.CircletSilverOrnate, 1494 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1247 },
            { WearableHat.CrownGold, 1299 },
            { WearableHat.CrownKistone0, 1313 },
            { WearableHat.CrownKistone1, 1327 },
            { WearableHat.CrownKistone2, 1341 },
            { WearableHat.CrownKistone3, 1355 },
            { WearableHat.CrownKistone4, 1369 },
            { WearableHat.CrownKistone5, 1383 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1285 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1612 },
            { WearableHat.TricornePlainBlack, 1660 },
            { WearableHat.TricornePlainBeige, 1708 },
            { WearableHat.TricornePirateBrown, 1636 },
            { WearableHat.TricornePirateBlack, 1684 },
            { WearableHat.TricornePirateBeige, 1732 },
            { WearableHat.BandanaBrown, 1828 },
            { WearableHat.BandanaBlue, 1804 },
            { WearableHat.BandanaBlack, 1780 },
            { WearableHat.BandanaRed, 1756 },
            { WearableHat.VikingCapBrownGrey1, 1852 },
            { WearableHat.VikingCapBrownGrey2, 1876 },
            { WearableHat.VikingCapGrey1, 1900 },
            { WearableHat.VikingCapGrey2, 1924 },
            { WearableHat.VikingCapBlackGrey1, 1948 },
            { WearableHat.VikingCapBlackGrey2, 1972 },
            { WearableHat.VikingCapBrownGold1, 1996 },
            { WearableHat.VikingCapBrownGold2, 2020 }
        }
    );

    private readonly RaceMasks _dwarfMasks = new RaceMasks(
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1555 },
            { WearableMask.LensesSilver, 1567 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2129 },
            { WearableMask.Glasses4, 2130 },
            { WearableMask.Glasses5, 2131 },
            { WearableMask.Glasses6, 2132 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1148 },
            { WearableMask.Butterfly, 1218 },
            { WearableMask.Mask01, 1162 },
            { WearableMask.Mask02, 1190 },
            { WearableMask.Mask03, 1204 },
            { WearableMask.Veil, 1396 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1605 }
        },
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1554 },
            { WearableMask.LensesSilver, 1566 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2124 },
            { WearableMask.Glasses4, 2125 },
            { WearableMask.Glasses5, 2126 },
            { WearableMask.Glasses6, 2127 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1149 },
            { WearableMask.Butterfly, 1219 },
            { WearableMask.Mask01, 1163 },
            { WearableMask.Mask02, 1191 },
            { WearableMask.Mask03, 1205 },
            { WearableMask.Veil, 1397 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1604 }
        }
    );

    private readonly RaceHats _humanHats = new RaceHats(new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1421 },
            { WearableHat.CircletGoldEmerald, 1469 },
            { WearableHat.CircletGoldSapphire, 1457 },
            { WearableHat.CircletGoldRuby, 1445 },
            { WearableHat.CircletGoldDiamond, 1481 },
            { WearableHat.CircletGoldOrnate, 1433 },
            { WearableHat.CircletSilverPlain, 1493 },
            { WearableHat.CircletSilverEmerald, 1541 },
            { WearableHat.CircletSilverSapphire, 1529 },
            { WearableHat.CircletSilverRuby, 1517 },
            { WearableHat.CircletSilverDiamond, 1553 },
            { WearableHat.CircletSilverOrnate, 1505 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1258 },
            { WearableHat.CrownGold, 1310 },
            { WearableHat.CrownKistone0, 1324 },
            { WearableHat.CrownKistone1, 1338 },
            { WearableHat.CrownKistone2, 1352 },
            { WearableHat.CrownKistone3, 1366 },
            { WearableHat.CrownKistone4, 1380 },
            { WearableHat.CrownKistone5, 1394 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1296 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1606 },
            { WearableHat.TricornePlainBlack, 1654 },
            { WearableHat.TricornePlainBeige, 1702 },
            { WearableHat.TricornePirateBrown, 1630 },
            { WearableHat.TricornePirateBlack, 1678 },
            { WearableHat.TricornePirateBeige, 1726 },
            { WearableHat.BandanaBrown, 1822 },
            { WearableHat.BandanaBlue, 1798 },
            { WearableHat.BandanaBlack, 1774 },
            { WearableHat.BandanaRed, 1750 },
            { WearableHat.VikingCapBrownGrey1, 1846 },
            { WearableHat.VikingCapBrownGrey2, 1870 },
            { WearableHat.VikingCapGrey1, 1894 },
            { WearableHat.VikingCapGrey2, 1918 },
            { WearableHat.VikingCapBlackGrey1, 1942 },
            { WearableHat.VikingCapBlackGrey2, 1966 },
            { WearableHat.VikingCapBrownGold1, 1990 },
            { WearableHat.VikingCapBrownGold2, 2014 }
        }, new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1420 },
            { WearableHat.CircletGoldEmerald, 1468 },
            { WearableHat.CircletGoldSapphire, 1456 },
            { WearableHat.CircletGoldRuby, 1444 },
            { WearableHat.CircletGoldDiamond, 1480 },
            { WearableHat.CircletGoldOrnate, 1432 },
            { WearableHat.CircletSilverPlain, 1492 },
            { WearableHat.CircletSilverEmerald, 1540 },
            { WearableHat.CircletSilverSapphire, 1528 },
            { WearableHat.CircletSilverRuby, 1516 },
            { WearableHat.CircletSilverDiamond, 1552 },
            { WearableHat.CircletSilverOrnate, 1504 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1259 },
            { WearableHat.CrownGold, 1311 },
            { WearableHat.CrownKistone0, 1325 },
            { WearableHat.CrownKistone1, 1339 },
            { WearableHat.CrownKistone2, 1353 },
            { WearableHat.CrownKistone3, 1367 },
            { WearableHat.CrownKistone4, 1381 },
            { WearableHat.CrownKistone5, 1395 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1297 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1608 },
            { WearableHat.TricornePlainBlack, 1656 },
            { WearableHat.TricornePlainBeige, 1704 },
            { WearableHat.TricornePirateBrown, 1632 },
            { WearableHat.TricornePirateBlack, 1680 },
            { WearableHat.TricornePirateBeige, 1728 },
            { WearableHat.BandanaBrown, 1824 },
            { WearableHat.BandanaBlue, 1800 },
            { WearableHat.BandanaBlack, 1776 },
            { WearableHat.BandanaRed, 1752 },
            { WearableHat.VikingCapBrownGrey1, 1848 },
            { WearableHat.VikingCapBrownGrey2, 1872 },
            { WearableHat.VikingCapGrey1, 1896 },
            { WearableHat.VikingCapGrey2, 1920 },
            { WearableHat.VikingCapBlackGrey1, 1944 },
            { WearableHat.VikingCapBlackGrey2, 1968 },
            { WearableHat.VikingCapBrownGold1, 1992 },
            { WearableHat.VikingCapBrownGold2, 2016 }
        }
    );

    private readonly RaceMasks _humanMasks = new RaceMasks(
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1565 },
            { WearableMask.LensesSilver, 1577 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2129 },
            { WearableMask.Glasses4, 2130 },
            { WearableMask.Glasses5, 2131 },
            { WearableMask.Glasses6, 2132 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1160 },
            { WearableMask.Butterfly, 1230 },
            { WearableMask.Mask01, 1174 },
            { WearableMask.Mask02, 1202 },
            { WearableMask.Mask03, 1216 },
            { WearableMask.Veil, 1408 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1605 }
        },
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1564 },
            { WearableMask.LensesSilver, 1576 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2124 },
            { WearableMask.Glasses4, 2125 },
            { WearableMask.Glasses5, 2126 },
            { WearableMask.Glasses6, 2127 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1161 },
            { WearableMask.Butterfly, 1231 },
            { WearableMask.Mask01, 1175 },
            { WearableMask.Mask02, 1203 },
            { WearableMask.Mask03, 1217 },
            { WearableMask.Veil, 1409 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1604 }
        }
    );

    private readonly RaceHats _elfHats = new RaceHats(new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1413 },
            { WearableHat.CircletGoldEmerald, 1461 },
            { WearableHat.CircletGoldSapphire, 1449 },
            { WearableHat.CircletGoldRuby, 1437 },
            { WearableHat.CircletGoldDiamond, 1473 },
            { WearableHat.CircletGoldOrnate, 1425 },
            { WearableHat.CircletSilverPlain, 1485 },
            { WearableHat.CircletSilverEmerald, 1533 },
            { WearableHat.CircletSilverSapphire, 1521 },
            { WearableHat.CircletSilverRuby, 1509 },
            { WearableHat.CircletSilverDiamond, 1545 },
            { WearableHat.CircletSilverOrnate, 1497 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown02, 1248 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.CrownGold, 1300 },
            { WearableHat.CrownKistone0, 1314 },
            { WearableHat.CrownKistone1, 1328 },
            { WearableHat.CrownKistone2, 1342 },
            { WearableHat.CrownKistone3, 1356 },
            { WearableHat.CrownKistone4, 1370 },
            { WearableHat.CrownKistone5, 1384 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1286 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1614 },
            { WearableHat.TricornePlainBlack, 1662 },
            { WearableHat.TricornePlainBeige, 1710 },
            { WearableHat.TricornePirateBrown, 1638 },
            { WearableHat.TricornePirateBlack, 1686 },
            { WearableHat.TricornePirateBeige, 1734 },
            { WearableHat.BandanaBrown, 1830 },
            { WearableHat.BandanaBlue, 1806 },
            { WearableHat.BandanaBlack, 1782 },
            { WearableHat.BandanaRed, 1758 },
            { WearableHat.VikingCapBrownGrey1, 1854 },
            { WearableHat.VikingCapBrownGrey2, 1878 },
            { WearableHat.VikingCapGrey1, 1902 },
            { WearableHat.VikingCapGrey2, 1926 },
            { WearableHat.VikingCapBlackGrey1, 1950 },
            { WearableHat.VikingCapBlackGrey2, 1974 },
            { WearableHat.VikingCapBrownGold1, 1998 },
            { WearableHat.VikingCapBrownGold2, 2022 }
        }, new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1412 },
            { WearableHat.CircletGoldEmerald, 1460 },
            { WearableHat.CircletGoldSapphire, 1448 },
            { WearableHat.CircletGoldRuby, 1436 },
            { WearableHat.CircletGoldDiamond, 1472 },
            { WearableHat.CircletGoldOrnate, 1424 },
            { WearableHat.CircletSilverPlain, 1484 },
            { WearableHat.CircletSilverEmerald, 1532 },
            { WearableHat.CircletSilverSapphire, 1520 },
            { WearableHat.CircletSilverRuby, 1508 },
            { WearableHat.CircletSilverDiamond, 1544 },
            { WearableHat.CircletSilverOrnate, 1496 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1249 },
            { WearableHat.CrownGold, 1301 },
            { WearableHat.CrownKistone0, 1315 },
            { WearableHat.CrownKistone1, 1329 },
            { WearableHat.CrownKistone2, 1343 },
            { WearableHat.CrownKistone3, 1357 },
            { WearableHat.CrownKistone4, 1371 },
            { WearableHat.CrownKistone5, 1385 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1287 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1616 },
            { WearableHat.TricornePlainBlack, 1664 },
            { WearableHat.TricornePlainBeige, 1712 },
            { WearableHat.TricornePirateBrown, 1640 },
            { WearableHat.TricornePirateBlack, 1688 },
            { WearableHat.TricornePirateBeige, 1736 },
            { WearableHat.BandanaBrown, 1832 },
            { WearableHat.BandanaBlue, 1808 },
            { WearableHat.BandanaBlack, 1784 },
            { WearableHat.BandanaRed, 1760 },
            { WearableHat.VikingCapBrownGrey1, 1856 },
            { WearableHat.VikingCapBrownGrey2, 1880 },
            { WearableHat.VikingCapGrey1, 1904 },
            { WearableHat.VikingCapGrey2, 1928 },
            { WearableHat.VikingCapBlackGrey1, 1952 },
            { WearableHat.VikingCapBlackGrey2, 1976 },
            { WearableHat.VikingCapBrownGold1, 2000 },
            { WearableHat.VikingCapBrownGold2, 2024 }
        }
    );

    private readonly RaceMasks _elfMasks = new RaceMasks(
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1557 },
            { WearableMask.LensesSilver, 1569 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2129 },
            { WearableMask.Glasses4, 2130 },
            { WearableMask.Glasses5, 2131 },
            { WearableMask.Glasses6, 2132 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1150 },
            { WearableMask.Butterfly, 1220 },
            { WearableMask.Mask01, 1164 },
            { WearableMask.Mask02, 1192 },
            { WearableMask.Mask03, 1206 },
            { WearableMask.Veil, 1398 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1605 }
        },
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1556 },
            { WearableMask.LensesSilver, 1568 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2124 },
            { WearableMask.Glasses4, 2125 },
            { WearableMask.Glasses5, 2126 },
            { WearableMask.Glasses6, 2127 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1151 },
            { WearableMask.Butterfly, 1221 },
            { WearableMask.Mask01, 1165 },
            { WearableMask.Mask02, 1193 },
            { WearableMask.Mask03, 1207 },
            { WearableMask.Veil, 1399 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1604 }
        }
    );

    private readonly RaceHats _hinHats = new RaceHats(
        new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1417 },
            { WearableHat.CircletGoldEmerald, 1465 },
            { WearableHat.CircletGoldSapphire, 1453 },
            { WearableHat.CircletGoldRuby, 1441 },
            { WearableHat.CircletGoldDiamond, 1477 },
            { WearableHat.CircletGoldOrnate, 1429 },
            { WearableHat.CircletSilverPlain, 1489 },
            { WearableHat.CircletSilverEmerald, 1537 },
            { WearableHat.CircletSilverSapphire, 1525 },
            { WearableHat.CircletSilverRuby, 1513 },
            { WearableHat.CircletSilverDiamond, 1549 },
            { WearableHat.CircletSilverOrnate, 1501 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1252 },
            { WearableHat.CrownGold, 1304 },
            { WearableHat.CrownKistone0, 1318 },
            { WearableHat.CrownKistone1, 1332 },
            { WearableHat.CrownKistone2, 1346 },
            { WearableHat.CrownKistone3, 1360 },
            { WearableHat.CrownKistone4, 1374 },
            { WearableHat.CrownKistone5, 1388 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1290 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1622 },
            { WearableHat.TricornePlainBlack, 1670 },
            { WearableHat.TricornePlainBeige, 1718 },
            { WearableHat.TricornePirateBrown, 1646 },
            { WearableHat.TricornePirateBlack, 1694 },
            { WearableHat.TricornePirateBeige, 1742 },
            { WearableHat.BandanaBrown, 1838 },
            { WearableHat.BandanaBlue, 1814 },
            { WearableHat.BandanaBlack, 1790 },
            { WearableHat.BandanaRed, 1766 },
            { WearableHat.VikingCapBrownGrey1, 1862 },
            { WearableHat.VikingCapBrownGrey2, 1886 },
            { WearableHat.VikingCapGrey1, 1910 },
            { WearableHat.VikingCapGrey2, 1934 },
            { WearableHat.VikingCapBlackGrey1, 1958 },
            { WearableHat.VikingCapBlackGrey2, 1982 },
            { WearableHat.VikingCapBrownGold1, 2006 },
            { WearableHat.VikingCapBrownGold2, 2030 }
        },
        new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1416 },
            { WearableHat.CircletGoldEmerald, 1464 },
            { WearableHat.CircletGoldSapphire, 1452 },
            { WearableHat.CircletGoldRuby, 1440 },
            { WearableHat.CircletGoldDiamond, 1476 },
            { WearableHat.CircletGoldOrnate, 1428 },
            { WearableHat.CircletSilverPlain, 1488 },
            { WearableHat.CircletSilverEmerald, 1536 },
            { WearableHat.CircletSilverSapphire, 1524 },
            { WearableHat.CircletSilverRuby, 1512 },
            { WearableHat.CircletSilverDiamond, 1548 },
            { WearableHat.CircletSilverOrnate, 1500 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1582 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1253 },
            { WearableHat.CrownGold, 1305 },
            { WearableHat.CrownKistone0, 1319 },
            { WearableHat.CrownKistone1, 1333 },
            { WearableHat.CrownKistone2, 1347 },
            { WearableHat.CrownKistone3, 1361 },
            { WearableHat.CrownKistone4, 1375 },
            { WearableHat.CrownKistone5, 1389 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1291 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1624 },
            { WearableHat.TricornePlainBlack, 1672 },
            { WearableHat.TricornePlainBeige, 1720 },
            { WearableHat.TricornePirateBrown, 1648 },
            { WearableHat.TricornePirateBlack, 1696 },
            { WearableHat.TricornePirateBeige, 1744 },
            { WearableHat.BandanaBrown, 1840 },
            { WearableHat.BandanaBlue, 1816 },
            { WearableHat.BandanaBlack, 1792 },
            { WearableHat.BandanaRed, 1768 },
            { WearableHat.VikingCapBrownGrey1, 1864 },
            { WearableHat.VikingCapBrownGrey2, 1888 },
            { WearableHat.VikingCapGrey1, 1912 },
            { WearableHat.VikingCapGrey2, 1936 },
            { WearableHat.VikingCapBlackGrey1, 1960 },
            { WearableHat.VikingCapBlackGrey2, 1984 },
            { WearableHat.VikingCapBrownGold1, 2008 },
            { WearableHat.VikingCapBrownGold2, 2032 }
        }
    );

    private readonly RaceMasks _hinMasks = new RaceMasks(
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1561 },
            { WearableMask.LensesSilver, 1573 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2129 },
            { WearableMask.Glasses4, 2130 },
            { WearableMask.Glasses5, 2131 },
            { WearableMask.Glasses6, 2132 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1154 },
            { WearableMask.Butterfly, 1224 },
            { WearableMask.Mask01, 1168 },
            { WearableMask.Mask02, 1196 },
            { WearableMask.Mask03, 1210 },
            { WearableMask.Veil, 1402 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1605 }
        },
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1560 },
            { WearableMask.LensesSilver, 1572 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2124 },
            { WearableMask.Glasses4, 2125 },
            { WearableMask.Glasses5, 2126 },
            { WearableMask.Glasses6, 2127 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1155 },
            { WearableMask.Butterfly, 1225 },
            { WearableMask.Mask01, 1169 },
            { WearableMask.Mask02, 1197 },
            { WearableMask.Mask03, 1211 },
            { WearableMask.Veil, 1403 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1604 }
        }
    );

    private readonly RaceHats _halfOrcHats = new RaceHats(new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1419 },
            { WearableHat.CircletGoldEmerald, 1467 },
            { WearableHat.CircletGoldSapphire, 1455 },
            { WearableHat.CircletGoldRuby, 1443 },
            { WearableHat.CircletGoldDiamond, 1479 },
            { WearableHat.CircletGoldOrnate, 1431 },
            { WearableHat.CircletSilverPlain, 1491 },
            { WearableHat.CircletSilverEmerald, 1539 },
            { WearableHat.CircletSilverSapphire, 1527 },
            { WearableHat.CircletSilverRuby, 1515 },
            { WearableHat.CircletSilverDiamond, 1551 },
            { WearableHat.CircletSilverOrnate, 1503 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1583 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1256 },
            { WearableHat.CrownGold, 1308 },
            { WearableHat.CrownKistone0, 1322 },
            { WearableHat.CrownKistone1, 1336 },
            { WearableHat.CrownKistone2, 1350 },
            { WearableHat.CrownKistone3, 1364 },
            { WearableHat.CrownKistone4, 1378 },
            { WearableHat.CrownKistone5, 1392 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1294 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1626 },
            { WearableHat.TricornePlainBlack, 1674 },
            { WearableHat.TricornePlainBeige, 1722 },
            { WearableHat.TricornePirateBrown, 1650 },
            { WearableHat.TricornePirateBlack, 1698 },
            { WearableHat.TricornePirateBeige, 1746 },
            { WearableHat.BandanaBrown, 1842 },
            { WearableHat.BandanaBlue, 1818 },
            { WearableHat.BandanaBlack, 1794 },
            { WearableHat.BandanaRed, 1770 },
            { WearableHat.VikingCapBrownGrey1, 1866 },
            { WearableHat.VikingCapBrownGrey2, 1890 },
            { WearableHat.VikingCapGrey1, 1914 },
            { WearableHat.VikingCapGrey2, 1938 },
            { WearableHat.VikingCapBlackGrey1, 1962 },
            { WearableHat.VikingCapBlackGrey2, 1986 },
            { WearableHat.VikingCapBrownGold1, 2010 },
            { WearableHat.VikingCapBrownGold2, 2034 }
        },
        new Dictionary<WearableHat, int>()
        {
            { WearableHat.CircletGoldPlain, 1418 },
            { WearableHat.CircletGoldEmerald, 1466 },
            { WearableHat.CircletGoldSapphire, 1454 },
            { WearableHat.CircletGoldRuby, 1442 },
            { WearableHat.CircletGoldDiamond, 1478 },
            { WearableHat.CircletGoldOrnate, 1430 },
            { WearableHat.CircletSilverPlain, 1490 },
            { WearableHat.CircletSilverEmerald, 1538 },
            { WearableHat.CircletSilverSapphire, 1526 },
            { WearableHat.CircletSilverRuby, 1514 },
            { WearableHat.CircletSilverDiamond, 1550 },
            { WearableHat.CircletSilverOrnate, 1502 },
            { WearableHat.Crown1, 1581 },
            { WearableHat.Crown2, 1583 },
            { WearableHat.Crown3, 1584 },
            { WearableHat.Crown02, 1257 },
            { WearableHat.CrownGold, 1309 },
            { WearableHat.CrownKistone0, 1323 },
            { WearableHat.CrownKistone1, 1337 },
            { WearableHat.CrownKistone2, 1351 },
            { WearableHat.CrownKistone3, 1365 },
            { WearableHat.CrownKistone4, 1379 },
            { WearableHat.CrownKistone5, 1393 },
            { WearableHat.BowlerHat, 1580 },
            { WearableHat.Hood1, 1592 },
            { WearableHat.WizardHat, 1600 },
            { WearableHat.WitchHat, 1599 },
            { WearableHat.TopHat, 1295 },
            { WearableHat.TopHatTall, 1598 },
            { WearableHat.TricornePlainBrown, 1628 },
            { WearableHat.TricornePlainBlack, 1676 },
            { WearableHat.TricornePlainBeige, 1724 },
            { WearableHat.TricornePirateBrown, 1652 },
            { WearableHat.TricornePirateBlack, 1700 },
            { WearableHat.TricornePirateBeige, 1748 },
            { WearableHat.BandanaBrown, 1844 },
            { WearableHat.BandanaBlue, 1820 },
            { WearableHat.BandanaBlack, 1796 },
            { WearableHat.BandanaRed, 1772 },
            { WearableHat.VikingCapBrownGrey1, 1868 },
            { WearableHat.VikingCapBrownGrey2, 1892 },
            { WearableHat.VikingCapGrey1, 1916 },
            { WearableHat.VikingCapGrey2, 1940 },
            { WearableHat.VikingCapBlackGrey1, 1964 },
            { WearableHat.VikingCapBlackGrey2, 1988 },
            { WearableHat.VikingCapBrownGold1, 2012 },
            { WearableHat.VikingCapBrownGold2, 2036 }
        }
    );

    private readonly RaceMasks _halfOrcMasks = new RaceMasks(
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1563 },
            { WearableMask.LensesSilver, 1575 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2129 },
            { WearableMask.Glasses4, 2130 },
            { WearableMask.Glasses5, 2131 },
            { WearableMask.Glasses6, 2132 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1158 },
            { WearableMask.Butterfly, 1228 },
            { WearableMask.Mask01, 1172 },
            { WearableMask.Mask02, 1200 },
            { WearableMask.Mask03, 1214 },
            { WearableMask.Veil, 1406 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1605 }
        },
        new Dictionary<WearableMask, int>()
        {
            { WearableMask.LensesBlack, 1562 },
            { WearableMask.LensesSilver, 1574 },
            { WearableMask.Glasses1, 1587 },
            { WearableMask.Glasses2, 1588 },
            { WearableMask.Glasses3, 1589 },
            { WearableMask.Glasses3A, 2124 },
            { WearableMask.Glasses4, 2125 },
            { WearableMask.Glasses5, 2126 },
            { WearableMask.Glasses6, 2127 },
            { WearableMask.Shades1, 1594 },
            { WearableMask.Shades2, 1595 },
            { WearableMask.Monocle1, 1593 },
            { WearableMask.Goggles1, 1590 },
            { WearableMask.Goggles2, 1591 },
            { WearableMask.MaskStar, 1159 },
            { WearableMask.Butterfly, 1229 },
            { WearableMask.Mask01, 1173 },
            { WearableMask.Mask02, 1201 },
            { WearableMask.Mask03, 1215 },
            { WearableMask.Veil, 1407 },
            { WearableMask.Bandage, 1579 },
            { WearableMask.Flower, 1586 },
            { WearableMask.Mask0, 1604 }
        }
    );

    private readonly RaceMasks _gnomeMasks = new RaceMasks(
        new Dictionary<WearableMask, int>()
        {
        },
        new Dictionary<WearableMask, int>()
        {
        }
    );
}

public record RaceHats(Dictionary<WearableHat, int> maleHats, Dictionary<WearableHat, int> femaleHats);

public record RaceMasks(Dictionary<WearableMask, int> MaleMasks, Dictionary<WearableMask, int> FemaleMasks);

public enum WearableMask
{
    None = 0,

    // Lenses
    LensesBlack = 1, // gold, round
    LensesSilver = 2, // silver, round
    Glasses1 = 3, // gray, square*
    Glasses2 = 4, // gold, red lenses*
    Glasses3 = 5, // gold, hexagonal*
    Glasses3A = 6, // silver, blue lenses*, also called glasses3 in list
    Glasses4 = 7, // gold, green lenses*
    Glasses5 = 8, // silver, black lenses*
    Glasses6 = 9, // silver, gray lenses*
    Shades1 = 10, // black, black lenses*
    Shades2 = 11, // silver, silver lenses*
    Monocle1 = 12, // gold, monocle*

    // Goggles
    Goggles1 = 13, // black, over eyes*
    Goggles2 = 14, // black, forehead*

    // Masks
    MaskStar = 15,
    Butterfly = 16,
    Mask01 = 17, // domino, black and red
    Mask02 = 18, // domino, white
    Mask03 = 19, // domino, black
    Veil = 20, // half niqab
    Bandage = 21, // blindfold, white*
    Flower = 22, // rose*
    Mask0 = 23, // tribal*
}

public enum WearableHat
{
    None = 0,

    // Circlets
    CircletGoldPlain = 1,
    CircletGoldEmerald = 2,
    CircletGoldSapphire = 3,
    CircletGoldRuby = 4,
    CircletGoldOrnate = 5,
    CircletGoldDiamond = 6,
    CircletSilverPlain = 7,
    CircletSilverEmerald = 8,
    CircletSilverSapphire = 9,
    CircletSilverRuby = 10,
    CircletSilverOrnate = 11,
    CircletSilverDiamond = 12,
    CrownKistone0 = 18, //gold diamond
    CrownKistone1 = 19, // gold ruby
    CrownKistone2 = 20, // silver sapphire
    CrownKistone3 = 21, // silver emerald
    CrownKistone4 = 22, // gold topaz
    CrownKistone5 = 23, // silver amethyst


    // Crowns
    Crown1 = 13, // Gold Crown, Dull
    Crown2 = 14, // Iron Crown
    Crown3 = 15, // Crown of Roses
    Crown02 = 16, // Crown of Fangs
    CrownGold = 17, // Gold Crown, Jeweled

    // Hats & Hoods
    Hood1 = 24,
    BowlerHat = 25,
    WizardHat = 26,
    WitchHat = 27,
    TopHat = 28,
    TopHatTall = 29,

    // Tricornes
    TricornePlainBrown = 30,
    TricornePlainBlack = 31,
    TricornePlainBeige = 32,
    TricornePirateBrown = 33,
    TricornePirateBlack = 34,
    TricornePirateBeige = 35,

    // Bandanas
    BandanaBrown = 36,
    BandanaBlack = 37,
    BandanaBlue = 38,
    BandanaRed = 39,

    // Viking Caps
    VikingCapBrownGrey1 = 40, // brown, neck guard
    VikingCapBrownGrey2 = 41, // brown
    VikingCapGrey1 = 42, // gray, neck guard
    VikingCapGrey2 = 43, // gray
    VikingCapBlackGrey1 = 44, // black, neck guard
    VikingCapBlackGrey2 = 45, // black
    VikingCapBrownGold1 = 46, // gold, neck guard
    VikingCapBrownGold2 = 47, // gold
}
