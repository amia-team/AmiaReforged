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
    /// Gets the progression record for a character, creating a default one if needed.
    /// </summary>
    KnowledgeProgression GetProgression(CharacterId characterId);

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
    /// Gets the effective soft cap for a character, considering their cap profile.
    /// </summary>
    int GetEffectiveSoftCap(CharacterId characterId);

    /// <summary>
    /// Gets the effective hard cap for a character, considering their cap profile.
    /// </summary>
    int GetEffectiveHardCap(CharacterId characterId);

    /// <summary>
    /// Gets the progression point cost for the character's next economy KP.
    /// </summary>
    int GetProgressionCostForNextPoint(CharacterId characterId);

    /// <summary>
    /// Gets the current progression curve configuration.
    /// </summary>
    ProgressionCurveConfig GetCurveConfig();
}
