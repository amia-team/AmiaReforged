namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Extension methods for <see cref="TraitCategory"/> display formatting.
/// Works on the shared subsystem TraitCategory enum.
/// </summary>
public static class TraitCategoryExtensions
{
    /// <summary>Returns a human-friendly display name for the category.</summary>
    public static string DisplayName(this TraitCategory category) => category switch
    {
        TraitCategory.Background => "Background",
        TraitCategory.Personality => "Personality",
        TraitCategory.Physical => "Physical",
        TraitCategory.Mental => "Mental",
        TraitCategory.Social => "Social",
        TraitCategory.Supernatural => "Supernatural",
        TraitCategory.Curse => "Curse",
        TraitCategory.Blessing => "Blessing",
        _ => category.ToString()
    };

    /// <summary>Returns a short sidebar-friendly label (≤12 chars).</summary>
    public static string ShortLabel(this TraitCategory category) => category switch
    {
        TraitCategory.Background => "Background",
        TraitCategory.Personality => "Personality",
        TraitCategory.Physical => "Physical",
        TraitCategory.Mental => "Mental",
        TraitCategory.Social => "Social",
        TraitCategory.Supernatural => "Supernatur.",
        TraitCategory.Curse => "Curse",
        TraitCategory.Blessing => "Blessing",
        _ => category.ToString()
    };

    /// <summary>Returns a brief plain-English description of the trait domain.</summary>
    public static string Description(this TraitCategory category) => category switch
    {
        TraitCategory.Background => "Origin, upbringing, or past experiences",
        TraitCategory.Personality => "Temperament, habits, behavioural tendencies",
        TraitCategory.Physical => "Bodily traits — strength, agility, appearance",
        TraitCategory.Mental => "Cognitive traits — intellect, perception, willpower",
        TraitCategory.Social => "Charisma, reputation effects, social standing",
        TraitCategory.Supernatural => "Magic, planar influence, or divine favour",
        TraitCategory.Curse => "Negative afflictions — lycanthropy, vampirism, hexes",
        TraitCategory.Blessing => "Positive boons — divine blessings, fey gifts",
        _ => string.Empty
    };
}
