using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Translates a <see cref="RewardMix"/> into concrete character rewards
/// (XP, gold, knowledge points, proficiency XP). Implementations are
/// responsible for interacting with the game engine or other subsystems.
/// </summary>
public interface IStageRewardGranter
{
    /// <summary>
    /// Grants the specified rewards to the character. Called when a quest stage
    /// with a non-empty <see cref="RewardMix"/> is completed.
    /// </summary>
    /// <param name="characterId">The character to receive the rewards.</param>
    /// <param name="questId">The quest that produced the rewards (for logging/auditing).</param>
    /// <param name="completedStageId">The stage that was completed (for logging/auditing).</param>
    /// <param name="rewards">The bundle of rewards to grant.</param>
    Task GrantRewardsAsync(CharacterId characterId, QuestId questId, int completedStageId, RewardMix rewards);
}
