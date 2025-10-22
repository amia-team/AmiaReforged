namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Effects;

/// <summary>
/// Defines the type of effect a trait can apply to a character.
/// </summary>
public enum TraitEffectType
{
    /// <summary>
    /// No mechanical effect (flavor trait).
    /// </summary>
    None = 0,

    /// <summary>
    /// Bonus or penalty to a skill.
    /// </summary>
    SkillModifier = 1,

    /// <summary>
    /// Bonus or penalty to an attribute (Strength, Dexterity, etc).
    /// </summary>
    AttributeModifier = 2,

    /// <summary>
    /// Grants knowledge points in a specific knowledge category.
    /// </summary>
    KnowledgePoints = 3,

    /// <summary>
    /// Custom scripted behavior (handled by trait-specific logic).
    /// </summary>
    Custom = 99
}
