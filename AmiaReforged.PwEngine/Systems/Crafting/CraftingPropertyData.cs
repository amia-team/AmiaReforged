using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CraftingPropertyData))]
public class CraftingPropertyData
{
    public Dictionary<int, IReadOnlyList<CraftingCategory>> Properties { get; } = new();

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

    private void SetupEquippedItems()
    {
        foreach (int item in ItemTypeConstants.EquippableItems())
        {
            List<CraftingCategory> properties = new();

            AddEquippedItemProperties(properties);

            Properties.TryAdd(item, properties);
        }
    }

    public void SetupMagicStaves()
    {
        List<CraftingCategory> properties = new();

        AddEquippedItemProperties(properties);

        // TODO: Support for spell slots.

        Properties.TryAdd(NWScript.BASE_ITEM_MAGICSTAFF, properties);
    }

    private void SetupAmulets()
    {
        // This list of properties is different because natural armor has its own unique costs.
        List<CraftingCategory> properties = new()
        {
            new CraftingCategory("natural_armor")
            {
                Label = "Armor",
                Properties = new[]
                {
                    new CraftingProperty
                    {
                        Cost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(1)!,
                        GuiLabel = "+1 AC",
                        CraftingTier = CraftingTier.Minor
                    },
                    new CraftingProperty
                    {
                        Cost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(2)!,
                        GuiLabel = "+2 AC",
                        CraftingTier = CraftingTier.Lesser
                    },
                    new CraftingProperty
                    {
                        Cost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(3)!,
                        GuiLabel = "+3 AC",
                        CraftingTier = CraftingTier.Intermediate
                    },
                    new CraftingProperty
                    {
                        Cost = 1,
                        ItemProperty = NWScript.ItemPropertyACBonus(4)!,
                        GuiLabel = "+4 AC",
                        CraftingTier = CraftingTier.Greater
                    },
                    new CraftingProperty
                    {
                        Cost = 2,
                        ItemProperty = NWScript.ItemPropertyACBonus(5)!,
                        GuiLabel = "+5 AC",
                        CraftingTier = CraftingTier.Flawless
                    }
                }
            }
        };

        properties.Add(GenericItemProperties.ElementalResistances);
        properties.Add(GenericItemProperties.DamageReductions);
        properties.Add(GenericItemProperties.PhysicalDamageResistances);
        properties.Add(GenericItemProperties.Regeneration);

        properties.Add(SavingThrowProperties.SpecificSaves);

        properties.Add(SkillProperties.Personal);
        properties.Add(SkillProperties.Advantageous);

        properties.Add(AbilityProperties.Abilities);

        Properties.TryAdd(NWScript.BASE_ITEM_AMULET, properties);
    }

    private void SetupGloves()
    {
        List<CraftingCategory> properties = new();

        AddEquippedItemProperties(properties);

        // These are also weapons...
        properties.Add(DamageProperties.OneHanders);
        properties.Add(DamageProperties.MassiveCriticals);

        // Gauntlets have another tier of massive criticals.
        properties.Add(new CraftingCategory("massive_criticals")
        {
            Label = "Massive Criticals",
            Properties = new[]
            {
                new CraftingProperty
                {
                    Cost = 1,
                    ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                    GuiLabel = "2d6 Massive Criticals",
                    CraftingTier = CraftingTier.Flawless
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

        properties.Add(SkillProperties.Personal);
        properties.Add(SkillProperties.Advantageous);

        properties.Add(AbilityProperties.Abilities);
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

        properties.Add(VisualEffectConstants.VisualEffects);
    }

    private void SetupThrownWeapons()
    {
        foreach (int weapon in ItemTypeConstants.ThrownWeapons())
        {
            List<CraftingCategory> properties = new() { DamageProperties.OneHanders };

            // Thrown Weapons have a different cost for Keen
            properties.Add(GenericItemProperties.Other);

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
                new CraftingCategory("ranged_massive_criticals")
                {
                    Label = "Massive Criticals",
                    Properties = new[]
                    {
                        new CraftingProperty
                        {
                            Cost = 1,
                            ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_1d12)!,
                            GuiLabel = "1d12 Massive Criticals",
                            CraftingTier = CraftingTier.DreamCoin
                        },
                        new CraftingProperty
                        {
                            Cost = 2,
                            ItemProperty = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d12)!,
                            GuiLabel = "2d12 Massive Criticals",
                            CraftingTier = CraftingTier.DreamCoin
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
                GenericItemProperties.VampiricRegeneration
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

        return (IReadOnlyList<CraftingProperty>) properties;
    }
}