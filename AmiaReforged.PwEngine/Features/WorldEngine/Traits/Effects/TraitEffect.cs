namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Effects;

/// <summary>
/// Represents a single effect that a trait applies to a character.
/// Value object defining what mechanical benefit/penalty a trait provides.
/// </summary>
public record TraitEffect
{
    /// <summary>
    /// The type of effect this trait provides.
    /// </summary>
    public required TraitEffectType EffectType { get; init; }

    /// <summary>
    /// Target of the effect (e.g., skill name, attribute name, knowledge category).
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Magnitude of the effect (positive for bonus, negative for penalty).
    /// </summary>
    public int Magnitude { get; init; }

    /// <summary>
    /// Optional description of the effect for display purposes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Creates a skill modifier effect.
    /// </summary>
    public static TraitEffect SkillModifier(string skillName, int bonus) =>
        new()
        {
            EffectType = TraitEffectType.SkillModifier,
            Target = skillName,
            Magnitude = bonus,
            Description = $"{(bonus >= 0 ? "+" : "")}{bonus} to {skillName}"
        };

    /// <summary>
    /// Creates an attribute modifier effect.
    /// </summary>
    public static TraitEffect AttributeModifier(string attributeName, int bonus) =>
        new()
        {
            EffectType = TraitEffectType.AttributeModifier,
            Target = attributeName,
            Magnitude = bonus,
            Description = $"{(bonus >= 0 ? "+" : "")}{bonus} to {attributeName}"
        };

    /// <summary>
    /// Creates a knowledge points effect.
    /// </summary>
    public static TraitEffect KnowledgePoints(string category, int points) =>
        new()
        {
            EffectType = TraitEffectType.KnowledgePoints,
            Target = category,
            Magnitude = points,
            Description = $"{points} knowledge points in {category}"
        };
}
