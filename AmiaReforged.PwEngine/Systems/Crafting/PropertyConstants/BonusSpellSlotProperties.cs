using AmiaReforged.PwEngine.Systems.Crafting.Models;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.PropertyConstants;

public static class BonusSpellSlotProperties
{
    public static readonly CraftingPropertyCategory AssassinBonusSpells = new()
    {
        Label = "Assassin Bonus Spells",
        Properties = new[]
        {
            // Perfect
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory BardBonusSpells = new()
    {
        Label = "Bard Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 0)!,
                GuiLabel = "Level 0",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 5)!,
                GuiLabel = "Level 5",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 6)!,
                GuiLabel = "Level 6",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory BlackguardBonusSpells = new()
    {
        Label = "Blackguard Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory ClericBonusSpells = new()
    {
        Label = "Cleric Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 0)!,
                GuiLabel = "Level 0",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 5)!,
                GuiLabel = "Level 5",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 6)!,
                GuiLabel = "Level 6",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 7)!,
                GuiLabel = "Level 7",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 8)!,
                GuiLabel = "Level 8",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 9)!,
                GuiLabel = "Level 9",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory DruidBonusSpells = new()
    {
        Label = "Druid Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 0)!,
                GuiLabel = "Level 0",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 5)!,
                GuiLabel = "Level 5",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 6)!,
                GuiLabel = "Level 6",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 7)!,
                GuiLabel = "Level 7",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 8)!,
                GuiLabel = "Level 8",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 9)!,
                GuiLabel = "Level 9",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory PaladinBonusSpells = new()
    {
        Label = "Paladin Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory RangerBonusSpells = new()
    {
        Label = "Ranger Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory SorcererBonusSpells = new()
    {
        Label = "Sorcerer Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 0)!,
                GuiLabel = "Level 0",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 5)!,
                GuiLabel = "Level 5",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 6)!,
                GuiLabel = "Level 6",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 7)!,
                GuiLabel = "Level 7",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 8)!,
                GuiLabel = "Level 8",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 9)!,
                GuiLabel = "Level 9",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };

    public static readonly CraftingPropertyCategory WizardBonusSpells = new()
    {
        Label = "Wizard Bonus Spells",
        // Perfect
        Properties = new[]
        {
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 0)!,
                GuiLabel = "Level 0",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 1)!,
                GuiLabel = "Level 1",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 2)!,
                GuiLabel = "Level 2",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 3)!,
                GuiLabel = "Level 3",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 4)!,
                GuiLabel = "Level 4",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 5)!,
                GuiLabel = "Level 5",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 6)!,
                GuiLabel = "Level 6",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 7)!,
                GuiLabel = "Level 7",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 8)!,
                GuiLabel = "Level 8",
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                Cost = 1,
                Property = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 9)!,
                GuiLabel = "Level 9",
                CraftingTier = CraftingTier.Perfect
            }
        }
    };
}