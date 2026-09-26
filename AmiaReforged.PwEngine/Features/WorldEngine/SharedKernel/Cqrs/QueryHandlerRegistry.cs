using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;

/// <summary>
/// Runtime routing table for query handlers.
/// Stores handler resolvers so handlers remain owned by the DI container and are
/// resolved only when a query is dispatched.
/// </summary>
public sealed class QueryHandlerRegistry
{
    private readonly ConcurrentDictionary<Type, HandlerRegistration> _handlers = new();

    private sealed record HandlerRegistration(
        Type HandlerInterface,
        Type HandlerType,
        Type ResultType,
        Func<object> Resolver);

    /// <summary>
    /// Registers an already-created handler instance.
    /// Primarily useful for tests and explicit/manual composition.
    /// </summary>
    public void Register<TQuery, TResult>(
        IQueryHandler<TQuery, TResult> handler)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(handler);

        Register(
            typeof(TQuery),
            typeof(TResult),
            typeof(IQueryHandler<TQuery, TResult>),
            handler.GetType(),
            () => handler);
    }

    internal void Register(
        Type queryType,
        Type resultType,
        Type handlerInterface,
        Type handlerType,
        Func<object> resolver)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentNullException.ThrowIfNull(handlerInterface);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(resolver);

        HandlerRegistration registration =
            new(handlerInterface, handlerType, resultType, resolver);

        if (_handlers.TryAdd(queryType, registration))
        {
            return;
        }

        HandlerRegistration existing = _handlers[queryType];

        if (existing.HandlerType == handlerType &&
            existing.ResultType == resultType)
        {
            // The same implementation may appear under both its strongly typed
            // handler service and a legacy marker registration. Treat that as
            // one route rather than a conflict.
            return;
        }

        throw new InvalidOperationException(
            $"Multiple query handlers are registered for {queryType.FullName}: " +
            $"{existing.HandlerType.FullName} ({existing.ResultType.FullName}) and " +
            $"{handlerType.FullName} ({resultType.FullName}).");
    }

    public bool TryResolve<TQuery, TResult>(
        out IQueryHandler<TQuery, TResult>? handler)
        where TQuery : IQuery<TResult>
    {
        if (!_handlers.TryGetValue(
                typeof(TQuery),
                out HandlerRegistration? registration))
        {
            handler = null;
            return false;
        }

        if (registration.ResultType != typeof(TResult))
        {
            throw new InvalidOperationException(
                $"Query {typeof(TQuery).FullName} is registered with result type " +
                $"{registration.ResultType.FullName}, but dispatch requested " +
                $"{typeof(TResult).FullName}.");
        }

        object resolved = registration.Resolver();

        if (resolved is not IQueryHandler<TQuery, TResult> typedHandler)
        {
            throw new InvalidOperationException(
                $"Registered handler {registration.HandlerType.FullName} for " +
                $"{typeof(TQuery).FullName} resolved as {resolved.GetType().FullName}, " +
                $"which does not implement {registration.HandlerInterface.FullName}.");
        }

        handler = typedHandler;
        return true;
    }

    public IQueryHandler<TQuery, TResult> Resolve<TQuery, TResult>()
        where TQuery : IQuery<TResult>
    {
        if (TryResolve<TQuery, TResult>(
                out IQueryHandler<TQuery, TResult>? handler))
        {
            return handler;
        }

        throw new InvalidOperationException(
            $"No handler registered for query type: {typeof(TQuery).Name}");
    }

    public int Count => _handlers.Count;
}
