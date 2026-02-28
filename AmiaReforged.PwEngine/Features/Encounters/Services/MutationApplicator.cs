using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

/// <summary>
/// Applies mutations to spawned creatures at encounter time.
///
/// Flow:
///   1. Gate check: roll percentile against ChaosState.Mutation axis.
///      If roll &gt; Mutation, no mutation — exit early.
///   2. Shuffle all active <see cref="MutationTemplate"/>s randomly.
///   3. Step through each template, rolling against its <see cref="MutationTemplate.SpawnChancePercent"/>.
///      The first to succeed wins.
///   4. Apply all active effects from the winning template to the creature.
///   5. Prepend the mutation prefix to the creature's name.///
/// When a <see cref="SpawnGroup"/> with <see cref="SpawnGroup.OverrideMutations"/> is provided,
/// only the group's <see cref="GroupMutationOverride"/>s are considered instead of the global pool,
/// and each override's <see cref="GroupMutationOverride.ChancePercent"/> is used in place of
/// the template's global <see cref="MutationTemplate.SpawnChancePercent"/>.
/// If overrides are enabled but none are defined, no mutation is ever applied./// </summary>
[ServiceBinding(typeof(MutationApplicator))]
public class MutationApplicator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Random Rng = new();

    private readonly IMutationRepository _repository;

    // Cached active templates — refreshed via RefreshCacheAsync()
    private List<MutationTemplate> _cachedTemplates = [];

    public MutationApplicator(IMutationRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Loads or refreshes the cached active mutation templates from the database.
    /// Call this at startup and after admin panel mutations.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        _cachedTemplates = await _repository.GetAllActiveAsync();
        Log.Info("Mutation template cache refreshed — {Count} active templates loaded.", _cachedTemplates.Count);
    }

    /// <summary>
    /// Attempts to apply a mutation to the creature based on the Mutation chaos axis.
    /// Returns true if a mutation was applied, false otherwise.
    /// </summary>
    public bool TryApplyMutation(uint creature, ChaosState chaos)
    {
        if (_cachedTemplates.Count == 0) return false;

        // Gate check: roll against the Mutation chaos axis
        int gateRoll = Rng.Next(100) + 1; // 1-100
        if (gateRoll > chaos.Mutation)
        {
            Log.Trace("Mutation gate failed: roll {Roll} > mutation axis {Axis}.", gateRoll, chaos.Mutation);
            return false;
        }

        // Shuffle templates randomly
        List<MutationTemplate> shuffled = _cachedTemplates
            .OrderBy(_ => Rng.Next())
            .ToList();

        // Step through each template — first to succeed wins
        foreach (MutationTemplate template in shuffled)
        {
            int templateRoll = Rng.Next(100) + 1; // 1-100
            if (templateRoll > template.SpawnChancePercent) continue;

            // This template wins — apply it
            ApplyMutation(creature, template, chaos);
            return true;
        }

        Log.Trace("All mutation template rolls failed for creature.");
        return false;
    }

    /// <summary>
    /// Group-aware mutation attempt. When the group has <see cref="SpawnGroup.OverrideMutations"/>
    /// enabled, only that group's overrides are used (with their custom chances). If no overrides
    /// are defined, no mutations are applied. When the flag is false, the global pool is used.
    /// </summary>
    public bool TryApplyMutation(uint creature, ChaosState chaos, SpawnGroup group)
    {
        if (!group.OverrideMutations)
        {
            // No override — use global mutation pool
            return TryApplyMutation(creature, chaos);
        }

        // Override enabled — use only the group's overrides
        if (group.MutationOverrides.Count == 0)
        {
            Log.Trace("Group '{Name}' overrides mutations but has none defined — skipping.", group.Name);
            return false;
        }

        // Gate check: roll against the Mutation chaos axis
        int gateRoll = Rng.Next(100) + 1; // 1-100
        if (gateRoll > chaos.Mutation)
        {
            Log.Trace("Mutation gate failed (group override): roll {Roll} > mutation axis {Axis}.",
                gateRoll, chaos.Mutation);
            return false;
        }

        // Shuffle overrides randomly
        List<GroupMutationOverride> shuffled = group.MutationOverrides
            .Where(o => o.MutationTemplate.IsActive)
            .OrderBy(_ => Rng.Next())
            .ToList();

        if (shuffled.Count == 0)
        {
            Log.Trace("Group '{Name}' has overrides but none reference active templates — skipping.",
                group.Name);
            return false;
        }

        // Step through each override — first to succeed wins (using override's ChancePercent)
        foreach (GroupMutationOverride ovr in shuffled)
        {
            int overrideRoll = Rng.Next(100) + 1; // 1-100
            if (overrideRoll > ovr.ChancePercent) continue;

            // This override wins — apply its template
            ApplyMutation(creature, ovr.MutationTemplate, chaos);
            return true;
        }

        Log.Trace("All group mutation override rolls failed for creature in group '{Name}'.", group.Name);
        return false;
    }

    private void ApplyMutation(uint creature, MutationTemplate template, ChaosState chaos)
    {
        // Prepend prefix to creature name
        string originalName = NWScript.GetName(creature);
        NWScript.SetName(creature, $"{template.Prefix} {originalName}");

        // Apply all active effects
        foreach (MutationEffect effect in template.Effects)
        {
            if (!effect.IsActive) continue;

            try
            {
                int scaledValue = ScaleByMutation(effect.Value, chaos.Mutation);
                ApplyEffect(creature, effect, scaledValue);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to apply mutation effect ({Type}) from template '{Prefix}'.",
                    effect.Type, template.Prefix);
            }
        }

        Log.Info("Mutation applied: '{Prefix}' to '{Name}' (template {Id}).",
            template.Prefix, NWScript.GetName(creature), template.Id);
    }

    private static int ScaleByMutation(int baseValue, int mutation)
    {
        // Same scaling as SpawnBonusApplicator: Mutation 0 = 1.0x, Mutation 100 = 2.0x
        double scale = 1.0 + mutation / 100.0;
        return (int)Math.Round(baseValue * scale);
    }

    private static void ApplyEffect(uint creature, MutationEffect effect, int value)
    {
        IntPtr nwnEffect = effect.Type switch
        {
            MutationEffectType.AbilityBonus when effect.AbilityType.HasValue =>
                NWScript.EffectAbilityIncrease((int)effect.AbilityType.Value, value),
            MutationEffectType.ExtraAttack =>
                NWScript.EffectModifyAttacks(Math.Clamp(value, 1, 5)),
            MutationEffectType.DamageBonus =>
                NWScript.EffectDamageIncrease(value, (int)(effect.DamageType ?? NwnDamageType.Magical)),
            MutationEffectType.TempHP =>
                NWScript.EffectTemporaryHitpoints(value),
            MutationEffectType.AC =>
                NWScript.EffectACIncrease(value),
            MutationEffectType.AttackBonus =>
                NWScript.EffectAttackIncrease(value),
            MutationEffectType.SpellResistance =>
                NWScript.EffectSpellResistanceIncrease(value),
            MutationEffectType.Concealment =>
                NWScript.EffectConcealment(Math.Clamp(value, 0, 100)),
            MutationEffectType.DamageShield =>
                NWScript.EffectDamageShield(value, 1, (int)(effect.DamageType ?? NwnDamageType.Magical)),
            _ => IntPtr.Zero
        };

        if (nwnEffect == IntPtr.Zero) return;

        if (effect.DurationSeconds <= 0)
        {
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, nwnEffect, creature);
        }
        else
        {
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, nwnEffect, creature,
                effect.DurationSeconds);
        }
    }
}
