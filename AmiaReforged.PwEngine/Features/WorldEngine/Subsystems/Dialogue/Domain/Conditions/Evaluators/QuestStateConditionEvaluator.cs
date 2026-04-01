using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions.Evaluators;

/// <summary>
/// Checks if a quest is in a specific state for the player.
/// Parameters: questId (required), requiredState (required: Discovered/InProgress/Completed/Failed/Abandoned).
/// </summary>
[ServiceBinding(typeof(IDialogueConditionEvaluator))]
public sealed class QuestStateConditionEvaluator : IDialogueConditionEvaluator
{
    [Inject] private Lazy<CodexQueryService>? QueryService { get; init; }

    public DialogueConditionType Type => DialogueConditionType.QuestState;

    public async Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId)
    {
        string? questId = condition.GetParam("questId");
        string? requiredStateName = condition.GetParam("requiredState");

        if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(requiredStateName))
            return false;

        if (!Enum.TryParse<QuestState>(requiredStateName, true, out QuestState requiredState))
            return false;

        if (QueryService?.Value == null) return false;

        SharedKernel.CharacterId cid = SharedKernel.CharacterId.From(characterId);
        IReadOnlyList<CodexQuestEntry> quests = await QueryService.Value.GetAllQuestsAsync(cid);

        CodexQuestEntry? quest = quests.FirstOrDefault(q => q.QuestId == new QuestId(questId));
        return quest?.EffectiveState == requiredState;
    }
}
