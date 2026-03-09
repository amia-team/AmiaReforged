using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Default implementation of <see cref="IInteractionHandlerRegistry"/>.
/// All <see cref="IInteractionHandler"/> implementations are collected via Anvil DI
/// and indexed by their <see cref="IInteractionHandler.InteractionTag"/>.
/// </summary>
[ServiceBinding(typeof(IInteractionHandlerRegistry))]
public sealed class InteractionHandlerRegistry : IInteractionHandlerRegistry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, IInteractionHandler> _handlers;

    public InteractionHandlerRegistry(IEnumerable<IInteractionHandler> handlers)
    {
        _handlers = new Dictionary<string, IInteractionHandler>(StringComparer.OrdinalIgnoreCase);

        foreach (IInteractionHandler handler in handlers)
        {
            if (_handlers.TryAdd(handler.InteractionTag, handler))
            {
                Log.Info("Registered interaction handler '{Tag}' ({Type})",
                    handler.InteractionTag, handler.GetType().Name);
            }
            else
            {
                Log.Warn("Duplicate interaction handler for tag '{Tag}': {Type} ignored (kept {ExistingType})",
                    handler.InteractionTag, handler.GetType().Name,
                    _handlers[handler.InteractionTag].GetType().Name);
            }
        }

        Log.Info("Interaction handler registry initialized with {Count} handler(s)", _handlers.Count);
    }

    /// <inheritdoc />
    public IInteractionHandler? GetHandler(string interactionTag)
        => _handlers.GetValueOrDefault(interactionTag);

    /// <inheritdoc />
    public IReadOnlyCollection<IInteractionHandler> GetAll()
        => _handlers.Values;
}
