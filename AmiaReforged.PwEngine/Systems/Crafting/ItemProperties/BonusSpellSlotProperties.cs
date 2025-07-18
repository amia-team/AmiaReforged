﻿using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.ItemProperties;

public static class BonusSpellSlotProperties
{
    private const int MythalCostBslot1 = 10000;
    private const int MythalCostBslot2 = 20000;
    private const int MythalCostBslot3 = 30000;
    private const int MythalCostBslot4 = 40000;
    private const int MythalCostBslot5 = 50000;
    private const int MythalCostBslot6 = 60000;
    private const int MythalCostBslot7 = 70000;
    private const int MythalCostBslot8 = 80000;
    private const int MythalCostBslot9 = 90000;

    public static readonly CraftingCategory AssassinBonusSpells = new(categoryId: "assassin_bonus_spells")
    {
        Label = "Assassin Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 1)!,
                GuiLabel = "Assassin Slot: Level 1",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 2)!,
                GuiLabel = "Assassin Slot: Level 2",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 3)!,
                GuiLabel = "Assassin Slot: Level 3",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_ASSASSIN, 4)!,
                GuiLabel = "Assassin Slot: Level 4",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Assassin,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory BardBonusSpells = new(categoryId: "bard_bonus_spells")
    {
        Label = "Bard Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 0)!,
                GuiLabel = "Bard Slot: Level 0",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 1)!,
                GuiLabel = "Bard Slot: Level 1",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 2)!,
                GuiLabel = "Bard Slot: Level 2",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 3)!,
                GuiLabel = "Bard Slot: Level 3",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 4)!,
                GuiLabel = "Bard Slot: Level 4",
                GoldCost = MythalCostBslot5,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 5)!,
                GuiLabel = "Bard Slot: Level 5",
                GoldCost = MythalCostBslot6,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_BARD, 6)!,
                GuiLabel = "Bard Slot: Level 6",
                GoldCost = MythalCostBslot7,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Bard,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory BlackguardBonusSpells = new(categoryId: "blackguard_bonus_spells")
    {
        Label = "Blackguard Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 1)!,
                GuiLabel = "Bard Slot: Level 1",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 2)!,
                GuiLabel = "Bard Slot: Level 2",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 3)!,
                GuiLabel = "Bard Slot: Level 3",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.CLASS_TYPE_BLACKGUARD, 4)!,
                GuiLabel = "Bard Slot: Level 4",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Blackguard,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory ClericBonusSpells = new(categoryId: "cleric_bonus_spells")
    {
        Label = "Cleric Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 0)!,
                GuiLabel = "Cleric Slot: Level 0",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 1)!,
                GuiLabel = "Cleric Slot: Level 1",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 2)!,
                GuiLabel = "Cleric Slot: Level 2",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 3)!,
                GuiLabel = "Cleric Slot: Level 3",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 4)!,
                GuiLabel = "Cleric Slot: Level 4",
                GoldCost = MythalCostBslot5,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 5)!,
                GuiLabel = "Cleric Slot: Level 5",
                GoldCost = MythalCostBslot6,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 6)!,
                GuiLabel = "Cleric Slot: Level 6",
                GoldCost = MythalCostBslot7,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 7)!,
                GuiLabel = "Cleric Slot: Level 7",
                GoldCost = MythalCostBslot8,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 8)!,
                GuiLabel = "Cleric Slot: Level 8",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_CLERIC, 9)!,
                GuiLabel = "Cleric Slot: Level 9",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Cleric,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory DruidBonusSpells = new(categoryId: "druid_bonus_spells")
    {
        Label = "Druid Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 0)!,
                GuiLabel = "Druid Slot: Level 0",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 1)!,
                GuiLabel = "Druid Slot: Level 1",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 2)!,
                GuiLabel = "Druid Slot: Level 2",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 3)!,
                GuiLabel = "Druid Slot: Level 3",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 4)!,
                GuiLabel = "Druid Slot: Level 4",
                GoldCost = MythalCostBslot5,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 5)!,
                GuiLabel = "Druid Slot: Level 5",
                GoldCost = MythalCostBslot6,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 6)!,
                GuiLabel = "Druid Slot: Level 6",
                GoldCost = MythalCostBslot7,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 7)!,
                GuiLabel = "Druid Slot: Level 7",
                GoldCost = MythalCostBslot8,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 8)!,
                GuiLabel = "Druid Slot: Level 8",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_DRUID, 9)!,
                GuiLabel = "Druid Slot: Level 9",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Druid,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory PaladinBonusSpells = new(categoryId: "paladin_bonus_spells")
    {
        Label = "Paladin Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 1)!,
                GuiLabel = "Paladin Slot: Level 1",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 2)!,
                GuiLabel = "Paladin Slot: Level 2",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 3)!,
                GuiLabel = "Paladin Slot: Level 3",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_PALADIN, 4)!,
                GuiLabel = "Paladin Slot: Level 4",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Paladin,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory RangerBonusSpells = new(categoryId: "ranger_bonus_spells")
    {
        Label = "Ranger Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 1)!,
                GuiLabel = "Ranger Slot: Level 1",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 2)!,
                GuiLabel = "Ranger Slot: Level 2",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 3)!,
                GuiLabel = "Ranger Slot: Level 3",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_RANGER, 4)!,
                GuiLabel = "Ranger Slot: Level 4",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Ranger,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory SorcererBonusSpells = new(categoryId: "sorcerer_bonus_spells")
    {
        Label = "Sorcerer Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 0)!,
                GuiLabel = "Sorcerer Slot: Level 0",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 1)!,
                GuiLabel = "Sorcerer Slot: Level 1",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 2)!,
                GuiLabel = "Sorcerer Slot: Level 2",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 3)!,
                GuiLabel = "Sorcerer Slot: Level 3",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 4)!,
                GuiLabel = "Sorcerer Slot: Level 4",
                GoldCost = MythalCostBslot5,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 5)!,
                GuiLabel = "Sorcerer Slot: Level 5",
                GoldCost = MythalCostBslot6,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 6)!,
                GuiLabel = "Sorcerer Slot: Level 6",
                GoldCost = MythalCostBslot7,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 7)!,
                GuiLabel = "Sorcerer Slot: Level 7",
                GoldCost = MythalCostBslot8,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 8)!,
                GuiLabel = "Sorcerer Slot: Level 8",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_SORCERER, 9)!,
                GuiLabel = "Sorcerer Slot: Level 9",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Sorcerer,
        BaseDifficulty = 25
    };

    public static readonly CraftingCategory WizardBonusSpells = new(categoryId: "wizard_bonus_spells")
    {
        Label = "Wizard Bonus Spells",
        Properties =
        [
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 0)!,
                GuiLabel = "Wizard Slot: Level 0",
                GoldCost = MythalCostBslot1,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 1)!,
                GuiLabel = "Wizard Slot: Level 1",
                GoldCost = MythalCostBslot2,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 2)!,
                GuiLabel = "Wizard Slot: Level 2",
                GoldCost = MythalCostBslot3,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 3)!,
                GuiLabel = "Wizard Slot: Level 3",
                GoldCost = MythalCostBslot4,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 4)!,
                GuiLabel = "Wizard Slot: Level 4",
                GoldCost = MythalCostBslot5,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 5)!,
                GuiLabel = "Wizard Slot: Level 5",
                GoldCost = MythalCostBslot6,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 6)!,
                GuiLabel = "Wizard Slot: Level 6",
                GoldCost = MythalCostBslot7,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 7)!,
                GuiLabel = "Wizard Slot: Level 7",
                GoldCost = MythalCostBslot8,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 8)!,
                GuiLabel = "Wizard Slot: Level 8",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            },
            new CraftingProperty
            {
                PowerCost = 1,
                ItemProperty = NWScript.ItemPropertyBonusLevelSpell(NWScript.IP_CONST_CLASS_WIZARD, 9)!,
                GuiLabel = "Wizard Slot: Level 9",
                GoldCost = MythalCostBslot9,
                CraftingTier = CraftingTier.Perfect
            }
        ],
        ExclusiveToClass = true,
        ExclusiveClass = ClassType.Wizard,
        BaseDifficulty = 25
    };
}