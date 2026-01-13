using AmiaReforged.PwEngine.Features.Crafting.ItemProperties;
using AmiaReforged.PwEngine.Features.Crafting.Models;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting;

[ServiceBinding(typeof(CraftingPropertyData))]
public class CraftingPropertyData
{
    public const int CasterWeapon1H = 9998;
    public const int CasterWeapon2H = 9999;

    public CraftingPropertyData()
    {
        SetupAmulets();
        SetupGloves();
        Setup1HMeleeWeapons();
        Setup2HMeleeWeapons();
        SetupEquippedItems();
        SetupMagicStaves();
        SetupThrownWeapons();
        SetupRangedWeapons();
        SetupAmmo();
    }

    public Dictionary<int, IReadOnlyList<CraftingCategory>> Properties { get; } = new();

    private void SetupEquippedItems()
    {
        foreach (int item in ItemTypeConstants.EquippableItems())
        {
            List<CraftingCategory> properties = [];

            AddEquippedItemProperties(properties);

            Properties.TryAdd(item, properties);
        }
    }

    private void SetupMagicStaves()
    {
        List<CraftingCategory> properties = [];

        AddBaseEquippedItemProperties(properties);

        // Caster weapons use the 1-cost bonus spell slots
        properties.Add(BonusSpellSlotProperties.AssassinBonusSpells);
        properties.Add(BonusSpellSlotProperties.BardBonusSpells);
        properties.Add(BonusSpellSlotProperties.BlackguardBonusSpells);
        properties.Add(BonusSpellSlotProperties.ClericBonusSpells);
        properties.Add(BonusSpellSlotProperties.DruidBonusSpells);
        properties.Add(BonusSpellSlotProperties.PaladinBonusSpells);
        properties.Add(BonusSpellSlotProperties.RangerBonusSpells);
        properties.Add(BonusSpellSlotProperties.SorcererBonusSpells);
        properties.Add(BonusSpellSlotProperties.WizardBonusSpells);

        properties.Add(SpellResistanceProperties.SpellResistances);

        Properties.TryAdd(CasterWeapon1H, properties);
        Properties.TryAdd(CasterWeapon2H, properties);
    }

    private void SetupAmulets()
    {
        // This list of properties is different because natural armor has its own unique costs.
        List<CraftingCategory> properties =
        [
            new(categoryId: "natural_armor")
            {
                Label = "Armor",
                Properties =
                [
                    new CraftingProperty
                    {
                        PowerCost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(1)!,
                        GuiLabel = "+1 AC",
                        CraftingTier = CraftingTier.Minor,
                        GoldCost = GenericItemProperties.AcCost1
                    },
                    new CraftingProperty
                    {
                        PowerCost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(2)!,
                        GuiLabel = "+2 AC",
                        CraftingTier = CraftingTier.Lesser,
                        GoldCost = GenericItemProperties.AcCost2
                    },
                    new CraftingProperty
                    {
                        PowerCost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(3)!,
                        GuiLabel = "+3 AC",
                        CraftingTier = CraftingTier.Intermediate,
                        GoldCost = GenericItemProperties.AcCost3
                    },
                    new CraftingProperty
                    {
                        PowerCost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(4)!,
                        GuiLabel = "+4 AC",
                        CraftingTier = CraftingTier.Greater,
                        GoldCost = GenericItemProperties.AcCost4
                    },
                    new CraftingProperty
                    {
                        PowerCost = 2,
                        ItemProperty = NWScript.ItemPropertyACBonus(5)!,
                        GuiLabel = "+5 AC",
                        CraftingTier = CraftingTier.Flawless,
                        GoldCost = GenericItemProperties.AcCost5
                    }
                ]
            },

            GenericItemProperties.ElementalResistances,
            GenericItemProperties.DamageReductions,
            GenericItemProperties.PhysicalDamageResistances,
            GenericItemProperties.Regeneration,
            SavingThrowProperties.SpecificSaves,
            SavingThrowProperties.GeneralSaves,
            SavingThrowProperties.UniversalSaves,
            SkillProperties.Personal,
            SkillProperties.Advantageous,
            AbilityProperties.Abilities,
            CastSpellProperties.FluffSpells,
            CastSpellProperties.BeneficialSpells,
            CastSpellProperties.DMSpellCasting,

            SpecialItemProperties.LightProperties,
            SpecialItemProperties.AdditionalProperties,
            SpecialItemProperties.AlignmentProperties,
            SpecialItemProperties.UseLimitationAlignmentGroup,
            SpecialItemProperties.UseLimitationClass,
            SpecialItemProperties.UseLimitationRace,
            SpecialItemProperties.QualityProperties,

            // Wondrous DM-only properties
            WondrousDmProperties.ArcaneSpellFailureReduction,
            WondrousDmProperties.SneakAttackFeats,
            WondrousDmProperties.ImmunityMiscellaneous,
            WondrousDmProperties.BonusFeats,
            WondrousDmProperties.SpellResistanceWondrous,
            WondrousDmProperties.WeightReduction,

            // Bonus spell slots
            BonusSpellSlotProperties.AssassinBonusSpellsCostly,
            BonusSpellSlotProperties.BardBonusSpellsCostly,
            BonusSpellSlotProperties.BlackguardBonusSpellsCostly,
            BonusSpellSlotProperties.ClericBonusSpellsCostly,
            BonusSpellSlotProperties.DruidBonusSpellsCostly,
            BonusSpellSlotProperties.PaladinBonusSpellsCostly,
            BonusSpellSlotProperties.RangerBonusSpellsCostly,
            BonusSpellSlotProperties.SorcererBonusSpellsCostly,
            BonusSpellSlotProperties.WizardBonusSpellsCostly,

            SpellResistanceProperties.SpellResistances

        ];


        Properties.TryAdd(NWScript.BASE_ITEM_AMULET, properties);
    }

    private void SetupGloves()
    {
        List<CraftingCategory> properties = [];

        AddEquippedItemProperties(properties);

        // These are also weapons...
        properties.Add(DamageProperties.GloveDamage);
        properties.Add(DamageProperties.MassiveCriticals);
        properties.Add(MeleeOnHitProperties.OnHits);

        // Gauntlets have another tier of massive criticals.
        properties.Add(new CraftingCategory(categoryId: "massive_criticals")
        {
            Label = "Massive Criticals",
            Properties =
            [
                new CraftingProperty
                {
                    PowerCost = 1,
                    ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                    GuiLabel = "2d6 Massive Criticals",
                    CraftingTier = CraftingTier.Flawless,
                    GoldCost = 20000
                }
            ]
        });

        properties.Add(GenericItemProperties.VampiricRegeneration);
        properties.Add(GenericItemProperties.Keen);

        Properties.TryAdd(NWScript.BASE_ITEM_GLOVES, properties);
    }

    /// <summary>
    /// Adds base equipped item properties without bonus spell slots.
    /// Used by both regular equipped items and caster weapons.
    /// </summary>
    private static void AddBaseEquippedItemProperties(List<CraftingCategory> properties)
    {
        properties.Add(GenericItemProperties.Armor);
        properties.Add(GenericItemProperties.ElementalResistances);
        properties.Add(GenericItemProperties.PhysicalDamageResistances);
        properties.Add(GenericItemProperties.DamageReductions);
        properties.Add(GenericItemProperties.Regeneration);

        properties.Add(SavingThrowProperties.SpecificSaves);
        properties.Add(SavingThrowProperties.GeneralSaves);
        properties.Add(SavingThrowProperties.UniversalSaves);

        properties.Add(SkillProperties.Personal);
        properties.Add(SkillProperties.Advantageous);

        properties.Add(AbilityProperties.Abilities);

        properties.Add(CastSpellProperties.FluffSpells);
        properties.Add(CastSpellProperties.BeneficialSpells);
        properties.Add(CastSpellProperties.DMSpellCasting);

        properties.Add(SpecialItemProperties.LightProperties);
        properties.Add(SpecialItemProperties.AdditionalProperties);
        properties.Add(SpecialItemProperties.AlignmentProperties);
        properties.Add(SpecialItemProperties.UseLimitationAlignmentGroup);
        properties.Add(SpecialItemProperties.UseLimitationClass);
        properties.Add(SpecialItemProperties.UseLimitationRace);
        properties.Add(SpecialItemProperties.QualityProperties);

        // Wondrous DM-only properties
        properties.Add(WondrousDmProperties.ArcaneSpellFailureReduction);
        properties.Add(WondrousDmProperties.SneakAttackFeats);
        properties.Add(WondrousDmProperties.ImmunityMiscellaneous);
        properties.Add(WondrousDmProperties.BonusFeats);
        properties.Add(WondrousDmProperties.SpellResistanceWondrous);
        properties.Add(WondrousDmProperties.WeightReduction);
    }

    /// <summary>
    /// Adds equipped item properties with bonus spell slots.
    /// Used for armor, accessories, and other non-caster-weapon items.
    /// </summary>
    private static void AddEquippedItemProperties(List<CraftingCategory> properties)
    {
        AddBaseEquippedItemProperties(properties);

        // Non-caster items use the bonus spell slots
        properties.Add(BonusSpellSlotProperties.AssassinBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.BardBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.BlackguardBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.ClericBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.DruidBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.PaladinBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.RangerBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.SorcererBonusSpellsCostly);
        properties.Add(BonusSpellSlotProperties.WizardBonusSpellsCostly);

        properties.Add(SpellResistanceProperties.SpellResistances);
    }

    private void Setup1HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.MeleeWeapons())
        {
            List<CraftingCategory> properties = [DamageProperties.OneHanders];

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private void Setup2HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.Melee2HWeapons())
        {
            List<CraftingCategory> properties = [DamageProperties.TwoHanders];

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private static void AddSharedWeaponProperties(List<CraftingCategory> properties)
    {
        properties.Add(DamageProperties.MassiveCriticals);

        properties.Add(AttackBonusProperties.AttackBonus);
        properties.Add(AttackBonusProperties.EnhancementBonus);

        properties.Add(GenericItemProperties.VampiricRegeneration);
        properties.Add(GenericItemProperties.Armor);
        properties.Add(GenericItemProperties.Regeneration);
        properties.Add(GenericItemProperties.Keen);

        properties.Add(SkillProperties.Personal);
        properties.Add(SkillProperties.Advantageous);

        properties.Add(AbilityProperties.Abilities);

        properties.Add(MeleeOnHitProperties.OnHits);

        properties.Add(VisualEffectConstants.VisualEffects);

        // Bonus spell slots (1-point cost) for all melee/thrown weapons
        properties.Add(BonusSpellSlotProperties.AssassinBonusSpells);
        properties.Add(BonusSpellSlotProperties.BardBonusSpells);
        properties.Add(BonusSpellSlotProperties.BlackguardBonusSpells);
        properties.Add(BonusSpellSlotProperties.ClericBonusSpells);
        properties.Add(BonusSpellSlotProperties.DruidBonusSpells);
        properties.Add(BonusSpellSlotProperties.PaladinBonusSpells);
        properties.Add(BonusSpellSlotProperties.RangerBonusSpells);
        properties.Add(BonusSpellSlotProperties.SorcererBonusSpells);
        properties.Add(BonusSpellSlotProperties.WizardBonusSpells);
    }

    private void SetupThrownWeapons()
    {
        foreach (int weapon in ItemTypeConstants.ThrownWeapons())
        {
            List<CraftingCategory> properties =
            [
                DamageProperties.OneHanders,
                GenericItemProperties.KeenThrown
            ];

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private void SetupRangedWeapons()
    {
        foreach (int weapon in ItemTypeConstants.RangedWeapons())
        {
            List<CraftingCategory> properties =
            [
                AttackBonusProperties.AttackBonus,
                DamageProperties.Mighty,
                DamageProperties.MassiveCriticalsRanged,
                DamageProperties.UnlimitedAmmo,
                SkillProperties.Advantageous,
                SkillProperties.Personal,
                GenericItemProperties.Regeneration,
                GenericItemProperties.Keen,
                AbilityProperties.Abilities,
            ];

            Properties.TryAdd(weapon, properties);
        }
    }

    private void SetupAmmo()
    {
        foreach (int ammo in ItemTypeConstants.Ammo())
        {
            List<CraftingCategory> properties =
            [
                DamageProperties.Ammo,
                GenericItemProperties.VampiricRegeneration,
                AmmoOnHitProperties.OnHits
            ];

            Properties.TryAdd(ammo, properties);
        }
    }

    public IReadOnlyList<CraftingProperty> UncategorizedPropertiesFor(int baseItemType)
    {
        List<CraftingProperty?> properties = [];

        foreach (CraftingCategory category in Properties[baseItemType])
        {
            properties.AddRange(category.Properties);
        }

        return properties;
    }

    public IReadOnlyList<CraftingProperty> UncategorizedPropertiesForNwItem(NwItem selection)
    {
        int baseItemInt = NWScript.GetBaseItemType(selection);
        return UncategorizedPropertiesFor(baseItemInt);
    }
}
