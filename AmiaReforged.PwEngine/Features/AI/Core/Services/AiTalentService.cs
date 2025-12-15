using AmiaReforged.PwEngine.Features.AI.Core.Models;
using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Handles feat-based abilities (buffs, special attacks).
/// Ports logic from ds_ai_include.nss lines 688-760, 1393-1400.
/// </summary>
[ServiceBinding(typeof(AiTalentService))]
public class AiTalentService
{
    private readonly AiStateManager _stateManager;
    private readonly bool _isEnabled;

    public AiTalentService(AiStateManager stateManager)
    {
        _stateManager = stateManager;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    /// <summary>
    /// Attempts to apply feat buffs on spawn (one-time only).
    /// Port of DoFeatBuff() from ds_ai_include.nss lines 688-705.
    /// </summary>
    public bool TryUseFeatBuff(NwCreature creature)
    {
        if (!_isEnabled) return false;

        AiState state = _stateManager.GetOrCreateState(creature);
        if (state.HasFeatBuffed) return false;

        // Try feats in priority order
        if (SafeUseFeat(creature, creature, Feat.BarbarianRage))
        {
            state.HasFeatBuffed = true;
            return true;
        }

        if (SafeUseFeat(creature, creature, Feat.DivineShield))
        {
            state.HasFeatBuffed = true;
            return true;
        }

        if (SafeUseFeat(creature, creature, Feat.DivineMight))
        {
            state.HasFeatBuffed = true;
            return true;
        }

        if (SafeUseFeat(creature, creature, Feat.DwarvenDefenderDefensiveStance))
        {
            state.HasFeatBuffed = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to use a special attack feat (random selection).
    /// Port of DoSpecialAttack() from ds_ai_include.nss lines 743-760.
    /// </summary>
    public bool TrySpecialAttack(NwCreature creature, NwGameObject? target)
    {
        if (!_isEnabled) return false;
        if (target == null) return false;

        int roll = Random.Shared.Next(1, 13); // d12
        if (roll < 9) return false; // 67% chance to not use special attack

        Feat? feat = roll switch
        {
            9 => Feat.Knockdown,
            10 => Feat.CalledShot,
            11 => Feat.Disarm,
            12 => Feat.TurnUndead,
            _ => null
        };

        if (feat == null) return false;

        // Turn Undead only works on undead
        if (feat == Feat.TurnUndead && target is NwCreature targetCreature)
        {
            if (targetCreature.Race.RacialType != RacialType.Undead)
            {
                return false;
            }
        }

        return SafeUseFeat(creature, target, feat.Value);
    }

    /// <summary>
    /// Safely uses a feat if available and prepared.
    /// Port of SafeUseFeat() from ds_ai_include.nss lines 1393-1400.
    /// </summary>
    private bool SafeUseFeat(NwCreature creature, NwGameObject target, Feat feat)
    {
        if (!creature.KnowsFeat(feat)) return false;
        if (!creature.HasFeatPrepared(feat)) return false;

        creature.ActionUseFeat(feat, target);
        return true;
    }
}
