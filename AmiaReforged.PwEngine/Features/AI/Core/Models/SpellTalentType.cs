namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Spells can be classified into talent categories, used in the creature AI.
/// </summary>
public enum SpellTalentType
{
    Unknown = 0,
    Attack = 1,
    Buff = 2,
    Heal = 3,
    Summon = 4,
    Dispel = 5,
    PersistentAoe = 6
}
