using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockFeatHandler))]
public class WarlockFeatHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public WarlockFeatHandler()
    {
        NwModule.Instance.OnItemEquip += AddArmoredCasterOnEquip;
        NwModule.Instance.OnPlayerRest += AddArmoredCasterOnRest;
        NwModule.Instance.OnItemUnequip += RemoveArmoredCasterOnUnequip;
        NwModule.Instance.OnClientEnter += OnClientEnterApplyDR;
        NwModule.Instance.OnPlayerLevelUp += OnLevelUpApplyDR;
        NwModule.Instance.OnCreatureDamage += OnDamagedApplyResilience;
        NwModule.Instance.OnHeal += OnHealRemoveResilience;
        NwModule.Instance.OnPlayerLevelUp += OnLevelUpGiveEnergyResist;
        NwModule.Instance.OnClientEnter += OnLoginGiveEnerygyResist;
        Log.Info("Warlock Feat Handler initialized.");
    }

    private static ItemProperty _armoredCaster = ItemProperty.ArcaneSpellFailure(IPArcaneSpellFailure.Minus20Pct);

    private async void AddArmoredCasterOnEquip(OnItemEquip obj)
    {
        if (NWScript.GetLevelByClass(57, obj.EquippedBy) < NWScript.GetHitDice(obj.EquippedBy)/2) return;
        if (obj.Item.HasItemProperty(ItemPropertyType.ArcaneSpellFailure)) return;
        if (!obj.EquippedBy.IsPlayerControlled) return;

        NwItem item = obj.Item;
        bool isLightArmor = item.BaseACValue >= 1 && item.BaseACValue <= 3 && 
            item.BaseItem == NwBaseItem.FromItemType(BaseItemType.Armor);
        bool isSmallShield = item.BaseItem == NwBaseItem.FromItemType(BaseItemType.SmallShield);

        if(!(isLightArmor || isSmallShield)) return;

        IPArcaneSpellFailure ipAsfReduction = IPArcaneSpellFailure.Minus5Pct;
        
        if (isLightArmor)
            ipAsfReduction = item.BaseACValue switch
            {
                1 => IPArcaneSpellFailure.Minus5Pct,
                2 => IPArcaneSpellFailure.Minus10Pct,
                3 => IPArcaneSpellFailure.Minus20Pct,
                _ => IPArcaneSpellFailure.Minus5Pct
            };
        
        _armoredCaster = ItemProperty.ArcaneSpellFailure(ipAsfReduction);
        _armoredCaster.Tag = "armored_caster";

        await NwTask.Delay(TimeSpan.FromSeconds(0.1f));
        item.AddItemProperty(_armoredCaster, EffectDuration.Temporary, TimeSpan.FromHours(8));
    }

    private void AddArmoredCasterOnRest(ModuleEvents.OnPlayerRest obj)
    {
        if (obj.RestEventType != RestEventType.Finished) return;
        NwCreature? warlock = obj.Player.ControlledCreature;
        NwItem? armor = warlock.GetItemInSlot(InventorySlot.Chest);
        NwItem? shield = warlock.GetItemInSlot(InventorySlot.LeftHand);

        if (armor == null && shield == null) return;
        if (NWScript.GetLevelByClass(57, warlock) < NWScript.GetHitDice(warlock)/2) return;

        bool isLightArmor = armor.BaseACValue >= 1 && armor.BaseACValue <= 3;
        bool isSmallShield = shield.BaseItem == NwBaseItem.FromItemType(BaseItemType.SmallShield);

        if(isLightArmor && !armor.HasItemProperty(ItemPropertyType.ArcaneSpellFailure))
        {
            IPArcaneSpellFailure ipAsfReduction = armor.BaseACValue switch
            {
                1 => IPArcaneSpellFailure.Plus5Pct,
                2 => IPArcaneSpellFailure.Minus10Pct,
                3 => IPArcaneSpellFailure.Minus20Pct,
                _ => IPArcaneSpellFailure.Plus5Pct
            };

            _armoredCaster = ItemProperty.ArcaneSpellFailure(ipAsfReduction);
            _armoredCaster.Tag = "armored_caster";

            armor.AddItemProperty(_armoredCaster, EffectDuration.Temporary, TimeSpan.FromHours(8));
        }
        if (isSmallShield && !shield.HasItemProperty(ItemPropertyType.ArcaneSpellFailure))
        {
            _armoredCaster = ItemProperty.ArcaneSpellFailure(IPArcaneSpellFailure.Minus5Pct);
            _armoredCaster.Tag = "armored_caster";

            shield.AddItemProperty(_armoredCaster, EffectDuration.Temporary, TimeSpan.FromHours(8));
        }
    }

    private void RemoveArmoredCasterOnUnequip(OnItemUnequip obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Creature) <= 0) return;
        if (!obj.Creature.IsPlayerControlled) return;

        NwItem item = obj.Item;
        bool isLightArmor = item.BaseACValue >= 1 && item.BaseACValue <= 3;
        bool isSmallShield = item.BaseItem == NwBaseItem.FromItemType(BaseItemType.SmallShield);

        if(!(isLightArmor || isSmallShield)) return;

        foreach (ItemProperty itemProperty in item.ItemProperties)
        {
            if (itemProperty.Tag == "armored_caster")
            {
            item.RemoveItemProperty(itemProperty);
            }
        }
    }

    private void OnClientEnterApplyDR(ModuleEvents.OnClientEnter obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Player.ControlledCreature) < 3) return;

        NwCreature warlock = obj.Player.ControlledCreature;
        int warlockLevels = NWScript.GetLevelByClass(57, warlock);
        int power = warlockLevels switch
        {
            >= 3 and < 7 => 1,
            >= 7 and < 11 => 2,
            >= 11 and < 15 => 3,
            >= 15 and < 19 => 4,
            >= 19 and < 23 => 5,
            >= 23 and < 27 => 6,
            >= 27 => 7,
            _ => 0
        };

        foreach (Effect effect in warlock.ActiveEffects)
        {
            if (effect.Tag == "warlock_damagereduction")
            {
                warlock.RemoveEffect(effect);
            }
        }
        Effect damageReduction = Effect.DamageReduction(5, (DamagePower)power);
        damageReduction.SubType = EffectSubType.Unyielding;
        damageReduction.Tag = "warlock_damagereduction";
        warlock.ApplyEffect(EffectDuration.Permanent, damageReduction);
    }

    private void OnLevelUpApplyDR(ModuleEvents.OnPlayerLevelUp obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Player.ControlledCreature) < 3) return;

        NwCreature warlock = obj.Player.ControlledCreature;
        int warlockLevels = NWScript.GetLevelByClass(57, warlock);
        int power = warlockLevels switch
        {
            >= 3 and < 7 => 1,
            >= 7 and < 11 => 2,
            >= 11 and < 15 => 3,
            >= 15 and < 19 => 4,
            >= 19 and < 23 => 5,
            >= 23 and < 27 => 6,
            >= 27 => 7,
            _ => 0
        };

        foreach (Effect effect in warlock.ActiveEffects)
        {
            if (effect.Tag == "warlock_damagereduction") warlock.RemoveEffect(effect);
        }

        Effect damageReduction = Effect.DamageReduction(5, (DamagePower)power);
        damageReduction.SubType = EffectSubType.Unyielding;
        damageReduction.Tag = "warlock_damagereduction";
        warlock.ApplyEffect(EffectDuration.Permanent, damageReduction);
    }

    private void OnDamagedApplyResilience(OnCreatureDamage obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Target) < 8) return;

        NwCreature warlock = (NwCreature)obj.Target;

        int warlockLevels = NWScript.GetLevelByClass(57, warlock);
        int regenAmount = warlockLevels switch
        {
            >= 8 and < 13 => 1,
            >= 13 and < 18 => 2,
            >= 18 and < 23 => 3,
            >= 23 and < 28 => 4,
            >= 28 => 5,
            _ => 0
        };

        Effect otherworldlyResilience = Effect.Regenerate(regenAmount, TimeSpan.FromSeconds(6));
        otherworldlyResilience.SubType = EffectSubType.Unyielding;
        otherworldlyResilience.Tag = "otherworldly_resilience";

        foreach (Effect effect in warlock.ActiveEffects)
        {
            if (effect.Tag == "otherworldly_resilience") return;
        }

        if (warlock.HP < warlock.MaxHP * 0.5) warlock.ApplyEffect(EffectDuration.Permanent, otherworldlyResilience);
    }

    private void OnHealRemoveResilience(OnHeal obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Target) < 8) return;

        NwCreature warlock = (NwCreature)obj.Target;

        if (warlock.HP >= warlock.MaxHP * 0.5)
        {
            foreach (Effect activeEffect in warlock.ActiveEffects)
            {
                if (activeEffect.Tag == "otherworldly_resilience") warlock.RemoveEffect(activeEffect);
            }
        }
    }
    private void OnLevelUpGiveEnergyResist(ModuleEvents.OnPlayerLevelUp obj)
    {
        NwCreature warlock = obj.Player.ControlledCreature;
        int warlockLevels = NWScript.GetLevelByClass(57, warlock);
        if (warlockLevels < 10) return;
        if (warlock.ActiveEffects.Any(effect => effect.Tag == "warlock_epicresistfeat"))
            return;

        bool hasResistAcid = warlock.KnowsFeat(NwFeat.FromFeatId(1309));
        bool hasResistCold = warlock.KnowsFeat(NwFeat.FromFeatId(1310));
        bool hasResistElectric = warlock.KnowsFeat(NwFeat.FromFeatId(1311));
        bool hasResistFire = warlock.KnowsFeat(NwFeat.FromFeatId(1312));
        bool hasResistSonic = warlock.KnowsFeat(NwFeat.FromFeatId(1313));

        Effect resistFeat = Effect.BonusFeat(Feat.ResistEnergyAcid);

        if (hasResistAcid) 
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyAcid);
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
        }
        if (hasResistCold)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyCold);
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
        }
        if (hasResistElectric)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyElectrical);
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
        }
        if (hasResistFire)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyFire);
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
        }
        if (hasResistSonic)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergySonic);
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
        }

        if (warlockLevels >= 20)
        {
            Effect epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceAcid1);

            if (hasResistAcid)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceAcid1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistCold)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceCold1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistElectric)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceElectrical1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistFire)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceFire1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistSonic)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceSonic1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
        }
    }
    
    private void OnLoginGiveEnerygyResist(ModuleEvents.OnClientEnter obj)
    {
        if (!obj.Player.ControlledCreature.IsPlayerControlled) return;
        NwCreature warlock = obj.Player.LoginCreature;
        int warlockLevels = NWScript.GetLevelByClass(57, warlock);

        if (warlockLevels < 10) return;
        if (warlock.ActiveEffects.Count(effect => effect.Tag == "warlock_resistfeat") == 2 ||
            warlock.ActiveEffects.Count(effect => effect.Tag == "warlock_epicresistfeat") == 2)
            return;

        bool hasResistAcid = warlock.KnowsFeat(NwFeat.FromFeatId(1309));
        bool hasResistCold = warlock.KnowsFeat(NwFeat.FromFeatId(1310));
        bool hasResistElectric = warlock.KnowsFeat(NwFeat.FromFeatId(1311));
        bool hasResistFire = warlock.KnowsFeat(NwFeat.FromFeatId(1312));
        bool hasResistSonic = warlock.KnowsFeat(NwFeat.FromFeatId(1313));

        Effect resistFeat = Effect.BonusFeat(Feat.ResistEnergyAcid);

        if (hasResistAcid) 
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyAcid);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
        }
        if (hasResistCold)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyCold);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
        }
        if (hasResistElectric)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyElectrical);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
        }
        if (hasResistFire)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergyFire);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
        }
        if (hasResistSonic)
        {
            resistFeat = Effect.BonusFeat(Feat.ResistEnergySonic);
            resistFeat.SubType = EffectSubType.Unyielding;
            resistFeat.Tag = "warlock_resistfeat";
            warlock.ApplyEffect(EffectDuration.Permanent, resistFeat);
        }

        if (warlockLevels >= 20)
        {
            Effect epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceAcid1);

            if (hasResistAcid)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceAcid1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistCold)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceCold1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistElectric)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceElectrical1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistFire)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceFire1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
            if (hasResistSonic)
            {
                epicResistFeat = Effect.BonusFeat(Feat.EpicEnergyResistanceSonic1);
                epicResistFeat.SubType = EffectSubType.Unyielding;
                epicResistFeat.Tag = "warlock_epicresistfeat";
                warlock.ApplyEffect(EffectDuration.Permanent, epicResistFeat);
            }
        }
    }
}