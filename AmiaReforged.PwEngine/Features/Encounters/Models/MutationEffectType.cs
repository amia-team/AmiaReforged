using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Types of effects that a mutation can apply to a creature.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MutationEffectType
{
    AbilityBonus = 0,
    ExtraAttack = 1,
    DamageBonus = 2,
    TempHP = 3,
    AC = 4,
    AttackBonus = 5,
    SpellResistance = 6,
    Concealment = 7,
    DamageShield = 8,
    Custom = 99
}
