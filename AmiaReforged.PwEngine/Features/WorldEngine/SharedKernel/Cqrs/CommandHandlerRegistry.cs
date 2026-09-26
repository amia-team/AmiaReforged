using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;

/// <summary>
/// Runtime routing table for command handlers.
/// Stores handler resolvers so handlers remain owned by the DI container and are
/// resolved only when a command is dispatched.
/// </summary>
public sealed class CommandHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, HandlerRegistration> _handlers = new();

    private sealed record HandlerRegistration(
        Type HandlerInterface,
        Type HandlerType,
        Func<object> Resolver);

    /// <summary>
    /// Registers an already-created handler instance.
    /// Primarily useful for tests and explicit/manual composition.
    /// </summary>
    public void Register<TCommand>(ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);

        Register(
            typeof(TCommand),
            typeof(ICommandHandler<TCommand>),
            handler.GetType(),
            () => handler);
    }

    internal void Register(
        Type commandType,
        Type handlerInterface,
        Type handlerType,
        Func<object> resolver)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(handlerInterface);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(resolver);

        HandlerRegistration registration =
            new(handlerInterface, handlerType, resolver);

        if (_handlers.TryAdd(commandType, registration))
        {
            return;
        }

        HandlerRegistration existing = _handlers[commandType];

        if (existing.HandlerType == handlerType)
        {
            // The same implementation may appear under both its strongly typed
            // handler service and a legacy marker registration. Treat that as
            // one route rather than a conflict.
            return;
        }

        throw new InvalidOperationException(
            $"Multiple command handlers are registered for {commandType.FullName}: " +
            $"{existing.HandlerType.FullName} and {handlerType.FullName}.");
    }

    public bool TryResolve<TCommand>(out ICommandHandler<TCommand>? handler)
        where TCommand : ICommand
    {
        if (!_handlers.TryGetValue(
                typeof(TCommand),
                out HandlerRegistration? registration))
        {
            handler = null;
            return false;
        }

        object resolved = registration.Resolver();

        if (resolved is not ICommandHandler<TCommand> typedHandler)
        {
            throw new InvalidOperationException(
                $"Registered handler {registration.HandlerType.FullName} for " +
                $"{typeof(TCommand).FullName} resolved as {resolved.GetType().FullName}, " +
                $"which does not implement {registration.HandlerInterface.FullName}.");
        }

        handler = typedHandler;
        return true;
    }

    public ICommandHandler<TCommand> Resolve<TCommand>()
        where TCommand : ICommand
    {
        if (TryResolve<TCommand>(out ICommandHandler<TCommand>? handler))
        {
            return handler;
        }

        throw new InvalidOperationException(
            $"No handler registered for command type: {typeof(TCommand).Name}");
    }

    public int Count => _handlers.Count;
}
