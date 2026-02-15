using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockUtilityHandler))]
public class WarlockUtilityHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockUtilityHandler(EventService eventService)
    {
        NwModule.Instance.OnClientEnter += GiveFeats;
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(RemoveIllegalFeats, EventCallbackType.After);
        NwModule.Instance.OnClientEnter += GivePactToken;
        NwModule.Instance.OnClientEnter += GiveEnergyToken;
        NwModule.Instance.OnClientEnter += GiveRelevelToken;
        NwModule.Instance.OnItemUse += TokenGivePactFeat;
        NwModule.Instance.OnItemUse += TokenGiveEnergyFeats;
        NwModule.Instance.OnItemUse += TokenRelevel;
        NwModule.Instance.OnEffectRemove += RemoveCustomShape;
        Log.Info(message: "Warlock Utility Handler initialized.");
    }

    private void GiveFeats(ModuleEvents.OnClientEnter obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Player.LoginCreature) <= 0) return;

        NwCreature warlock = obj.Player.LoginCreature;

        if (NWScript.GetLevelByClass(57, warlock) >= 1 && NWScript.GetHasFeat(1307, warlock) == 0)
        {
            warlock.AddFeat(NwFeat.FromFeatId(1307));
            obj.Player.SendServerMessage(WarlockUtils.String(message: "Armored Caster feat added."));
        }

        if (NWScript.GetLevelByClass(57, warlock) >= 3 && NWScript.GetHasFeat(1308, warlock) == 0)
        {
            warlock.AddFeat(NwFeat.FromFeatId(1308));
            obj.Player.SendServerMessage(WarlockUtils.String(message: "Damage Reduction feat added."));
        }

        if (NWScript.GetLevelByClass(57, warlock) >= 8 && NWScript.GetHasFeat(1297, warlock) == 0)
        {
            warlock.AddFeat(NwFeat.FromFeatId(1297));
            obj.Player.SendServerMessage(WarlockUtils.String(message: "Otherworldly Resilience feat added."));
        }
    }

    private void RemoveIllegalFeats(OnLevelDown obj)
    {
        // Check if character has potential illegal feats, return if not
        if (!obj.Creature.IsPlayerControlled) return;
        if (!obj.Creature.Feats.Any(feat => feat.Id == 1297 || feat.Id >= 1307 && feat.Id <= 1319)) return;

        if (NWScript.GetLevelByClass(57, obj.Creature) < 1 && NWScript.GetHasFeat(1307) == 1)
        {
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1307));
            obj.Creature.LoginPlayer.SendServerMessage(
                WarlockUtils.String(message: "Armored Caster feat removed."));
        }

        if (NWScript.GetLevelByClass(57, obj.Creature) < 3 && NWScript.GetHasFeat(1308) == 1)
        {
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1308));
            obj.Creature.LoginPlayer.SendServerMessage(
                WarlockUtils.String(message: "Damage Reduction feat removed."));
        }

        if (NWScript.GetLevelByClass(57, obj.Creature) < 8 && NWScript.GetHasFeat(1297) == 0)
        {
            obj.Creature.AddFeat(NwFeat.FromFeatId(1297));
            obj.Creature.LoginPlayer.SendServerMessage(
                WarlockUtils.String(message: "Otherworldly Resilience feat removed."));
        }

        if (NWScript.GetLevelByClass(57, obj.Creature) < 10)
            foreach (Effect effect in obj.Creature.ActiveEffects)
            {
                if (effect.Tag == "warlock_resistfeat") obj.Creature.RemoveEffect(effect);
            }

        if (NWScript.GetLevelByClass(57, obj.Creature) < 20)
            foreach (Effect effect in obj.Creature.ActiveEffects)
            {
                if (effect.Tag == "warlock_epicresistfeat") obj.Creature.RemoveEffect(effect);
            }

        if (NWScript.GetLevelByClass(57, obj.Creature) <= 0
            && obj.Creature.Feats.Any(feat => feat.Id >= 1314 && feat.Id <= 1319))
        {
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1314));
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1315));
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1316));
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1317));
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1318));
            obj.Creature.RemoveFeat(NwFeat.FromFeatId(1319));
            obj.Creature.LoginPlayer.SendServerMessage(WarlockUtils.String(message: "Pact feat removed."));
        }
    }

    /* private async void PreventMultiples(ModuleEvents.OnPlayerLevelUp obj)
    {
        if (obj.Player.LoginCreature.Feats.Count(feat => feat.Id >= 1314 && feat.Id <= 1319) > 1)
        {
            int xpDelevel = NwEffects.GetXPForLevel(obj.Player.LoginCreature.Level) - 1;
            int xpRelevel = obj.Player.LoginCreature.Xp;
            obj.Player.LoginCreature.Xp = xpDelevel;
            await NwTask.Delay(TimeSpan.FromSeconds(1));
            obj.Player.LoginCreature.Xp = xpRelevel;
            obj.Player.SendServerMessage("You can only have one pact feat.");
        }
        if (obj.Player.LoginCreature.Feats.Count(feat => feat.Id >= 1309 && feat.Id <= 1313) > 2)
        {
            int xpDelevel = NwEffects.GetXPForLevel(obj.Player.LoginCreature.Level) - 1;
            int xpRelevel = obj.Player.LoginCreature.Xp;
            obj.Player.LoginCreature.Xp = xpDelevel;
            await NwTask.Delay(TimeSpan.FromSeconds(1));
            obj.Player.LoginCreature.Xp = xpRelevel;
            obj.Player.SendServerMessage("You can only have two energy resist feats.");
        }
    } */

    private void GivePactToken(ModuleEvents.OnClientEnter obj)
    {
        bool knowsPactFeat = obj.Player.LoginCreature.Feats.Any(feat => feat.Id >= 1314 && feat.Id <= 1319);
        bool isValidLvlWarlock = NWScript.GetLevelByClass(57, obj.Player.LoginCreature) > 1;
        bool hasToken = obj.Player.LoginCreature.Inventory.Items.Any(item => item.ResRef == "utilitytoken");
        bool hasInventorySpace =
            obj.Player.LoginCreature.Inventory.CheckFit(NwBaseItem.FromItemType(BaseItemType.Bullet));

        if (!isValidLvlWarlock) return;
        if (knowsPactFeat) return;
        if (hasToken) return;
        if (!hasInventorySpace) return;

        NwItem.Create(template: "utilitytoken", obj.Player.LoginCreature, 1, newTag: "utility_token_pactfeat");
        NwItem utilityToken =
            obj.Player.LoginCreature.Inventory.Items.First(item => item.Tag == "utility_token_pactfeat");
        utilityToken.Description = "Warlock: Use this token to get your pact sorted!";
        utilityToken.Name = "Warlock Pact Feat Token";
        obj.Player.SendServerMessage(
            WarlockUtils.String(message: "Warlock Pact Feat Token added to inventory, examine it!"));
    }

    private void GiveEnergyToken(ModuleEvents.OnClientEnter obj)
    {
        bool knowsEnergyFeat = obj.Player.LoginCreature.Feats.Any(feat => feat.Id >= 1309 && feat.Id <= 1313);
        bool isValidLvlWarlock = NWScript.GetLevelByClass(57, obj.Player.LoginCreature) > 10;
        bool hasToken = obj.Player.LoginCreature.Inventory.Items.Any(item => item.ResRef == "utilitytoken");
        bool hasInventorySpace =
            obj.Player.LoginCreature.Inventory.CheckFit(NwBaseItem.FromItemType(BaseItemType.Bullet));

        if (!isValidLvlWarlock) return;
        if (knowsEnergyFeat) return;
        if (hasToken) return;
        if (!hasInventorySpace) return;

        NwItem.Create(template: "utilitytoken", obj.Player.LoginCreature, 1, newTag: "utility_token_energyfeat");
        NwItem utilityToken =
            obj.Player.LoginCreature.Inventory.Items.First(item => item.Tag == "utility_token_energyfeat");
        utilityToken.Description = "Warlock: Use this token to get your energy feats sorted.";
        utilityToken.Name = "Warlock Energy Feat Token";
        obj.Player.SendServerMessage(
            WarlockUtils.String(message: "Warlock Energy Feat Token added to inventory, examine it!"));
    }

    private void GiveRelevelToken(ModuleEvents.OnClientEnter obj)
    {
        bool isValidLvlWarlock = NWScript.GetLevelByClass(57, obj.Player.LoginCreature) > 1;
        bool hasToken = obj.Player.LoginCreature.Inventory.Items.Any(item => item.ResRef == "utilitytoken");
        bool hasInventorySpace =
            obj.Player.LoginCreature.Inventory.CheckFit(NwBaseItem.FromItemType(BaseItemType.Bullet));

        NwCreature warlock = obj.Player.LoginCreature;

        bool knowsEssenceInvocation = warlock.HasSpellUse(NwSpell.FromSpellId(1015)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1016)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1017)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1018)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1019)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1020)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1021)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1022)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1023)) ||
                                      warlock.HasSpellUse(NwSpell.FromSpellId(1024));

        bool baseChaOver11 = NWScript.GetAbilityScore(warlock, NWScript.ABILITY_CHARISMA, 1) >= 11;

        if (!isValidLvlWarlock) return;
        if (hasToken) return;
        if (!hasInventorySpace) return;
        if (knowsEssenceInvocation) return;
        if (!baseChaOver11) return;

        NwItem.Create(template: "utilitytoken", obj.Player.LoginCreature, 1, newTag: "utility_token_warlockrelevel");
        NwItem utilityToken =
            obj.Player.LoginCreature.Inventory.Items.First(item => item.Tag == "utility_token_warlockrelevel");
        NWScript.SetLocalInt(utilityToken, sVarName: "warlockxp_releveltoken", obj.Player.LoginCreature.Xp);
        utilityToken.Description =
            "Warlock: Use this token to get your invocations sorted. Using the token relevels you to the level you were when you gained this token.";
        utilityToken.Name = "Warlock Relevel Token";
        obj.Player.SendServerMessage(
            WarlockUtils.String(message: "Warlock Relevel Token added to inventory, examine it!"));
    }

    private void TokenGivePactFeat(OnItemUse obj)
    {
        if (!obj.UsedBy.IsPlayerControlled) return;
        if (obj.Item.Tag != "utility_token_pactfeat") return;

        bool knowsPactFeat = obj.UsedBy.Feats.Any(feat => feat.Id >= 1314 && feat.Id <= 1319);

        if (knowsPactFeat)
        {
            obj.Item.Destroy();
            obj.UsedBy.ControllingPlayer.SendServerMessage(WarlockUtils.String(
                message: "You already have a pact feat! Relog to see if you qualify for other utility tokens."));
            obj.UsedBy.Location.ApplyEffect(EffectDuration.Instant,
                Effect.VisualEffect(VfxType.ImpElementalProtection));
            return;
        }

        int pactFeatInt = NWScript.GetLocalInt(obj.UsedBy, sVarName: "pactfeat_int");
        if (pactFeatInt == 0)
        {
            obj.UsedBy.ClearActionQueue();
            NWScript.AssignCommand(obj.UsedBy,
                () => NWScript.ActionStartConversation(obj.UsedBy, sDialogResRef: "pactfeat_select", 1, 0));
            return;
        }

        obj.UsedBy.AddFeat(NwFeat.FromFeatId(pactFeatInt));
        obj.Item.Destroy();
        string pactFeatName = NWScript.IntToString(pactFeatInt);
        switch (pactFeatName)
        {
            case "1314":
                pactFeatName = "Aberrant";
                break;
            case "1315":
                pactFeatName = "Celestial";
                break;
            case "1316":
                pactFeatName = "Fey";
                break;
            case "1317":
                pactFeatName = "Fiend";
                break;
            case "1318":
                pactFeatName = "Elemental";
                break;
            case "1319":
                pactFeatName = "Slaad";
                break;
        }

        obj.UsedBy.ControllingPlayer.SendServerMessage(WarlockUtils.String(pactFeatName +
                                                                           " Pact feat added. Relog to see if you qualify for other utility tokens."));
        obj.UsedBy.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpElementalProtection));
        NWScript.DeleteLocalInt(obj.UsedBy, sVarName: "pactfeat_int");
    }

    private void TokenGiveEnergyFeats(OnItemUse obj)
    {
        if (!obj.UsedBy.IsPlayerControlled) return;
        if (obj.Item.Tag != "utility_token_energyfeat") return;

        bool knowsEnergyFeats = obj.UsedBy.Feats.Count(feat => feat.Id >= 1309 && feat.Id <= 1313) >= 2;

        if (knowsEnergyFeats)
        {
            obj.Item.Destroy();
            obj.UsedBy.ControllingPlayer.SendServerMessage(WarlockUtils.String(
                message: "You already have energy feats! Relog to see if you qualify for other utility tokens."));
            obj.UsedBy.Location.ApplyEffect(EffectDuration.Instant,
                Effect.VisualEffect(VfxType.ImpElementalProtection));
            return;
        }

        int energyFeatInt1 = NWScript.GetLocalInt(obj.UsedBy, sVarName: "energyfeat_int1");
        int energyFeatInt2 = NWScript.GetLocalInt(obj.UsedBy, sVarName: "energyfeat_int2");
        if (energyFeatInt1 == 0 || energyFeatInt2 == 0)
        {
            obj.UsedBy.ClearActionQueue();
            NWScript.AssignCommand(obj.UsedBy,
                () => NWScript.ActionStartConversation(obj.UsedBy, sDialogResRef: "energyfeat_selec", 1, 0));
            return;
        }

        obj.UsedBy.AddFeat(NwFeat.FromFeatId(energyFeatInt1));
        obj.UsedBy.AddFeat(NwFeat.FromFeatId(energyFeatInt2));
        obj.Item.Destroy();
        string energyFeat1Name = NWScript.IntToString(energyFeatInt1);
        switch (energyFeat1Name)
        {
            case "1309":
                energyFeat1Name = "Acid";
                break;
            case "1310":
                energyFeat1Name = "Cold";
                break;
            case "1311":
                energyFeat1Name = "Electrical";
                break;
            case "1312":
                energyFeat1Name = "Fire";
                break;
            case "1313":
                energyFeat1Name = "Sonic";
                break;
        }

        string energyFeat2Name = NWScript.IntToString(energyFeatInt2);
        switch (energyFeat2Name)
        {
            case "1309":
                energyFeat2Name = "Acid";
                break;
            case "1310":
                energyFeat2Name = "Cold";
                break;
            case "1311":
                energyFeat2Name = "Electrical";
                break;
            case "1312":
                energyFeat2Name = "Fire";
                break;
            case "1313":
                energyFeat2Name = "Sonic";
                break;
        }

        obj.UsedBy.ControllingPlayer.SendServerMessage(WarlockUtils.String(energyFeat1Name + " and " +
                                                                           energyFeat2Name +
                                                                           " Energy feats added. Relog to see if you qualify for other utility tokens."));
        obj.UsedBy.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpElementalProtection));
        NWScript.DeleteLocalInt(obj.UsedBy, sVarName: "energyfeat_int1");
        NWScript.DeleteLocalInt(obj.UsedBy, sVarName: "energyfeat_int2");
    }

    private async void TokenRelevel(OnItemUse obj)
    {
        try
        {
            if (!obj.UsedBy.IsPlayerControlled) return;
            if (obj.Item.Tag != "utility_token_warlockrelevel") return;

            int level = obj.UsedBy.Level;

            for (int i = level; i > 1; i--)
            {
                CreatureLevelInfo levelInfo = obj.UsedBy.GetLevelStats(level);
                if (levelInfo.ClassInfo.Class.Id == 57)
                {
                    int xpDelevel = NwEffects.GetXpForLevel(level) - 1;
                    int xpRelevel = NWScript.GetLocalInt(obj.Item, sVarName: "warlockxp_releveltoken");
                    obj.Item.Destroy();
                    obj.UsedBy.Xp = xpDelevel;
                    await NwTask.Delay(TimeSpan.FromSeconds(1));
                    obj.UsedBy.Xp = xpRelevel;
                    obj.UsedBy.ControllingPlayer.SendServerMessage(WarlockUtils.String(
                        message:
                        "Releveled to the last warlock level. Relog to see if you qualify for other utility tokens."));
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in TokenRelevel");
        }
    }

    private void RemoveCustomShape(OnEffectRemove obj)
    {
        if (obj.Effect.Tag != "customshape_effect") return;
        NwCreature playerCharacter = (NwCreature)obj.Object;
        if (!playerCharacter.IsPlayerControlled) return;

        NwItem pcKey = playerCharacter.Inventory.Items.First(i => i.Tag == "ds_pckey");

        int originalGender = NWScript.GetLocalInt(pcKey, sVarName: "original_gender");
        int originalPheno = NWScript.GetLocalInt(pcKey, sVarName: "original_pheno");
        int originalAppearance = NWScript.GetLocalInt(pcKey, sVarName: "original_appearance");
        int originalSoundset = NWScript.GetLocalInt(pcKey, sVarName: "original_soundset");
        int originalPortrait = NWScript.GetLocalInt(pcKey, sVarName: "original_portrait");
        int originalTail = NWScript.GetLocalInt(pcKey, sVarName: "original_tail");
        int originalWings = NWScript.GetLocalInt(pcKey, sVarName: "original_wings");
        int originalRFoot = NWScript.GetLocalInt(pcKey, sVarName: "original_rfoot");
        int originalLFoot = NWScript.GetLocalInt(pcKey, sVarName: "original_lfoot");
        int originalRShin = NWScript.GetLocalInt(pcKey, sVarName: "original_rshin");
        int originalLShin = NWScript.GetLocalInt(pcKey, sVarName: "original_lshin");
        int originalRThigh = NWScript.GetLocalInt(pcKey, sVarName: "original_rthigh");
        int originalLThigh = NWScript.GetLocalInt(pcKey, sVarName: "original_lthigh");
        int originalPelvis = NWScript.GetLocalInt(pcKey, sVarName: "original_pelvis");
        int originalTorso = NWScript.GetLocalInt(pcKey, sVarName: "original_torso");
        int originalBelt = NWScript.GetLocalInt(pcKey, sVarName: "original_belt");
        int originalNeck = NWScript.GetLocalInt(pcKey, sVarName: "original_neck");
        int originalRFore = NWScript.GetLocalInt(pcKey, sVarName: "original_rfore");
        int originalLFore = NWScript.GetLocalInt(pcKey, sVarName: "original_lfore");
        int originalRBicep = NWScript.GetLocalInt(pcKey, sVarName: "original_rbicep");
        int originalLBicep = NWScript.GetLocalInt(pcKey, sVarName: "original_lbicep");
        int originalRShoulder = NWScript.GetLocalInt(pcKey, sVarName: "original_rshoulder");
        int originalLShoulder = NWScript.GetLocalInt(pcKey, sVarName: "original_lshoulder");
        int originalRHand = NWScript.GetLocalInt(pcKey, sVarName: "original_rhand");
        int originalLHand = NWScript.GetLocalInt(pcKey, sVarName: "original_lhand");
        int originalHead = NWScript.GetLocalInt(pcKey, sVarName: "original_head");
        int originalColorHair = NWScript.GetLocalInt(pcKey, sVarName: "original_colorhair");
        int originalColorSkin = NWScript.GetLocalInt(pcKey, sVarName: "original_colorSkin");
        int originalColorTattoo1 = NWScript.GetLocalInt(pcKey, sVarName: "original_colortattoo1");
        int originalColorTattoo2 = NWScript.GetLocalInt(pcKey, sVarName: "original_colortattoo2");
        float originalScale = NWScript.GetLocalFloat(pcKey, sVarName: "original_scale");

        NWScript.SetGender(playerCharacter, originalGender);
        NWScript.SetCreatureAppearanceType(playerCharacter, originalAppearance);
        NWScript.SetPhenoType(originalPheno, playerCharacter);
        NWScript.SetSoundset(playerCharacter, originalSoundset);
        NWScript.SetPortraitId(playerCharacter, originalPortrait);
        NWScript.SetCreatureTailType(originalTail, playerCharacter);
        NWScript.SetCreatureWingType(originalWings, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_FOOT, originalRFoot, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_FOOT, originalLFoot, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_SHIN, originalRShin, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_SHIN, originalLShin, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_THIGH, originalRThigh, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_THIGH, originalLThigh, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_PELVIS, originalPelvis, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_TORSO, originalTorso, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_BELT, originalBelt, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_NECK, originalNeck, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_FOREARM, originalRFore, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_FOREARM, originalLFore, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_BICEP, originalRBicep, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_BICEP, originalLBicep, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_SHOULDER, originalRShoulder, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_SHOULDER, originalLShoulder, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_RIGHT_HAND, originalRHand, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_LEFT_HAND, originalLHand, playerCharacter);
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, originalHead, playerCharacter);
        NWScript.SetColor(playerCharacter, NWScript.COLOR_CHANNEL_HAIR, originalColorHair);
        NWScript.SetColor(playerCharacter, NWScript.COLOR_CHANNEL_SKIN, originalColorSkin);
        NWScript.SetColor(playerCharacter, NWScript.COLOR_CHANNEL_TATTOO_1, originalColorTattoo1);
        NWScript.SetColor(playerCharacter, NWScript.COLOR_CHANNEL_TATTOO_2, originalColorTattoo2);
        NWScript.SetObjectVisualTransform(playerCharacter, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, originalScale);

        playerCharacter.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPolymorph));
    }
}
