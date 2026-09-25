using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Service for managing knowledge point progression through the economy system.
/// Handles the accumulation of progression points, rollover into economy KP,
/// curve calculation, and cap enforcement.
/// </summary>
public interface IKnowledgeProgressionService
{
    /// <summary>
    /// Reads the character's progression record without creating or persisting anything.
    /// Returns <c>null</c> when the character has no progression row yet; initialization
    /// must happen on an explicit write path (award, level-up grant, registration, or a
    /// dedicated initialization command).
    /// </summary>
    KnowledgeProgression? GetProgression(CharacterId characterId);

    /// <summary>
    /// Awards progression points from crafting/economy activities.
    /// Accumulates points and rolls over into economy KP when threshold is reached.
    /// Respects soft cap (tedium multiplier) and hard cap (blocked).
    /// </summary>
    ProgressionResult AwardProgressionPoints(CharacterId characterId, int points);

    /// <summary>
    /// Grants a level-up knowledge point directly (bypasses the economy curve).
    /// Called when a character levels up.
    /// </summary>
    void GrantLevelUpKnowledgePoint(CharacterId characterId);

    /// <summary>
    /// Reads the effective soft cap for a character, considering their cap profile.
    /// Side-effect-free: reads the existing progression row (if any) and falls back to
    /// the configured default soft cap when no row exists.
    /// </summary>
    int GetEffectiveSoftCap(CharacterId characterId);

    /// <summary>
    /// Reads the effective hard cap for a character, considering their cap profile.
    /// Side-effect-free: reads the existing progression row (if any) and falls back to
    /// the configured default hard cap when no row exists.
    /// </summary>
    int GetEffectiveHardCap(CharacterId characterId);

    /// <summary>
    /// Reads the progression point cost for the character's next economy KP.
    /// Side-effect-free: reads the existing progression row (if any) and treats a
    /// missing row as zero economy KP earned.
    /// </summary>
    int GetProgressionCostForNextPoint(CharacterId characterId);

    /// <summary>
    /// Gets the current progression curve configuration.
    /// </summary>
    ProgressionCurveConfig GetCurveConfig();
}
