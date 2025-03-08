using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

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
            List<CraftingCategory> properties = new();

            AddEquippedItemProperties(properties);

            Properties.TryAdd(item, properties);
        }
    }

    private void SetupMagicStaves()
    {
        List<CraftingCategory> properties = new();

        AddEquippedItemProperties(properties);

        Properties.TryAdd(CasterWeapon1H, properties);
        Properties.TryAdd(CasterWeapon2H, properties);
    }

    private void SetupAmulets()
    {
        // This list of properties is different because natural armor has its own unique costs.
        List<CraftingCategory> properties = new()
        {
            new(categoryId: "natural_armor")
            {
                Label = "Armor",
                Properties = new[]
                {
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
                }
            }
        };

        properties.Add(GenericItemProperties.ElementalResistances);
        properties.Add(GenericItemProperties.DamageReductions);
        properties.Add(GenericItemProperties.PhysicalDamageResistances);
        properties.Add(GenericItemProperties.Regeneration);

        properties.Add(SavingThrowProperties.SpecificSaves);
        properties.Add(SavingThrowProperties.GeneralSaves);
        properties.Add(SavingThrowProperties.UniversalSaves);

        properties.Add(SkillProperties.Personal);
        properties.Add(SkillProperties.Advantageous);

        properties.Add(AbilityProperties.Abilities);

        properties.Add(CastSpellProperties.FluffSpells);
        properties.Add(CastSpellProperties.BeneficialSpells);


        Properties.TryAdd(NWScript.BASE_ITEM_AMULET, properties);
    }

    private void SetupGloves()
    {
        List<CraftingCategory> properties = new();

        AddEquippedItemProperties(properties);

        // These are also weapons...
        properties.Add(DamageProperties.GloveDamage);
        properties.Add(DamageProperties.MassiveCriticals);
        properties.Add(MeleeOnHitProperties.OnHits);

        // Gauntlets have another tier of massive criticals.
        properties.Add(new(categoryId: "massive_criticals")
        {
            Label = "Massive Criticals",
            Properties = new[]
            {
                new CraftingProperty
                {
                    PowerCost = 1,
                    ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                    GuiLabel = "2d6 Massive Criticals",
                    CraftingTier = CraftingTier.Flawless,
                    GoldCost = 20000
                }
            }
        });

        properties.Add(GenericItemProperties.VampiricRegeneration);
        properties.Add(GenericItemProperties.Other);

        Properties.TryAdd(NWScript.BASE_ITEM_GLOVES, properties);
    }

    private static void AddEquippedItemProperties(List<CraftingCategory> properties)
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

        properties.Add(BonusSpellSlotProperties.AssassinBonusSpells);
        properties.Add(BonusSpellSlotProperties.BardBonusSpells);
        properties.Add(BonusSpellSlotProperties.ClericBonusSpells);
        properties.Add(BonusSpellSlotProperties.DruidBonusSpells);
        properties.Add(BonusSpellSlotProperties.PaladinBonusSpells);
        properties.Add(BonusSpellSlotProperties.RangerBonusSpells);
        properties.Add(BonusSpellSlotProperties.SorcererBonusSpells);
        properties.Add(BonusSpellSlotProperties.WizardBonusSpells);

        properties.Add(SpellResistanceProperties.SpellResistances);
    }

    private void Setup1HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.MeleeWeapons())
        {
            List<CraftingCategory> properties = new() { DamageProperties.OneHanders };

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private void Setup2HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.Melee2HWeapons())
        {
            List<CraftingCategory> properties = new() { DamageProperties.TwoHanders };

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
        properties.Add(GenericItemProperties.Other);

        properties.Add(SkillProperties.Personal);
        properties.Add(SkillProperties.Advantageous);

        properties.Add(AbilityProperties.Abilities);

        properties.Add(MeleeOnHitProperties.OnHits);

        properties.Add(VisualEffectConstants.VisualEffects);
    }

    private void SetupThrownWeapons()
    {
        foreach (int weapon in ItemTypeConstants.ThrownWeapons())
        {
            List<CraftingCategory> properties = new()
            {
                DamageProperties.OneHanders,
                GenericItemProperties.Other
            };

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private void SetupRangedWeapons()
    {
        foreach (int weapon in ItemTypeConstants.RangedWeapons())
        {
            List<CraftingCategory> properties = new()
            {
                AttackBonusProperties.AttackBonus,
                DamageProperties.Mighty,
                DamageProperties.MassiveCriticals,
                SkillProperties.Advantageous,
                SkillProperties.Personal,
                GenericItemProperties.Regeneration,
                AbilityProperties.Abilities,
                //Ranged have extra Massive Critical options
                new(categoryId: "ranged_massive_criticals")
                {
                    Label = "Massive Criticals",
                    Properties = new[]
                    {
                        new CraftingProperty
                        {
                            PowerCost = 1,
                            ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_1d12)!,
                            GuiLabel = "1d12 Massive Criticals",
                            CraftingTier = CraftingTier.Wondrous
                        },
                        new CraftingProperty
                        {
                            PowerCost = 2,
                            ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d12)!,
                            GuiLabel = "2d12 Massive Criticals",
                            CraftingTier = CraftingTier.Wondrous
                        }
                    }
                }
            };

            Properties.TryAdd(weapon, properties);
        }
    }

    private void SetupAmmo()
    {
        foreach (int ammo in ItemTypeConstants.Ammo())
        {
            List<CraftingCategory> properties = new()
            {
                DamageProperties.Ammo,
                GenericItemProperties.VampiricRegeneration,
                AmmoOnHitProperties.OnHits
            };

            Properties.TryAdd(ammo, properties);
        }
    }

    public IReadOnlyList<CraftingProperty> UncategorizedPropertiesFor(int baseItemType)
    {
        List<CraftingProperty?> properties = new();

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