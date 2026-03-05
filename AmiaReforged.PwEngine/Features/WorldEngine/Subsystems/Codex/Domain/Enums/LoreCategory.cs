namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Represents the knowledge-skill category of a lore entry, based on D&amp;D 3.5e knowledge skills.
/// </summary>
public enum LoreCategory
{
    /// <summary>Knowledge: Arcana — mysteries, magic traditions, arcane symbols, constructs, dragons, magical beasts.</summary>
    Arcana = 0,

    /// <summary>Knowledge: Architecture &amp; Engineering — buildings, aqueducts, bridges, fortifications.</summary>
    ArchitectureAndEngineering = 1,

    /// <summary>Knowledge: Dungeoneering — aberrations, caverns, oozes, spelunking.</summary>
    Dungeoneering = 2,

    /// <summary>Knowledge: Geography — lands, terrain, climate, people.</summary>
    Geography = 3,

    /// <summary>Knowledge: History — wars, colonies, migrations, founding of cities.</summary>
    History = 4,

    /// <summary>Knowledge: Local — legends, personalities, inhabitants, laws, customs, traditions, humanoids.</summary>
    Local = 5,

    /// <summary>Knowledge: Nature — animals, fey, giants, monstrous humanoids, plants, seasons, cycles, weather, vermin.</summary>
    Nature = 6,

    /// <summary>Knowledge: Nobility &amp; Royalty — lineages, heraldry, family trees, mottos, personalities.</summary>
    NobilityAndRoyalty = 7,

    /// <summary>Knowledge: Religion — gods, mythic history, ecclesiastic tradition, holy symbols, undead.</summary>
    Religion = 8,

    /// <summary>Knowledge: The Planes — inner/outer planes, Astral, Ethereal, outsiders, planar magic.</summary>
    ThePlanes = 9,

    /// <summary>Out-of-character information (server rules, mechanics, meta).</summary>
    Ooc = 10
}

/// <summary>
/// Extension methods for <see cref="LoreCategory"/> display formatting.
/// </summary>
public static class LoreCategoryExtensions
{
    /// <summary>Returns a human-friendly display name for the category.</summary>
    public static string DisplayName(this LoreCategory category) => category switch
    {
        LoreCategory.Arcana => "Arcana",
        LoreCategory.ArchitectureAndEngineering => "Architecture & Engineering",
        LoreCategory.Dungeoneering => "Dungeoneering",
        LoreCategory.Geography => "Geography",
        LoreCategory.History => "History",
        LoreCategory.Local => "Local",
        LoreCategory.Nature => "Nature",
        LoreCategory.NobilityAndRoyalty => "Nobility & Royalty",
        LoreCategory.Religion => "Religion",
        LoreCategory.ThePlanes => "The Planes",
        LoreCategory.Ooc => "OOC",
        _ => category.ToString()
    };

    /// <summary>Returns a short sidebar-friendly label.</summary>
    public static string ShortLabel(this LoreCategory category) => category switch
    {
        LoreCategory.Arcana => "Arcana",
        LoreCategory.ArchitectureAndEngineering => "Arch & Eng",
        LoreCategory.Dungeoneering => "Dungeoneering",
        LoreCategory.Geography => "Geography",
        LoreCategory.History => "History",
        LoreCategory.Local => "Local",
        LoreCategory.Nature => "Nature",
        LoreCategory.NobilityAndRoyalty => "Nobility",
        LoreCategory.Religion => "Religion",
        LoreCategory.ThePlanes => "The Planes",
        LoreCategory.Ooc => "OOC",
        _ => category.ToString()
    };

    /// <summary>Returns a brief plain-English description of the knowledge domain.</summary>
    public static string Description(this LoreCategory category) => category switch
    {
        LoreCategory.Arcana => "Mysteries, magic traditions, arcane symbols, constructs, dragons, magical beasts",
        LoreCategory.ArchitectureAndEngineering => "Buildings, aqueducts, bridges, fortifications",
        LoreCategory.Dungeoneering => "Aberrations, caverns, oozes, spelunking",
        LoreCategory.Geography => "Lands, terrain, climate, people",
        LoreCategory.History => "Wars, colonies, migrations, founding of cities",
        LoreCategory.Local => "Legends, personalities, inhabitants, laws, customs, traditions",
        LoreCategory.Nature => "Animals, fey, giants, plants, seasons, cycles, weather",
        LoreCategory.NobilityAndRoyalty => "Lineages, heraldry, family trees, mottos, personalities",
        LoreCategory.Religion => "Gods, mythic history, ecclesiastic tradition, holy symbols, undead",
        LoreCategory.ThePlanes => "Inner/outer planes, Astral, Ethereal, outsiders, planar magic",
        LoreCategory.Ooc => "Server rules, mechanics, meta-game information",
        _ => string.Empty
    };
}
