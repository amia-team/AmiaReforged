using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockSummonStatsHandler))]

public class WarlockSummonStatsHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockSummonStatsHandler()
    {
        NwModule.Instance.OnAssociateAdd += OnSummonAdjustAberration;
        NwModule.Instance.OnAssociateAdd += OnSummonAdjustCelestial;
        NwModule.Instance.OnAssociateAdd += OnSummonAdjustElemental;
        NwModule.Instance.OnAssociateAdd += OnSummonAdjustFey;
        NwModule.Instance.OnAssociateAdd += OnSummonAdjustFiend;
        NwModule.Instance.OnAssociateAdd += OnSummonAdjustSlaad;
        Log.Info("Warlock Summon Stats Handler initialized.");
    }

    private void OnSummonAdjustAberration(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        bool isAberration = obj.Associate.ResRef == "wlkaberrant";
        if (!isAberration) return;
        if (obj.AssociateType != AssociateType.Henchman) return;

        NwCreature summon = obj.Associate;

        int summonTier = SummonUtility.GetSummonTier(obj.Owner);
        Effect damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1, DamageType.BaseWeapon);

        switch(summonTier)
        {
            case 1:
                for (int i = 1; i < 1; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 30;
                summon.HP = 30;
                summon.BaseAC = 0;
                summon.SetSkillRank(Skill.Discipline, 4);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 4);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 4);
                summon.SetBaseSavingThrow(SavingThrow.Will, 4);
            break;
            
            case 2:
                for (int i = 1; i < 5; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 60;
                summon.HP = 60;
                summon.BaseAC = 6;
                summon.SetsRawAbilityScore(Ability.Strength, 12);
                summon.BaseAttackBonus = 3;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 8);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 8);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 8);
                summon.SetBaseSavingThrow(SavingThrow.Will, 8);
            break;

            case 3:
                for (int i = 1; i < 10; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 90;
                summon.HP = 90;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 14);
                summon.BaseAttackBonus = 6;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 12);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 12);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 12);
                summon.SetBaseSavingThrow(SavingThrow.Will, 12);
            break;

            case 4:
                for (int i = 1; i < 15; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 90;
                summon.HP = 90;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 16);
                summon.BaseAttackBonus = 9;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 16);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 16);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 16);
                summon.SetBaseSavingThrow(SavingThrow.Will, 16);
            break;

            case 5:
                for (int i = 1; i < 20; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 120;
                summon.HP = 120;
                summon.BaseAC = 18;
                summon.SetsRawAbilityScore(Ability.Strength, 18);
                summon.BaseAttackBonus = 12;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d8, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 20);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 20);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 20);
                summon.SetBaseSavingThrow(SavingThrow.Will, 20);
            break;

            case 6:
                for (int i = 1; i < 25; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 150;
                summon.HP = 150;
                summon.BaseAC = 24;
                summon.SetsRawAbilityScore(Ability.Strength, 20);
                summon.BaseAttackBonus = 15;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d10, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 24);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 24);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 24);
                summon.SetBaseSavingThrow(SavingThrow.Will, 24);
            break;

            case 7:
                for (int i = 1; i < 30; i++) summon.LevelUpHenchman(ClassType.Aberration, PackageType.Aberration);
                summon.MaxHP = 150;
                summon.HP = 150;
                summon.BaseAC = 24;
                summon.SetsRawAbilityScore(Ability.Strength, 22);
                summon.BaseAttackBonus = 18;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d12, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 28);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 28);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 28);
                summon.SetBaseSavingThrow(SavingThrow.Will, 28);
            break;
        }
        
        summon.BaseAttackCount = 1;
        summon.Size = CreatureSize.Medium;
        summon.MovementRate = MovementRate.Normal;

        Effect aberrationEffects = WarlockSummonConstants.aberrationEffects;
        aberrationEffects.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, aberrationEffects);

        damageIncrease.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, damageIncrease);
        
        foreach (NwFeat feat in summon.Feats) 
        {
            if (feat.Id == 289 || feat.Id == 226) continue;
            summon.RemoveFeat(feat);
        }
    }

    private void OnSummonAdjustCelestial(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        bool isCelestial = obj.Associate.ResRef == "wlkcelestial";
        if (!isCelestial) return;
        if (obj.AssociateType != AssociateType.Summoned) return;

        NwCreature summon = obj.Associate;

        int summonTier = SummonUtility.GetSummonTier(obj.Owner);
        Effect damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1, DamageType.BaseWeapon);
        int concealment = default;

        switch(summonTier)
        {
            case 1:
                for (int i = 1; i < 1; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 30;
                summon.HP = 30;
                summon.BaseAC = 0;
                summon.SetSkillRank(Skill.Discipline, 4);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 4);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 4);
                summon.SetBaseSavingThrow(SavingThrow.Will, 4);
                summon.BaseAttackCount = 1;
                concealment = 20;
            break;

            case 2:
                for (int i = 1; i < 5; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 60;
                summon.HP = 60;
                summon.BaseAC = 6;
                summon.SetsRawAbilityScore(Ability.Strength, 12);
                summon.BaseAttackBonus = 3;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 8);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 8);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 8);
                summon.SetBaseSavingThrow(SavingThrow.Will, 8);
                summon.BaseAttackCount = 1;
                concealment = 25;
            break;

            case 3:
                for (int i = 1; i < 10; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 90;
                summon.HP = 90;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 14);
                summon.BaseAttackBonus = 6;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 12);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 12);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 12);
                summon.SetBaseSavingThrow(SavingThrow.Will, 12);
                summon.BaseAttackCount = 1;
                concealment = 30;
            break;

            case 4:
                for (int i = 1; i < 15; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 120;
                summon.HP = 120;
                summon.BaseAC = 18;
                summon.SetsRawAbilityScore(Ability.Strength, 16);
                summon.BaseAttackBonus = 9;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 16);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 16);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 16);
                summon.SetBaseSavingThrow(SavingThrow.Will, 16);
                summon.BaseAttackCount = 2;
                concealment = 35;
            break;

            case 5:
                for (int i = 1; i < 20; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 150;
                summon.HP = 150;
                summon.BaseAC = 24;
                summon.SetsRawAbilityScore(Ability.Strength, 18);
                summon.BaseAttackBonus = 12;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d8, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 20);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 20);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 20);
                summon.SetBaseSavingThrow(SavingThrow.Will, 20);
                summon.BaseAttackCount = 2;
                concealment = 40;
            break;

            case 6:
                for (int i = 1; i < 25; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 180;
                summon.HP = 180;
                summon.BaseAC = 30;
                summon.SetsRawAbilityScore(Ability.Strength, 20);
                summon.BaseAttackBonus = 15;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d10, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 24);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 24);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 24);
                summon.SetBaseSavingThrow(SavingThrow.Will, 24);
                summon.BaseAttackCount = 3;
                concealment = 45;
            break;

            case 7:
                for (int i = 1; i < 30; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
                summon.MaxHP = 210;
                summon.HP = 210;
                summon.BaseAC = 36;
                summon.SetsRawAbilityScore(Ability.Strength, 22);
                summon.BaseAttackBonus = 18;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d12, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 28);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 28);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 28);
                summon.SetBaseSavingThrow(SavingThrow.Will, 28);
                summon.BaseAttackCount = 3;
                concealment = 50;
            break;
        }

        summon.Size = CreatureSize.Medium;

        Effect damage = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
        Effect celestialEffects = WarlockSummonConstants.CelestialEffects(concealment);
        celestialEffects.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, celestialEffects);
        summon.ApplyEffect(EffectDuration.Permanent, damage);

        damageIncrease.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, damageIncrease);
        
        foreach (NwFeat feat in summon.Feats) 
        {
            if (feat.Id == 289 || feat.Id == 226) continue;
            summon.RemoveFeat(feat);
        }
    }

    private void OnSummonAdjustElemental(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        bool isElemental = obj.Associate.ResRef == "wlkelemental";
        if (!isElemental) return;
        if (obj.AssociateType != AssociateType.Henchman) return;

        NwCreature summon = obj.Associate;

        int summonTier = SummonUtility.GetSummonTier(obj.Owner);
        Effect damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1, DamageType.BaseWeapon);

        switch(summonTier)
        {
            case 1:
                for (int i = 1; i < 1; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 30;
                summon.HP = 30;
                summon.BaseAC = 0;
                summon.SetSkillRank(Skill.Discipline, 4);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 4);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 4);
                summon.SetBaseSavingThrow(SavingThrow.Will, 4);
            break;
            
            case 2:
                for (int i = 1; i < 5; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 60;
                summon.HP = 60;
                summon.BaseAC = 6;
                summon.SetsRawAbilityScore(Ability.Strength, 12);
                summon.BaseAttackBonus = 3;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 8);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 8);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 8);
                summon.SetBaseSavingThrow(SavingThrow.Will, 8);
            break;

            case 3:
                for (int i = 1; i < 10; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 90;
                summon.HP = 90;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 14);
                summon.BaseAttackBonus = 6;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 12);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 12);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 12);
                summon.SetBaseSavingThrow(SavingThrow.Will, 12);
            break;

            case 4:
                for (int i = 1; i < 15; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 90;
                summon.HP = 90;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 16);
                summon.BaseAttackBonus = 9;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 16);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 16);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 16);
                summon.SetBaseSavingThrow(SavingThrow.Will, 16);
            break;

            case 5:
                for (int i = 1; i < 20; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 120;
                summon.HP = 120;
                summon.BaseAC = 18;
                summon.SetsRawAbilityScore(Ability.Strength, 18);
                summon.BaseAttackBonus = 12;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d8, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 20);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 20);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 20);
                summon.SetBaseSavingThrow(SavingThrow.Will, 20);
            break;

            case 6:
                for (int i = 1; i < 25; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 150;
                summon.HP = 150;
                summon.BaseAC = 24;
                summon.SetsRawAbilityScore(Ability.Strength, 20);
                summon.BaseAttackBonus = 15;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d10, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 24);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 24);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 24);
                summon.SetBaseSavingThrow(SavingThrow.Will, 24);
            break;

            case 7:
                for (int i = 1; i < 30; i++) summon.LevelUpHenchman(ClassType.Elemental, PackageType.Elemental);
                summon.MaxHP = 150;
                summon.HP = 150;
                summon.BaseAC = 24;
                summon.SetsRawAbilityScore(Ability.Strength, 22);
                summon.BaseAttackBonus = 18;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d12, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 28);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 28);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 28);
                summon.SetBaseSavingThrow(SavingThrow.Will, 28);
            break;
        }

        summon.BaseAttackCount = 1;
        summon.Size = CreatureSize.Medium;

        DamageType element = default;

        if (summon.Tag.Contains('1'))
        {
            element = DamageType.Fire;
            summon.Appearance = NwGameTables.AppearanceTable.GetRow(109);
            summon.Name = "Summoned Fire Mephit";
        }
        if (summon.Tag.Contains('2'))
        {
            element = DamageType.Cold;
            summon.Appearance = NwGameTables.AppearanceTable.GetRow(115);
            summon.PortraitResRef = "po_mepwater_";
            summon.Name = "Summoned Water Mephit";
        }
        if (summon.Tag.Contains('3'))
        {
            element = DamageType.Fire;
            Effect coldElement = Effect.LinkEffects(Effect.DamageIncrease(1, DamageType.Cold), Effect.DamageImmunityIncrease(DamageType.Cold, 100));
            coldElement.SubType = EffectSubType.Supernatural;
            summon.ApplyEffect(EffectDuration.Permanent, coldElement);
            summon.Appearance = NwGameTables.AppearanceTable.GetRow(113);
            summon.PortraitResRef = "po_mepsteam_";
            summon.Name = "Summoned Steam Mephit";
        }

        Effect elementalEffects = Effect.LinkEffects(Effect.DamageIncrease(1, element), Effect.DamageImmunityIncrease(element, 100));
        elementalEffects.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, elementalEffects);
        
        damageIncrease.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, damageIncrease);
        
        foreach (NwFeat feat in summon.Feats) 
        {
            if (feat.Id == 289 || feat.Id == 226) continue;
            summon.RemoveFeat(feat);
        }

    }

    private void OnSummonAdjustFey(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        bool isFey = obj.Associate.ResRef == "wlkfey";
        if (!isFey) return;
        if (obj.AssociateType != AssociateType.Summoned) return;

        NwCreature summon = obj.Associate;

        int summonTier = SummonUtility.GetSummonTier(obj.Owner);
        Effect damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1, DamageType.BaseWeapon);
        int concealment = default;

        switch(summonTier)
        {
            case 1:
                for (int i = 1; i < 1; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 30;
                summon.HP = 30;
                summon.BaseAC = 0;
                summon.SetSkillRank(Skill.Discipline, 4);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 4);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 4);
                summon.SetBaseSavingThrow(SavingThrow.Will, 4);
                summon.BaseAttackCount = 1;
                concealment = 20;
            break;

            case 2:
                for (int i = 1; i < 5; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 60;
                summon.HP = 60;
                summon.BaseAC = 6;
                summon.SetsRawAbilityScore(Ability.Strength, 12);
                summon.BaseAttackBonus = 3;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 8);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 8);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 8);
                summon.SetBaseSavingThrow(SavingThrow.Will, 8);
                summon.BaseAttackCount = 1;
                concealment = 25;            
            break;

            case 3:
                for (int i = 1; i < 10; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 90;
                summon.HP = 90;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 14);
                summon.BaseAttackBonus = 6;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 12);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 12);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 12);
                summon.SetBaseSavingThrow(SavingThrow.Will, 12);
                summon.BaseAttackCount = 1;
                concealment = 30;
            break;

            case 4:
                for (int i = 1; i < 15; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 120;
                summon.HP = 120;
                summon.BaseAC = 18;
                summon.SetsRawAbilityScore(Ability.Strength, 16);
                summon.BaseAttackBonus = 9;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 16);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 16);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 16);
                summon.SetBaseSavingThrow(SavingThrow.Will, 16);
                summon.BaseAttackCount = 2;
                concealment = 35;
            break;

            case 5:
                for (int i = 1; i < 20; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 150;
                summon.HP = 150;
                summon.BaseAC = 24;
                summon.SetsRawAbilityScore(Ability.Strength, 18);
                summon.BaseAttackBonus = 12;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d8, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 20);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 20);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 20);
                summon.SetBaseSavingThrow(SavingThrow.Will, 20);
                summon.BaseAttackCount = 2;
                concealment = 40;
            break;

            case 6:
                for (int i = 1; i < 25; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 180;
                summon.HP = 180;
                summon.BaseAC = 30;
                summon.SetsRawAbilityScore(Ability.Strength, 20);
                summon.BaseAttackBonus = 15;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d10, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 24);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 24);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 24);
                summon.SetBaseSavingThrow(SavingThrow.Will, 24);
                summon.BaseAttackCount = 3;
                concealment = 45;
            break;

            case 7:
                for (int i = 1; i < 30; i++) summon.LevelUpHenchman(ClassType.Fey, PackageType.Fey);
                summon.MaxHP = 210;
                summon.HP = 210;
                summon.BaseAC = 36;
                summon.SetsRawAbilityScore(Ability.Strength, 22);
                summon.BaseAttackBonus = 18;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d12, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 28);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 28);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 28);
                summon.SetBaseSavingThrow(SavingThrow.Will, 28);
                summon.BaseAttackCount = 3;
                concealment = 50;
            break;
        }
        
        summon.Size = CreatureSize.Medium;

        Effect feyEffects = WarlockSummonConstants.FeyEffects(concealment);
        feyEffects.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, feyEffects);

        damageIncrease.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, damageIncrease);
        
        foreach (NwFeat feat in summon.Feats) 
        {
            if (feat.Id == 289 || feat.Id == 226) continue;
            summon.RemoveFeat(feat);
        }
    }

    private void OnSummonAdjustFiend(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        bool isFiend = obj.Associate.ResRef == "wlkfiend";
        if (!isFiend) return;
        if (obj.AssociateType != AssociateType.Henchman) return;

        NwCreature summon = obj.Associate;

        int summonTier = SummonUtility.GetSummonTier(obj.Owner);
        Effect damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1, DamageType.BaseWeapon);

        switch(summonTier)
        {
            case 1:
                for (int i = 1; i < 1; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 30;
                summon.HP = 30;
                summon.BaseAC = 0;
                summon.SetSkillRank(Skill.Discipline, 4);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 4);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 4);
                summon.SetBaseSavingThrow(SavingThrow.Will, 4);
            break;

            case 2:
                for (int i = 1; i < 5; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 35;
                summon.HP = 35;
                summon.BaseAC = 2;
                summon.SetsRawAbilityScore(Ability.Strength, 12);
                summon.BaseAttackBonus = 3;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 8);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 8);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 8);
                summon.SetBaseSavingThrow(SavingThrow.Will, 8);
            break;

            case 3:
                for (int i = 1; i < 10; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 40;
                summon.HP = 40;
                summon.BaseAC = 4;
                summon.SetsRawAbilityScore(Ability.Strength, 14);
                summon.BaseAttackBonus = 6;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 12);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 12);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 12);
                summon.SetBaseSavingThrow(SavingThrow.Will, 12);
            break;

            case 4:
                for (int i = 1; i < 15; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 45;
                summon.HP = 45;
                summon.BaseAC = 6;
                summon.SetsRawAbilityScore(Ability.Strength, 16);
                summon.BaseAttackBonus = 9;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d6, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 16);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 16);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 16);
                summon.SetBaseSavingThrow(SavingThrow.Will, 16);
            break;

            case 5:
                for (int i = 1; i < 20; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 50;
                summon.HP = 50;
                summon.BaseAC = 8;
                summon.SetsRawAbilityScore(Ability.Strength, 18);
                summon.BaseAttackBonus = 12;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d8, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 20);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 20);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 20);
                summon.SetBaseSavingThrow(SavingThrow.Will, 20);
            break;

            case 6:
                for (int i = 1; i < 25; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 55;
                summon.HP = 55;
                summon.BaseAC = 10;
                summon.SetsRawAbilityScore(Ability.Strength, 20);
                summon.BaseAttackBonus = 15;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d10, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 24);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 24);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 24);
                summon.SetBaseSavingThrow(SavingThrow.Will, 24);
            break;

            case 7:
                for (int i = 1; i < 30; i++) summon.LevelUpHenchman(ClassType.Vermin, PackageType.Vermin);
                summon.MaxHP = 60;
                summon.HP = 60;
                summon.BaseAC = 12;
                summon.SetsRawAbilityScore(Ability.Strength, 22);
                summon.BaseAttackBonus = 18;
                damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d12, DamageType.BaseWeapon);
                summon.SetSkillRank(Skill.Discipline, 28);
                summon.SetBaseSavingThrow(SavingThrow.Fortitude, 28);
                summon.SetBaseSavingThrow(SavingThrow.Reflex, 28);
                summon.SetBaseSavingThrow(SavingThrow.Will, 28);
            break;
        }

        summon.BaseAttackCount = 1;
        summon.Size = CreatureSize.Medium;

        Effect mindImmunity = Effect.Immunity(ImmunityType.MindSpells);
        mindImmunity.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, mindImmunity);

        damageIncrease.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, damageIncrease);
        
        foreach (NwFeat feat in summon.Feats) 
        {
            if (feat.Id == 289 || feat.Id == 226) continue;
            summon.RemoveFeat(feat);
        }
    }

    private void OnSummonAdjustSlaad(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        bool isSlaad = obj.Associate.ResRef == "wlkslaadred" || obj.Associate.ResRef == "wlkslaadblue" || obj.Associate.ResRef == "wlkslaadgreen" 
            || obj.Associate.ResRef == "wlkslaadgray";
        if (!isSlaad) return;
        if (obj.AssociateType != AssociateType.Summoned) return;

        NwCreature summon = obj.Associate;
        NwCreature warlock = obj.Owner;
        
        int summonTier = SummonUtility.GetSummonTier(obj.Owner);
        Effect damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1, DamageType.BaseWeapon);
        string slaadTier = summon.ResRef;

        if (summonTier == 1 && slaadTier == "wlkslaadred")
        {
            for (int i = 1; i < 1; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 30;
            summon.HP = 30;
            summon.BaseAC = 0;
            summon.SetSkillRank(Skill.Discipline, 4);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 4);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 4);
            summon.SetBaseSavingThrow(SavingThrow.Will, 4);
            summon.BaseAttackCount = 1;
            summon.VisualTransform.Scale = 0.7f;
        }
        if (summonTier >= 2 && slaadTier == "wlkslaadred")
        {
            for (int i = 1; i < 5; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 60;
            summon.HP = 60;
            summon.BaseAC = 6;
            summon.SetsRawAbilityScore(Ability.Strength, 12);
            summon.BaseAttackBonus = 3;
            damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d4, DamageType.BaseWeapon);
            summon.SetSkillRank(Skill.Discipline, 8);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 8);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 8);
            summon.SetBaseSavingThrow(SavingThrow.Will, 8);
            summon.BaseAttackCount = 1;
            summon.VisualTransform.Scale = 0.7f;
        }
        if (summonTier == 3 && slaadTier == "wlkslaadblue")
        {
            for (int i = 1; i < 10; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 90;
            summon.HP = 90;
            summon.BaseAC = 12;
            summon.SetsRawAbilityScore(Ability.Strength, 14);
            summon.BaseAttackBonus = 6;
            damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus1d6, DamageType.BaseWeapon);
            summon.SetSkillRank(Skill.Discipline, 12);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 12);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 12);
            summon.SetBaseSavingThrow(SavingThrow.Will, 12);
            summon.BaseAttackCount = 1;
            summon.VisualTransform.Scale = 0.8f;
        }
        if (summonTier >= 4 && slaadTier == "wlkslaadblue")
        {
            for (int i = 1; i < 15; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 120;
            summon.HP = 120;
            summon.BaseAC = 18;
            summon.SetsRawAbilityScore(Ability.Strength, 16);
            summon.BaseAttackBonus = 9;
            damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d6, DamageType.BaseWeapon);
            summon.SetSkillRank(Skill.Discipline, 16);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 16);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 16);
            summon.SetBaseSavingThrow(SavingThrow.Will, 16);
            summon.BaseAttackCount = 2;
            summon.VisualTransform.Scale = 0.8f;
        }
        if (summonTier == 5 && slaadTier == "wlkslaadgreen")
        {
            for (int i = 1; i < 20; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 150;
            summon.HP = 150;
            summon.BaseAC = 24;
            summon.SetsRawAbilityScore(Ability.Strength, 18);
            summon.BaseAttackBonus = 12;
            damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d8, DamageType.BaseWeapon);
            summon.SetSkillRank(Skill.Discipline, 20);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 20);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 20);
            summon.SetBaseSavingThrow(SavingThrow.Will, 20);
            summon.BaseAttackCount = 2;
            summon.VisualTransform.Scale = 0.9f;
        }
        if (summonTier >= 6 && slaadTier == "wlkslaadgreen")
        {
            for (int i = 1; i < 25; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 180;
            summon.HP = 180;
            summon.BaseAC = 30;
            summon.SetsRawAbilityScore(Ability.Strength, 20);
            summon.BaseAttackBonus = 15;
            damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d10, DamageType.BaseWeapon);
            summon.SetSkillRank(Skill.Discipline, 24);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 24);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 24);
            summon.SetBaseSavingThrow(SavingThrow.Will, 24);
            summon.BaseAttackCount = 3;
            summon.VisualTransform.Scale = 0.9f;
        }
        if (summonTier == 7 && slaadTier == "wlkslaadgray")
        {
            for (int i = 1; i < 30; i++) summon.LevelUpHenchman(ClassType.Outsider, PackageType.Outsider);
            summon.MaxHP = 210;
            summon.HP = 210;
            summon.BaseAC = 36;
            summon.SetsRawAbilityScore(Ability.Strength, 22);
            summon.BaseAttackBonus = 18;
            damageIncrease = Effect.DamageIncrease((int)DamageBonus.Plus2d12, DamageType.BaseWeapon);
            summon.SetSkillRank(Skill.Discipline, 28);
            summon.SetBaseSavingThrow(SavingThrow.Fortitude, 28);
            summon.SetBaseSavingThrow(SavingThrow.Reflex, 28);
            summon.SetBaseSavingThrow(SavingThrow.Will, 28);
            summon.BaseAttackCount = 3;
        }
        
        summon.Size = CreatureSize.Medium;
        
        int regen = slaadTier switch
        {
            "wlkslaadred" => 2,
            "wlkslaadblue" => 4,
            "wlkslaadgreen" => 6,
            "wlkslaadgray" => 8,
            _ => 2
        };

        Effect slaadEffects = WarlockSummonConstants.SlaadEffects(regen); 
        slaadEffects.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, slaadEffects);

        damageIncrease.SubType = EffectSubType.Supernatural;
        summon.ApplyEffect(EffectDuration.Permanent, damageIncrease);
        
        foreach (NwFeat feat in summon.Feats) 
        {
            if (feat.Id == 289 || feat.Id == 226) continue;
            summon.RemoveFeat(feat);
        }

        // The duration effect is used to determine the remaining duration of the slaad summon 
        // for subsequent slaadi spawning in WarlockSummonUtilHandler => OnFrogDeathRussianDoll
        foreach (Effect effect in warlock.ActiveEffects)
        {
            if(effect.Tag == "frogduration")
            {
                if (effect.DurationRemaining != 
                    NWScript.RoundsToSeconds(SummonUtility.PactSummonDuration(warlock))) return;
            }
        }
        Effect durationEffect = Effect.VisualEffect(VfxType.None);
        durationEffect.SubType = EffectSubType.Supernatural;
        durationEffect.Tag = "frogduration";
        warlock.ApplyEffect(EffectDuration.Temporary, durationEffect, NwTimeSpan.FromRounds(SummonUtility.PactSummonDuration(warlock)));
    }
}