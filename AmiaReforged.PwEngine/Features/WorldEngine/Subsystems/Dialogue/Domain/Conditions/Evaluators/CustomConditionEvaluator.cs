using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions.Evaluators;

/// <summary>
/// Evaluates Custom dialogue conditions by dispatching to registered
/// <see cref="ICustomConditionHandler"/> implementations.
/// Parameters: handlerName (required), plus any additional key-value pairs consumed by the handler.
/// </summary>
[ServiceBinding(typeof(IDialogueConditionEvaluator))]
public sealed class CustomConditionEvaluator : IDialogueConditionEvaluator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, ICustomConditionHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public CustomConditionEvaluator(IEnumerable<ICustomConditionHandler> handlers)
    {
        foreach (ICustomConditionHandler handler in handlers)
        {
            _handlers[handler.HandlerName] = handler;
            Log.Debug("Registered custom condition handler: {HandlerName}", handler.HandlerName);
        }
    }

    public DialogueConditionType Type => DialogueConditionType.Custom;

    public async Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId)
    {
        string? handlerName = condition.GetParam("handlerName");
        if (string.IsNullOrEmpty(handlerName))
        {
            Log.Warn("Custom condition missing required 'handlerName' parameter");
            return false;
        }

        if (!_handlers.TryGetValue(handlerName, out ICustomConditionHandler? handler))
        {
            Log.Warn("No custom condition handler registered for: {HandlerName}", handlerName);
            return false; // Fail closed
        }

        return await handler.EvaluateAsync(condition, player, characterId);
    }
}
