using System.Collections.Concurrent;
using Anvil.API;
using Anvil.Services;
using Anvil.API.Events;
using NWN.Core;
using NLog;

namespace AmiaReforged.Classes.Services;

[ServiceBinding(typeof(WarlockPactSummonHandler))]
public class WarlockPactSummonHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockPactSummonHandler()
    {
        NwModule.Instance.OnClientLeave += UnsummonOnLeave;
        NwModule.Instance.OnAssociateAdd += UnsummonOnSummon;
        NwModule.Instance.OnPlayerDeath += UnsummonOnDeath;
        NwModule.Instance.OnPlayerRest += UnsummonOnRest;
        NwModule.Instance.OnAssociateRemove += UnsummonOnRemove;
        NwModule.Instance.OnAssociateAdd += OnSummonMakePretty;
        Log.Info("Warlock Pact Summon Handler initialized.");
    }

    private void UnsummonOnLeave(ModuleEvents.OnClientLeave obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Player.ControlledCreature) <= 0) return;
        NWScript.DeleteLocalInt(obj.Player.ControlledCreature, "wlk_summon_cd");
        if (obj.Player.ControlledCreature.GetAssociate(AssociateType.Henchman) == null) return;

        NwCreature warlock = obj.Player.ControlledCreature;

        foreach (NwCreature summon in warlock.Associates)
        {
            if (summon.ResRef == "wlkaberrant" || summon.ResRef == "wlkelemental" || summon.ResRef == "wlkfiend")
            {
                summon.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                summon.Destroy();
            }
        }
    }
    private void UnsummonOnSummon(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        if (obj.AssociateType == AssociateType.Dominated) return;

        if (obj.AssociateType == AssociateType.Summoned)
        {
            foreach (NwCreature summon in obj.Owner.Henchmen)
            {
                if (NWScript.GetLocalInt(summon, "wlk_unsummonable") == 1)
                {
                    NWScript.RemoveHenchman(obj.Owner, summon);
                }
            }
        }
        if (obj.Associate.ResRef == "wlkaberrant" || obj.Associate.ResRef == "wlkelemental" || obj.Associate.ResRef == "wlkfiend")
        {
            foreach (NwCreature summon in obj.Owner.Henchmen)
            {
                if (NWScript.GetLocalInt(summon, "wlk_unsummonable") == 1)
                {
                    NWScript.RemoveHenchman(obj.Owner, summon);
                }
            }
            foreach (NwCreature summon in obj.Owner.Associates)
            {
                if (summon.AssociateType == AssociateType.Summoned)
                {
                    summon.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                    obj.Owner.LoginPlayer.SendServerMessage("Unsummoning " + summon.Name + ".");
                    summon.Destroy();
                }
                return;
            }
        }
    }

    private void UnsummonOnDeath(ModuleEvents.OnPlayerDeath obj)
    {
        if (NWScript.GetLevelByClass(57, obj.DeadPlayer.ControlledCreature) <= 0) return;
        if (obj.DeadPlayer.ControlledCreature.GetAssociate(AssociateType.Henchman) == null) return;

        NwCreature warlock = obj.DeadPlayer.ControlledCreature;

        foreach (NwCreature summon in warlock.Associates)
        {
            if (summon.ResRef == "wlkaberrant" || summon.ResRef == "wlkelemental" || summon.ResRef == "wlkfiend")
            {
                NWScript.RemoveHenchman(warlock, summon);
            }
        }
    }
    private void UnsummonOnRest(ModuleEvents.OnPlayerRest obj)
    {
        if (obj.RestEventType != RestEventType.Started) return;
        NwCreature warlock = obj.Player.ControlledCreature;

        if (NWScript.GetLevelByClass(57, warlock) <= 0) return;
        if (warlock.GetAssociate(AssociateType.Henchman) == null) return;
        if (NWScript.GetLocalInt(warlock, "AR_RestChoice") == 0) return;

        foreach (NwCreature summon in warlock.Associates)
        {
            if (summon.ResRef == "wlkaberrant" || summon.ResRef == "wlkelemental" || summon.ResRef == "wlkfiend")
            {
                NWScript.RemoveHenchman(warlock, summon);
            }
        }
    }

    private void UnsummonOnRemove(OnAssociateRemove obj)
    {
        if (!obj.Owner.IsPlayerControlled) return;
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        if (obj.Associate.AssociateType != AssociateType.Henchman) return;

        bool isAberration = obj.Associate.ResRef == "wlkaberrant";
        bool isElemental = obj.Associate.ResRef == "wlkelemental";
        bool isFiend = obj.Associate.ResRef == "wlkfiend";
        bool isPactSummon = isAberration || isElemental || isFiend;

        if (!isPactSummon) return;

        NwCreature summon = obj.Associate;
        Effect desummonVfx = Effect.VisualEffect(VfxType.FnfSummonMonster1);
        if (isAberration) desummonVfx = Effect.VisualEffect(VfxType.ComChunkYellowMedium);
        if (isFiend) desummonVfx = Effect.VisualEffect(VfxType.ComChunkRedSmall);
        summon.Location.ApplyEffect(EffectDuration.Instant, desummonVfx);
        obj.Owner.LoginPlayer.SendServerMessage("Unsummoning "+summon.Name+".");
        summon.Destroy();
    }

    private void OnSummonMakePretty(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;

        if (obj.AssociateType == AssociateType.Henchman || obj.AssociateType == AssociateType.Summoned)
        {
            bool isAberration = obj.Associate.ResRef == "wlkaberrant";
            bool isCelestial = obj.Associate.ResRef == "wlkcelestial";
            bool isElemental = obj.Associate.ResRef == "wlkelemental";
            bool isFey = obj.Associate.ResRef == "wlkfey";
            bool isFiend = obj.Associate.ResRef == "wlkfiend";
            bool isSlaad = obj.Associate.ResRef == "wlkslaadred" || obj.Associate.ResRef == "wlkslaadblue" ||
                obj.Associate.ResRef == "wlkslaadgreen" || obj.Associate.ResRef == "wlkslaadgray";
            bool isPactSummon = isAberration || isCelestial || isElemental || isFey || isFiend || isSlaad;

            if (!isPactSummon) return;

            NwCreature summon = obj.Associate;

            int warlockLevels = NWScript.GetLevelByClass(57, obj.Owner);
            int summonTier = warlockLevels switch
            {
                >= 1 and < 5 => 1,
                >= 5 and < 10 => 2,
                >= 10 and < 15 => 3,
                >= 15 and < 20 => 4,
                >= 20 and < 25 => 5,
                >= 25 and < 30 => 6,
                >= 30 => 7,
                _ => 0
            };

            int summonCount = default;

            if (isAberration || isElemental)
            {
                summonCount = warlockLevels switch
                {
                    >= 1 and < 15 => 1,
                    >= 15 and < 30 => 2,
                    >= 30 => 3,
                    _ => 0
                };

                if (isAberration)
                {
                    Effect acidDamage = Effect.DamageIncrease(1, DamageType.Acid);
                    acidDamage.SubType = EffectSubType.Supernatural;
                    summon.ApplyEffect(EffectDuration.Permanent, acidDamage);
                }

                if (isElemental)
                {
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
                    Effect elementalEffect = Effect.LinkEffects(Effect.DamageIncrease(1, element), Effect.DamageImmunityIncrease(element, 100));
                    elementalEffect.SubType = EffectSubType.Supernatural;
                    summon.ApplyEffect(EffectDuration.Permanent, elementalEffect);
                }
            }

            if (isCelestial || isFey)
            {
                summonCount = 1;
                Effect concealment = Effect.Concealment(15 + summonTier * 5);

                Effect visual1 = default;
                Effect visual2 = default;

                if (isCelestial)
                {
                    visual1 = Effect.VisualEffect(VfxType.DurGhostSmoke2);
                    visual2 = Effect.VisualEffect(VfxType.DurLightWhite20);
                }
                if (isFey)
                {
                    visual1 = Effect.VisualEffect(VfxType.DurAuraBlueDark);
                    visual2 = Effect.VisualEffect(VfxType.DurInvisibility);
                }

                Effect ghostly = Effect.LinkEffects(visual1, visual2, concealment);
                ghostly.SubType = EffectSubType.Supernatural;
                summon.ApplyEffect(EffectDuration.Permanent, ghostly);
            }

            if (isSlaad)
            {
                summonCount = 1;
                summon.VisualTransform.Scale = 0.75f;
                int regen = default;
                if (summon.ResRef == "wlkslaadred") regen = 2;
                if (summon.ResRef == "wlkslaadblue") regen = 4;
                if (summon.ResRef == "wlkslaadgreen") regen = 6;
                if (summon.ResRef == "wlkslaadgray") regen = 8;
                Effect slaadEffects = Effect.LinkEffects(Effect.DamageResistance(DamageType.Acid, 5),
                Effect.DamageResistance(DamageType.Cold, 5), Effect.DamageResistance(DamageType.Electrical, 5),
                Effect.DamageResistance(DamageType.Fire, 5), Effect.DamageResistance(DamageType.Sonic, 5),
                Effect.Regenerate(regen, TimeSpan.FromSeconds(6)));
                slaadEffects.SubType = EffectSubType.Supernatural;
                summon.ApplyEffect(EffectDuration.Permanent, slaadEffects);
            }

            if (isFiend) summonCount = summonTier;

            for (int i = 1; i < warlockLevels; i++) summon.LevelUpHenchman(ClassType.Commoner, PackageType.Commoner);

            int ac = (summonTier - summonCount) * 5;
            int hp = 30 + (summonTier - summonCount) * 30;
            int apr = summonTier/summonCount/2;
            int saves = summonTier * 4;
            int skills = summonTier * 4;
            int strength = summonTier * 8;

            summon.RemoveFeat(Feat.Toughness);
            summon.RemoveFeat(Feat.Knockdown);
            summon.RemoveFeat(Feat.PowerAttack);
            summon.RemoveFeat(Feat.ImprovedPowerAttack);
            summon.RemoveFeat(Feat.Cleave);
            summon.RemoveFeat(Feat.GreatFortitude);
            summon.RemoveFeat(Feat.LightningReflexes);
            summon.RemoveFeat(Feat.IronWill);
            summon.RemoveFeat(Feat.SkillFocusConcentration);
            summon.RemoveFeat(Feat.SkillFocusHide);
            summon.RemoveFeat(Feat.SkillFocusMoveSilently);
            summon.RemoveFeat(Feat.Alertness);
            summon.MaxHP = hp;
            summon.HP = hp;
            summon.BaseAttackBonus = 1;
            summon.BaseAttackCount = apr;
            summon.BaseAC = (sbyte)ac;
            summon.Size = CreatureSize.Medium;
            summon.SetsRawAbilityScore(Ability.Strength, (byte)strength);
            summon.SetSkillRank(Skill.Discipline, (sbyte)skills);
            summon.SetBaseSavingThrow(SavingThrow.All, (sbyte)saves);
        }
    }
}