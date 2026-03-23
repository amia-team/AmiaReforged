using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions.Evaluators;

/// <summary>
/// Checks if the player has a specific item (by tag) in their inventory.
/// Parameters: itemTag (required), count (optional, default 1).
/// </summary>
[ServiceBinding(typeof(IDialogueConditionEvaluator))]
public sealed class HasItemConditionEvaluator : IDialogueConditionEvaluator
{
    public DialogueConditionType Type => DialogueConditionType.HasItem;

    public Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId)
    {
        string? itemTag = condition.GetParam("itemTag");
        if (string.IsNullOrEmpty(itemTag)) return Task.FromResult(false);

        int requiredCount = int.TryParse(condition.GetParamOrDefault("count", "1"), out int c) ? c : 1;

        NwCreature? creature = player.LoginCreature;
        if (creature == null) return Task.FromResult(false);

        int actual = creature.Inventory.Items.Count(i => i.Tag == itemTag);
        return Task.FromResult(actual >= requiredCount);
    }
}
