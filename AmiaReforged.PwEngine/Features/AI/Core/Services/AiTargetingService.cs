using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Handles target selection, validation, and detection logic for AI creatures.
/// Ports logic from ds_ai_include.nss lines 301-548.
/// </summary>
[ServiceBinding(typeof(AiTargetingService))]
public class AiTargetingService
{
    private readonly bool _isEnabled;

    public AiTargetingService()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    /// <summary>
    /// Gets a valid target for the creature, considering the current target.
    /// Implements attention span logic and target switching.
    /// Port of GetTarget() from ds_ai_include.nss lines 301-358.
    /// </summary>
    public NwGameObject? GetValidTarget(NwCreature creature, NwGameObject? currentTarget)
    {
        if (!_isEnabled) return null;

        // Validate current target
        TargetValidity currentValidity = TargetValidity.Invalid;
        if (currentTarget is NwCreature currentCreature)
        {
            currentValidity = ValidateTarget(creature, currentCreature);
        }
        else
        {
            return currentTarget;
        }


        // Option 1: Current target is valid and hostile
        if (currentValidity > TargetValidity.NotHostile)
        {
            // Check attention span
            int attentionSpan = 8 + (int)currentValidity;

            // PCs and possessed familiars have shorter attention span (critters prefer them)
            if (currentCreature != null &&
                (currentCreature.IsPlayerControlled || currentCreature.IsPossessedFamiliar))
            {
                attentionSpan = 4;
            }

            // d12 roll to maintain attention
            if (Random.Shared.Next(1, 13) <= attentionSpan)
            {
                return currentTarget; // Keep current target
            }
        }

        // Option 2: Find new target
        NwCreature? nearestEnemy = creature.GetNearestCreatures(CreatureTypeFilter.Reputation(ReputationType.Enemy)).FirstOrDefault();
        if (nearestEnemy != null && ValidateTarget(creature, nearestEnemy) > TargetValidity.NotHostile)
        {
            return nearestEnemy;
        }

        // Option 3: Keep current target if it's still somewhat valid
        if (currentValidity > TargetValidity.Invalid)
        {
            return currentTarget;
        }

        // Option 4: Try to find any suitable target
        return FindNearestEnemy(creature);
    }

    /// <summary>
    /// Validates if a target is hostile and detectable.
    /// Port of GetIsValidHostile() from ds_ai_include.nss lines 426-474.
    /// </summary>
    public TargetValidity ValidateTarget(NwCreature creature, NwCreature target)
    {
        if (!_isEnabled) return TargetValidity.Invalid;

        if (target == null) return TargetValidity.Error;

        // Check for invalid states
        if (target.PlotFlag || target.IsDead || target.Immortal)
        {
            return TargetValidity.Invalid;
        }

        // Check reputation
        if (!target.IsEnemy(creature))
        {
            return TargetValidity.NotHostile;
        }

        // Check if seen
        if (creature.IsCreatureSeen(target))
        {
            return TargetValidity.Seen;
        }

        // Check detectability (not seen but maybe heard/perceived)
        if (IsDetectable(creature, target))
        {
            return TargetValidity.Heard;
        }

        return TargetValidity.Undetectable;
    }

    /// <summary>
    /// Checks if a target is detectable by the creature.
    /// Port of GetIsDetectable() from ds_ai_include.nss lines 476-548.
    /// </summary>
    public bool IsDetectable(NwCreature creature, NwCreature target)
    {
        if (!_isEnabled) return false;
        if (target == null) return false;

        // Check for Greater Sanctuary (GS)
        if (target.ActiveEffects.Any(e => e.EffectType == EffectType.Sanctuary))
        {
            return false;
        }

        // Check for invisibility
        if (target.ActiveEffects.Any(e => e.EffectType == EffectType.Invisibility))
        {
            float distance = creature.Distance(target);

            // Can detect invisible targets at close range (<6.0m)
            if (distance > 0.0f && distance < 6.0f)
            {
                return true;
            }

            return false;
        }

        // Check if the object has been perceived
        string perceptionVar = $"ds_ai_p{target.Name.Substring(0, Math.Min(12, target.Name.Length))}";
        int hasPerceived = creature.GetObjectVariable<LocalVariableInt>(perceptionVar).Value;

        return hasPerceived == 1;
    }

    /// <summary>
    /// Finds a random enemy from nearby creatures.
    /// Port of FindSingleTarget() from ds_ai_include.nss lines 360-424.
    /// </summary>
    public NwCreature? FindNearestEnemy(NwCreature creature, float radius = 30.0f)
    {
        if (!_isEnabled) return null;

        var nearbyTargets = new List<NwCreature>();

        // Find up to 3 valid hostile targets
        foreach (var nearby in creature.GetNearestCreatures())
        {
            if (nearbyTargets.Count >= 3) break;

            float distance = creature.Distance(nearby);
            if (distance > radius) break;

            if (ValidateTarget(creature, nearby) > TargetValidity.NotHostile)
            {
                nearbyTargets.Add(nearby);
            }
        }

        // Return random target from the list
        if (nearbyTargets.Count == 0) return null;

        int randomIndex = Random.Shared.Next(nearbyTargets.Count);
        return nearbyTargets[randomIndex];
    }
}

