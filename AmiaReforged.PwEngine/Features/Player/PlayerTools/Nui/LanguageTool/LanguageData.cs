namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.LanguageTool;

/// <summary>
/// Static data for character languages based on racial types and special conditions.
/// </summary>
public static class LanguageData
{
    /// <summary>
    /// All available languages that can be selected.
    /// </summary>
    public static readonly List<string> AllSelectableLanguages = new()
    {
        "Aboleth",
        "Abyssal",
        "Aglarondan",
        "Alzhedo",
        "Aquan",
        "Auran",
        "Celestial",
        "Chessentan",
        "Chondathan",
        "Chultan",
        "Damaran",
        "Dambrathan",
        "Draconic",
        "Drow",
        "Durpari",
        "Dwarven",
        "Elven",
        "Giant",
        "Gith",
        "Gnoll",
        "Gnomish",
        "Goblin",
        "Halruaan",
        "Hin",
        "Ignan",
        "Illuskan",
        "Infernal",
        "Kenku",
        "Kentaur",
        "Kozakuran",
        "Lantanese",
        "Midani",
        "Mulhorandi",
        "Nexalan",
        "Orcish",
        "Rashemi",
        "Shaaran",
        "Shou",
        "Slaadi",
        "Sylvan",
        "Tashalan",
        "Terran",
        "Thayan",
        "Tuigan",
        "Turmic",
        "Uluik",
        "Undercommon",
        "Untheric",
        "Waelan",
        "Yuan-Ti"
    };

    /// <summary>
    /// Maps racial types to their automatic languages.
    /// Key: Racial type ID, Value: List of automatic languages
    /// </summary>
    public static readonly Dictionary<int, List<string>> RacialAutomaticLanguages = new()
    {
        // Dwarf (0, 31)
        { 0, new List<string> { "Common", "Dwarven" } },
        { 31, new List<string> { "Common", "Dwarven" } },

        // Elf (1, 32, 34, 35)
        { 1, new List<string> { "Common", "Elven" } },
        { 32, new List<string> { "Common", "Elven" } },
        { 34, new List<string> { "Common", "Elven" } },
        { 35, new List<string> { "Common", "Elven" } },

        // Gnome (2)
        { 2, new List<string> { "Common", "Gnomish" } },

        // Halfling (3, 37, 40)
        { 3, new List<string> { "Common", "Hin" } },
        { 37, new List<string> { "Common", "Hin" } },
        { 40, new List<string> { "Common", "Hin" } },

        // Half Elf (4)
        { 4, new List<string> { "Common", "Elven" } },

        // Half Orc (5)
        { 5, new List<string> { "Common", "Orcish" } },

        // Human (6, 46, 47, 48, 49, 50, 51, 52, 53)
        { 6, new List<string> { "Common" } },
        { 46, new List<string> { "Common" } },
        { 47, new List<string> { "Common" } },
        { 48, new List<string> { "Common" } },
        { 49, new List<string> { "Common" } },
        { 50, new List<string> { "Common" } },
        { 51, new List<string> { "Common" } },
        { 52, new List<string> { "Common" } },
        { 53, new List<string> { "Common" } },

        // Construct (10)
        { 10, new List<string> { "Common" } },

        // Dragon (11)
        { 11, new List<string> { "Common", "Draconic" } },

        // Goblinoid (12, 38, 42, 55)
        { 12, new List<string> { "Common", "Undercommon", "Goblin" } },
        { 38, new List<string> { "Common", "Undercommon", "Goblin" } },
        { 42, new List<string> { "Common", "Undercommon", "Goblin" } },
        { 55, new List<string> { "Common", "Undercommon", "Goblin" } },

        // Monstrous Humanoid (13, 56)
        { 13, new List<string> { "Common", "Undercommon" } },
        { 56, new List<string> { "Common", "Undercommon" } },

        // Orc (14, 43, 44, 45)
        { 14, new List<string> { "Common", "Undercommon", "Orcish" } },
        { 43, new List<string> { "Common", "Undercommon", "Orcish" } },
        { 44, new List<string> { "Common", "Undercommon", "Orcish" } },
        { 45, new List<string> { "Common", "Undercommon", "Orcish" } },

        // Reptilian (15)
        { 15, new List<string> { "Common", "Undercommon" } },

        // Fey (17)
        { 17, new List<string> { "Common", "Sylvan", "Elven" } },

        // Giant (18)
        { 18, new List<string> { "Common", "Giant" } },

        // Outsider (20)
        { 20, new List<string> { "Common" } },

        // Shapechanger (23)
        { 23, new List<string> { "Common" } },

        // Undead (24)
        { 24, new List<string> { "Common" } },

        // Duergar (30)
        { 30, new List<string> { "Undercommon", "Common", "Dwarven" } },

        // Drow (33)
        { 33, new List<string> { "Undercommon", "Common", "Drow", "Drow Sign" } },

        // Svirfneblin (36)
        { 36, new List<string> { "Common", "Undercommon", "Gnomish" } },

        // Half Drow (41)
        { 41, new List<string> { "Common", "Undercommon", "Drow" } },

        // Elfling (54)
        { 54, new List<string> { "Common", "Elven", "Hin" } },

        // Kobold (39)
        { 39, new List<string> { "Common", "Undercommon", "Draconic" } }
    };

    /// <summary>
    /// Special languages that have requirements.
    /// </summary>
    public static class SpecialLanguages
    {
        public const string Druidic = "Druidic";
        public const string ThievesCant = "Thieves' Cant";
        public const string Thorass = "Thorass";
        public const string Loross = "Loross";
        public const string DrowSignLanguage = "Drow Sign";
    }
}

