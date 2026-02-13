using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.GuardSpawner;

/// <summary>
/// Static data definitions for the Guard Spawner tool.
/// Contains settlement definitions, creature mappings, and AI script configurations.
/// </summary>
public static class GuardSpawnerData
{
    /// <summary>
    /// Represents a settlement or nation with its linked settlement IDs.
    /// </summary>
    public record GuardSettlement(string DisplayName, int[] LinkedSettlementIds);

    /// <summary>
    /// Represents a guard creature that can be added to a widget.
    /// </summary>
    public record GuardCreature(string DisplayName, string ResRef);

    /// <summary>
    /// All available settlements/nations for the dropdown.
    /// Nations group multiple settlement IDs together.
    /// </summary>
    public static readonly List<GuardSettlement> AllSettlements =
    [
        // Nations (multiple linked settlements)
        new GuardSettlement("Kingdom of Barak Runedar", [1, 2]),
        new GuardSettlement("Nation of Forrstakkr", [3, 4, 5]),
        new GuardSettlement("Kingdom of Kohlingen", [6, 7, 8, 9]),
        new GuardSettlement("Jarldom of Ostland", [11, 12, 13]),
        new GuardSettlement("Jarldom of Wiltun", [14, 15]),
        new GuardSettlement("Nation of Moonvale", [27, 29]),
        new GuardSettlement("Republic of Southport", [32, 35, 36]),

        // Individual settlements
        new GuardSettlement("Calderis", [10]),
        new GuardSettlement("Belenoth", [16]),
        new GuardSettlement("Monsters", [17]),
        new GuardSettlement("Blue Lagoon", [18]),
        new GuardSettlement("Brokentooth Cave", [19]),
        new GuardSettlement("Chillwyck", [20]),
        new GuardSettlement("The Dale", [21]),
        new GuardSettlement("Djedet", [22]),
        new GuardSettlement("Eilistraeen Shrine", [23]),
        new GuardSettlement("Endir's Point", [24]),
        new GuardSettlement("Hangman's Cove", [25]),
        new GuardSettlement("L'Obsul", [26]),
        new GuardSettlement("Nes'ek", [28]),
        new GuardSettlement("Quagmire Camp", [30]),
        new GuardSettlement("Quagmire Kobolds", [31]),
        new GuardSettlement("Salandran Temple", [33]),
        new GuardSettlement("Shadowscape", [34]),
        new GuardSettlement("Triumvir", [37]),
        new GuardSettlement("Winya Ravana", [38]),
        new GuardSettlement("Zanshibon", [39]),
        new GuardSettlement("Crouching Lemur", [40])
    ];

    /// <summary>
    /// Settlement IDs that are part of the Beacon Alliance.
    /// Beacon widgets use ally_count=15, qty=4, and include all these settlements.
    /// </summary>
    public static readonly int[] BeaconAllianceSettlementIds =
    [
        6, 7, 8, 9, 18, 21, 23, 27, 29, 32, 33, 35, 36, 38, 40
    ];

    /// <summary>
    /// Creature resrefs that should use the MAGE AI script set.
    /// </summary>
    public static readonly HashSet<string> MageResrefs =
    [
        "br_mage",
        "kohl_mage",
        "wilt_mage",
        "bele_mage",
        "bldsp_mage",
        "dale_mage",
        "dale_cleric",
        "moon_mguard",
        "grove_mage",
        "grove_druid",
        "winya_mage",
        "cl_mage"
    ];

    /// <summary>
    /// Standard AI scripts for non-mage guard creatures.
    /// </summary>
    public static readonly Dictionary<EventScriptType, string> StandardAiScripts = new()
    {
        { EventScriptType.CreatureOnBlockedByDoor, "x0_ch_hen_block" },
        { EventScriptType.CreatureOnDamaged, "x0_ch_hen_damage" },
        { EventScriptType.CreatureOnDeath, "hen_death_pm" },
        { EventScriptType.CreatureOnDialogue, "hen_conv_pm" },
        { EventScriptType.CreatureOnDisturbed, "x0_ch_hen_distrb" },
        { EventScriptType.CreatureOnEndCombatRound, "x0_ch_hen_combat" },
        { EventScriptType.CreatureOnHeartbeat, "hb_henchsum" },
        { EventScriptType.CreatureOnMeleeAttacked, "x0_ch_hen_attack" },
        { EventScriptType.CreatureOnNotice, "x0_ch_hen_percep" },
        { EventScriptType.CreatureOnRested, "hen_rest_pm" },
        { EventScriptType.CreatureOnSpawnIn, "hen_spawn_pm" },
        { EventScriptType.CreatureOnSpellCastAt, "x0_ch_hen_spell" },
        { EventScriptType.CreatureOnUserDefinedEvent, "x0_ch_hen_usrdef" }
    };

    /// <summary>
    /// MAGE AI scripts for spellcasting guard creatures.
    /// </summary>
    public static readonly Dictionary<EventScriptType, string> MageAiScripts = new()
    {
        { EventScriptType.CreatureOnBlockedByDoor, "j_ai_onblocked" },
        { EventScriptType.CreatureOnDamaged, "j_ai_ondamaged" },
        { EventScriptType.CreatureOnDeath, "hen_death_pm" },
        { EventScriptType.CreatureOnDialogue, "hen_conv_pm" },
        { EventScriptType.CreatureOnDisturbed, "j_ai_ondisturbed" },
        { EventScriptType.CreatureOnEndCombatRound, "j_ai_oncombatrou" },
        { EventScriptType.CreatureOnHeartbeat, "x0_ch_hen_heart" },
        { EventScriptType.CreatureOnMeleeAttacked, "j_ai_onphiattack" },
        { EventScriptType.CreatureOnNotice, "j_ai_onpercieve" },
        { EventScriptType.CreatureOnRested, "hen_rest_pm" },
        { EventScriptType.CreatureOnSpawnIn, "hen_spawn_pm" },
        { EventScriptType.CreatureOnSpellCastAt, "j_ai_onspellcast" },
        { EventScriptType.CreatureOnUserDefinedEvent, "j_ai_onuserdef" }
    };

    /// <summary>
    /// Creatures available for each settlement/nation.
    /// Key is the settlement display name, value is the list of available creatures.
    /// </summary>
    public static readonly Dictionary<string, List<GuardCreature>> CreaturesBySettlement = new()
    {
        ["Kingdom of Barak Runedar"] =
        [
            new GuardCreature("Fyrd", "br_militia"),
            new GuardCreature("Hearthguard", "br_elite"),
            new GuardCreature("Raider", "br_scout"),
            new GuardCreature("Runecaster", "br_mage")
        ],

        ["Nation of Forrstakkr"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Kingdom of Kohlingen"] =
        [
            new GuardCreature("Everguard", "kohl_everguard"),
            new GuardCreature("Platinum Knight", "kohl_pk"),
            new GuardCreature("Roadwarden", "roadwarden"),
            new GuardCreature("Silver Dragon Cavalier", "kohl_cav"),
            new GuardCreature("Silver Dragon Construct", "kohl_contruct"),
            new GuardCreature("Silver Dragon Guard", "kohl_guard"),
            new GuardCreature("Silver Dragon Knight", "kohl_sdknight"),
            new GuardCreature("Silver Dragon Mage", "kohl_mage"),
            new GuardCreature("Silver Dragon Ranger", "kohl_scout"),
            new GuardCreature("Tempuran", "kohl_tempuran")
        ],

        ["Jarldom of Ostland"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Jarldom of Wiltun"] =
        [
            new GuardCreature("Iron Sentinel", "wilt_elite"),
            new GuardCreature("Iron Defender", "wilt_guard"),
            new GuardCreature("Iron Caller", "wilt_mage"),
            new GuardCreature("Iron Deadshot", "wilt_scout")
        ],

        ["Calderis"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Belenoth"] =
        [
            new GuardCreature("Belenoth Golem Elite", "bele_elite"),
            new GuardCreature("Belenoth Militia", "bele_militia"),
            new GuardCreature("Belenoth Marksman", "bele_scout"),
            new GuardCreature("Belenoth Sorcerer", "bele_mage")
        ],

        ["Monsters"] =
        [
            new GuardCreature("Bloodspear Zombie", "bldsp_undead"),
            new GuardCreature("Elite Berserker Ogre", "bldspr_elite"),
            new GuardCreature("Goblin Archer", "bldsp_scout"),
            new GuardCreature("Ogrillon Shaman", "bldsp_mage"),
            new GuardCreature("Orc Thug", "bldsp_guard")
        ],

        ["Blue Lagoon"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Brokentooth Cave"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Chillwyck"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["The Dale"] =
        [
            new GuardCreature("Dale Golem", "dale_construct"),
            new GuardCreature("Dale Militia", "dale_stdgrd"),
            new GuardCreature("Dale Militia Cavalier", "dale_cavalry"),
            new GuardCreature("Dale Militia Mage", "dale_mage"),
            new GuardCreature("Dale Militia Officer", "dale_elite"),
            new GuardCreature("Dale Warslinger", "dale_scout"),
            new GuardCreature("Hin Disciple", "dale_monk"),
            new GuardCreature("Marchwarden", "dale_mward"),
            new GuardCreature("Priest of Yondalla", "dale_cleric")
        ],

        ["Djedet"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Eilistraeen Shrine"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Endir's Point"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Hangman's Cove"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["L'Obsul"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Nation of Moonvale"] =
        [
            new GuardCreature("Moonknight", "moon_knight"),
            new GuardCreature("Moonpier Militia", "moon_militia"),
            new GuardCreature("Moonpier Mage Guard", "moon_mguard"),
            new GuardCreature("Moonpier Scout", "moonpier_scout"),
            new GuardCreature("Shambling Mound", "moon_shambler"),
            new GuardCreature("Oakmist Elder Ranger", "grove_elite"),
            new GuardCreature("Oakmist Ranger", "grove_guard"),
            new GuardCreature("Oakmist Feline Protector", "grove_cat"),
            new GuardCreature("Oakmist Canine Protector", "grove_wolf"),
            new GuardCreature("Enchantress", "grove_mage"),
            new GuardCreature("Oakmist Druid", "grove_druid"),
            new GuardCreature("Oakmist Ranger Scout", "grove_scout"),
            new GuardCreature("Oakmist Shambling Mound", "grove_shambler"),
            new GuardCreature("Oakmist Hulking Protector", "grove_hulk")
        ],

        ["Nes'ek"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Quagmire Camp"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Quagmire Kobolds"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Republic of Southport"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Salandran Temple"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Shadowscape"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Triumvir"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Winya Ravana"] =
        [
            new GuardCreature("Armathora Sentinel", "winya_knight"),
            new GuardCreature("Armathora Warden", "winya_guard"),
            new GuardCreature("Cooshee", "winya_cooshee"),
            new GuardCreature("Crested Felldrake", "winya_cfelldrake"),
            new GuardCreature("Spitting Felldrake", "winya_sfelldrake"),
            new GuardCreature("Winya Militia", "winya_militia"),
            new GuardCreature("Armathora Wizard", "winya_mage"),
            new GuardCreature("Armathora Ranger", "winya_archer"),
            new GuardCreature("Golem Guardian", "winya_golem")
        ],

        ["Zanshibon"] =
        [
            new GuardCreature("Placeholder", "roadwarden")
        ],

        ["Crouching Lemur"] =
        [
            new GuardCreature("Immaculate", "cl_elite"),
            new GuardCreature("Brother", "cl_guard"),
            new GuardCreature("Crouching Lemur Mage", "cl_mage"),
            new GuardCreature("Crouching Lemur Scout", "cl_scout")
        ]
    };

    /// <summary>
    /// Gets the creatures available for a given settlement.
    /// </summary>
    public static List<GuardCreature> GetCreaturesForSettlement(string settlementName)
    {
        return CreaturesBySettlement.TryGetValue(settlementName, out var creatures)
            ? creatures
            : [];
    }

    /// <summary>
    /// Determines if a creature resref uses mage AI scripts.
    /// </summary>
    public static bool IsMageCreature(string resref)
    {
        return MageResrefs.Contains(resref);
    }

    /// <summary>
    /// Gets the appropriate AI scripts for a creature based on its resref.
    /// </summary>
    public static Dictionary<EventScriptType, string> GetAiScriptsForCreature(string resref)
    {
        return IsMageCreature(resref) ? MageAiScripts : StandardAiScripts;
    }
}

