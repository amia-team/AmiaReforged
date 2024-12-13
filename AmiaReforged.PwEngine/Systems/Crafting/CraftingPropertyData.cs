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

            Properties.Add(item, properties);
        }
    }

    public void SetupMagicStaves()
    {
        List<CraftingProperty> properties = new();
        
        AddEquippedItemProperties(properties);
        
        // TODO: Support for spell slots.
        
        Properties.Add(NWScript.BASE_ITEM_MAGICSTAFF, properties);
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
                GuiLabel = "+1",
                CraftingTier = CraftingTier.Minor
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(2)!,
                GuiLabel = "+2",
                CraftingTier = CraftingTier.Lesser
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(3)!,
                GuiLabel = "+3",
                CraftingTier = CraftingTier.Intermediate
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyACBonus(4)!,
                GuiLabel = "+4",
                CraftingTier = CraftingTier.Greater
            },
            new CraftingProperty
            {
                Cost = 2,
                Property = NWScript.ItemPropertyACBonus(5)!,
                GuiLabel = "+5",
                CraftingTier = CraftingTier.Flawless
            }
        };

        properties.AddRange(GenericItemProperties.ElementalResistances);
        properties.AddRange(GenericItemProperties.DamageReductions);
        properties.AddRange(GenericItemProperties.PhysicalDamageResistances);
        properties.AddRange(GenericItemProperties.Regeneration);

        properties.AddRange(SavingThrowProperties.SpecificSaves);

        properties.AddRange(SkillProperties.Roleplay);
        properties.AddRange(SkillProperties.Beneficial);

        properties.AddRange(AbilityProperties.Abilities);

        Properties.Add(NWScript.BASE_ITEM_AMULET, properties);
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
                GuiLabel = "Massive Criticals 2d6",
                CraftingTier = CraftingTier.Flawless
            });
            
            properties.AddRange(GenericItemProperties.VampiricRegeneration);
            properties.Add(GenericItemProperties.Keen);
            
            Properties.Add(NWScript.BASE_ITEM_GLOVES, properties);
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

        properties.AddRange(SkillProperties.Roleplay);
        properties.AddRange(SkillProperties.Beneficial);

        properties.AddRange(AbilityProperties.Abilities);
    }

    private void Setup1HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.MeleeWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.OneHanders);

            AddSharedWeaponProperties(properties);

            Properties.Add(weapon, properties);
        }
    }

    private void Setup2HMeleeWeapons()
    {
        foreach (int weapon in ItemTypeConstants.MeleeWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.TwoHanders);

            AddSharedWeaponProperties(properties);

            Properties.Add(weapon, properties);
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

        properties.AddRange(SkillProperties.Roleplay);
        properties.AddRange(SkillProperties.Beneficial);

        properties.AddRange(AbilityProperties.Abilities);

        properties.AddRange(VisualEffectConstants.VisualEffects);
    }

    private void SetupThrownWeapons()
    {
        foreach (int weapon in ItemTypeConstants.ThrownWeapons())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.OneHanders);

            AddSharedWeaponProperties(properties);

            Properties.Add(weapon, properties);
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

            properties.AddRange(SkillProperties.Beneficial);
            properties.AddRange(SkillProperties.Roleplay);

            properties.AddRange(GenericItemProperties.Regeneration);

            properties.AddRange(AbilityProperties.Abilities);

            Properties.Add(weapon, properties);
        }
    }

    private void SetupAmmo()
    {
        foreach (int ammo in ItemTypeConstants.Ammo())
        {
            List<CraftingProperty> properties = new();

            properties.AddRange(DamageProperties.Ammo);
            properties.AddRange(GenericItemProperties.VampiricRegeneration);

            Properties.Add(ammo, properties);
        }
    }
}