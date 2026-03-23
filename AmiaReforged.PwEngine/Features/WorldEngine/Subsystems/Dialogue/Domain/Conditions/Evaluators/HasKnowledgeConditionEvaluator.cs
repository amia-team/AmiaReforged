using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions.Evaluators;

/// <summary>
/// Checks if the player has unlocked a specific knowledge/lore entry.
/// Parameters: loreId (required).
/// </summary>
[ServiceBinding(typeof(IDialogueConditionEvaluator))]
public sealed class HasKnowledgeConditionEvaluator : IDialogueConditionEvaluator
{
    [Inject] private Lazy<IWorldEngineFacade>? WorldEngine { get; init; }

    public DialogueConditionType Type => DialogueConditionType.HasKnowledge;

    public async Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId)
    {
        string? loreId = condition.GetParam("loreId");
        if (string.IsNullOrEmpty(loreId)) return false;

        if (WorldEngine?.Value?.Codex == null) return false;

        return await WorldEngine.Value.Codex.HasKnowledgeAsync(
            SharedKernel.CharacterId.From(characterId), loreId);
    }
}
