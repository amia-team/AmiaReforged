using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Applies <see cref="SpawnBonus"/> effects to spawned NWN creatures.
/// This is a separate bonus layer that stacks alongside the legacy addon system
/// (Greater/Cagey/Retribution/Ghostly).
///
/// The Mutation chaos axis scales bonus magnitudes:
///   effectiveValue = baseValue * (1 + Mutation / 100)
/// So Mutation=0 gives the base value, Mutation=100 doubles it.
/// </summary>
[ServiceBinding(typeof(SpawnBonusApplicator))]
public class SpawnBonusApplicator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Applies all active bonuses to the creature, scaling by chaos mutation.
    /// </summary>
    public void ApplyBonuses(uint creature, IReadOnlyList<SpawnBonus> bonuses, ChaosState chaos)
    {
        foreach (SpawnBonus bonus in bonuses)
        {
            if (!bonus.IsActive) continue;

            try
            {
                int scaledValue = ScaleByMutation(bonus.Value, chaos.Mutation);
                ApplyBonus(creature, bonus, scaledValue);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to apply spawn bonus '{Name}' ({Type}) to creature.",
                    bonus.Name, bonus.Type);
            }
        }
    }

    private static int ScaleByMutation(int baseValue, int mutation)
    {
        // Mutation 0 = 1.0x, Mutation 100 = 2.0x
        double scale = 1.0 + mutation / 100.0;
        return (int)Math.Round(baseValue * scale);
    }

    private static void ApplyBonus(uint creature, SpawnBonus bonus, int value)
    {
        IntPtr effect = bonus.Type switch
        {
            SpawnBonusType.TempHP => NWScript.EffectTemporaryHitpoints(value),
            SpawnBonusType.AC => NWScript.EffectACIncrease(value),
            SpawnBonusType.DamageShield => NWScript.EffectDamageShield(value, 1, 1), // 1 = 1d4, 1 = bludgeoning
            SpawnBonusType.Concealment => NWScript.EffectConcealment(Math.Clamp(value, 0, 100)),
            SpawnBonusType.AttackBonus => NWScript.EffectAttackIncrease(value),
            SpawnBonusType.DamageBonus => NWScript.EffectDamageIncrease(value, NWScript.DAMAGE_TYPE_MAGICAL),
            SpawnBonusType.SpellResistance => NWScript.EffectSpellResistanceIncrease(value),
            SpawnBonusType.Custom => IntPtr.Zero, // No-op for custom type
            _ => IntPtr.Zero
        };

        if (effect == IntPtr.Zero) return;

        // DURATION_TYPE_PERMANENT = 1 for creature-lifetime buffs
        // DURATION_TYPE_TEMPORARY = 2 for timed buffs
        if (bonus.DurationSeconds <= 0)
        {
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, effect, creature);
        }
        else
        {
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, effect, creature,
                bonus.DurationSeconds);
        }

        Log.Trace("Applied bonus '{Name}' ({Type}, value={Value}) to creature {Creature}.",
            bonus.Name, bonus.Type, value, NWScript.GetName(creature));
    }
}
