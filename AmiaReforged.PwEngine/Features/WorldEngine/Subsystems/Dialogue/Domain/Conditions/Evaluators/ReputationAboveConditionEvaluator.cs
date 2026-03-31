using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions.Evaluators;

/// <summary>
/// Checks if the player's reputation with a faction is at or above a minimum threshold.
/// Parameters: factionId (required), minScore (required).
/// </summary>
[ServiceBinding(typeof(IDialogueConditionEvaluator))]
public sealed class ReputationAboveConditionEvaluator : IDialogueConditionEvaluator
{
    [Inject] private Lazy<CodexQueryService>? QueryService { get; init; }

    public DialogueConditionType Type => DialogueConditionType.ReputationAbove;

    public async Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId)
    {
        string? factionIdStr = condition.GetParam("factionId");
        string? minScoreStr = condition.GetParam("minScore");

        if (string.IsNullOrEmpty(factionIdStr) || string.IsNullOrEmpty(minScoreStr))
            return false;

        if (!int.TryParse(minScoreStr, out int minScore))
            return false;

        if (QueryService?.Value == null) return false;

        SharedKernel.CharacterId cid = SharedKernel.CharacterId.From(characterId);
        FactionId factionId = new FactionId(factionIdStr);

        FactionReputation? reputation = await QueryService.Value.GetReputationAsync(cid, factionId);

        // No reputation entry means neutral (0)
        int currentScore = reputation?.CurrentScore.Value ?? 0;
        return currentScore >= minScore;
    }
}
