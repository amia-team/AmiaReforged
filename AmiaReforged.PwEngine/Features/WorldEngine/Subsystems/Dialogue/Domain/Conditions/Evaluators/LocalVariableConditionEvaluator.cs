using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions.Evaluators;

/// <summary>
/// Checks if a local variable on the player's creature or the conversation NPC matches an expected value.
/// Parameters: variableName (required), expectedValue (required).
/// </summary>
[ServiceBinding(typeof(IDialogueConditionEvaluator))]
public sealed class LocalVariableConditionEvaluator : IDialogueConditionEvaluator
{
    public DialogueConditionType Type => DialogueConditionType.LocalVariable;

    public Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId)
    {
        string? variableName = condition.GetParam("variableName");
        string? expectedValue = condition.GetParam("expectedValue");

        if (string.IsNullOrEmpty(variableName) || expectedValue == null)
            return Task.FromResult(false);

        NwCreature? creature = player.LoginCreature;
        if (creature == null) return Task.FromResult(false);

        // Check player creature's local variables
        string? actualValue = creature.GetObjectVariable<LocalVariableString>(variableName).Value;
        if (actualValue == expectedValue) return Task.FromResult(true);

        // Also check as int
        if (int.TryParse(expectedValue, out int expectedInt))
        {
            int actualInt = creature.GetObjectVariable<LocalVariableInt>(variableName).Value;
            return Task.FromResult(actualInt == expectedInt);
        }

        return Task.FromResult(false);
    }
}
