namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// NWN damage type constants used for damage bonus effects.
/// Values match the NWScript DAMAGE_TYPE_* constants.
/// </summary>
public enum NwnDamageType
{
    Bludgeoning = 1,
    Piercing = 2,
    Slashing = 4,
    Magical = 8,
    Acid = 16,
    Cold = 32,
    Divine = 64,
    Electrical = 128,
    Fire = 256,
    Negative = 512,
    Positive = 1024,
    Sonic = 2048
}
