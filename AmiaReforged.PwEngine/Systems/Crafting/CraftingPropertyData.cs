using AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CraftingPropertyData))]
public class CraftingPropertyData
{
    public Dictionary<int, IReadOnlyCollection<CraftingProperty>> Properties { get; } = new();

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
            List<CraftingProperty> properties = new();

            AddEquippedItemProperties(properties);

            Properties.TryAdd(item, properties);
        }
    }

    public void SetupMagicStaves()
    {
        List<CraftingProperty> properties = new();
        
        AddEquippedItemProperties(properties);
        
        // TODO: Support for spell slots.
        
        Properties.TryAdd(NWScript.BASE_ITEM_MAGICSTAFF, properties);
    }

    private void SetupAmulets()
    {
        // This list of properties is different because natural armor has its own unique costs.
        List<CraftingProperty> properties = new()
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(1)!,
                GuiLabel = "+1 AC",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2 AC",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3 AC",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4 AC",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5 AC",
                CraftingTier = CraftingTier.Flawless
            }
        };

        properties.AddRange(GenericItemProperties.ElementalResistances);
        properties.AddRange(GenericItemProperties.DamageReductions);
        properties.AddRange(GenericItemProperties.PhysicalDamageResistances);
        properties.AddRange(GenericItemProperties.Regeneration);

        properties.AddRange(SavingThrowProperties.SpecificSaves);

        properties.AddRange(SkillProperties.Personal);
        properties.AddRange(SkillProperties.Advantageous);

        properties.AddRange(AbilityProperties.Abilities);

        Properties.TryAdd(NWScript.BASE_ITEM_AMULET, properties);
    }

    private void SetupGloves()
    {

            List<CraftingProperty> properties = new();
            
            AddEquippedItemProperties(properties);

            // These are also weapons...
            properties.AddRange(DamageProperties.OneHanders);
            properties.AddRange(DamageProperties.MassiveCriticals);
            
            // Gauntlets have another tier of massive criticals.
            properties.Add(new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d6)!,
                GuiLabel = "2d6 Massive Criticals",
                CraftingTier = CraftingTier.Flawless
            });
            
            properties.AddRange(GenericItemProperties.VampiricRegeneration);
            properties.Add(GenericItemProperties.Keen);
            
            Properties.TryAdd(NWScript.BASE_ITEM_GLOVES, properties);
    }

    private static void AddEquippedItemProperties(List<CraftingProperty> properties)
    {
        properties.AddRange(GenericItemProperties.Armor);
        properties.AddRange(GenericItemProperties.ElementalResistances);
        properties.AddRange(GenericItemProperties.PhysicalDamageResistances);
        properties.AddRange(GenericItemProperties.DamageReductions);
        properties.AddRange(GenericItemProperties.Regeneration);

        properties.AddRange(SavingThrowProperties.SpecificSaves);
        properties.AddRange(SavingThrowProperties.GeneralSaves);

        properties.AddRange(SkillProperties.Personal);
        properties.AddRange(SkillProperties.Advantageous);

        properties.AddRange(AbilityProperties.Abilities);
    }

    private void Setup1HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.MeleeWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.OneHanders);

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private void Setup2HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.Melee2HWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.TwoHanders);

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private static void AddSharedWeaponProperties(List<CraftingProperty> properties)
    {
        properties.AddRange(DamageProperties.MassiveCriticals);

        properties.AddRange(AttackBonusProperties.AttackBonus);
        properties.AddRange(AttackBonusProperties.EnhancementBonus);

        properties.AddRange(GenericItemProperties.VampiricRegeneration);
        properties.AddRange(GenericItemProperties.Armor);
        properties.AddRange(GenericItemProperties.Regeneration);
        properties.Add(GenericItemProperties.Keen);

        properties.AddRange(SkillProperties.Personal);
        properties.AddRange(SkillProperties.Advantageous);

        properties.AddRange(AbilityProperties.Abilities);

        properties.AddRange(VisualEffectConstants.VisualEffects);
    }

    private void SetupThrownWeapons()
    {
        foreach (int weapon in ItemTypeConstants.ThrownWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.OneHanders);
            
            // Thrown Weapons have a different cost for Keen
            properties.Add(new CraftingProperty
            {
                Cost = 3,
                Property = NWScript.ItemPropertyKeen()!,
                GuiLabel = "Keen",
                CraftingTier = CraftingTier.Perfect
            });

            AddSharedWeaponProperties(properties);

            Properties.TryAdd(weapon, properties);
        }
    }

    private void SetupRangedWeapons()
    {
        foreach (int weapon in ItemTypeConstants.RangedWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(AttackBonusProperties.AttackBonus);

            properties.AddRange(DamageProperties.Mighty);
            properties.AddRange(DamageProperties.MassiveCriticals);

            properties.AddRange(SkillProperties.Advantageous);
            properties.AddRange(SkillProperties.Personal);

            properties.AddRange(GenericItemProperties.Regeneration);

            properties.AddRange(AbilityProperties.Abilities);
            
            //Ranged have extra Massive Critical options
            properties.Add(new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_1d12)!,
                GuiLabel = "1d12 Massive Criticals",
                CraftingTier = CraftingTier.DreamCoin
            });
            properties.Add(new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertyMassiveCritical(NWScript.IP_CONST_DAMAGEBONUS_2d12)!,
                GuiLabel = "2d12 Massive Criticals",
                CraftingTier = CraftingTier.DreamCoin
            });

            Properties.TryAdd(weapon, properties);
        }
    }

    private void SetupAmmo()
    {
        foreach (int ammo in ItemTypeConstants.Ammo())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.Ammo);
            properties.AddRange(GenericItemProperties.VampiricRegeneration);

            Properties.TryAdd(ammo, properties);
        }
    }
}