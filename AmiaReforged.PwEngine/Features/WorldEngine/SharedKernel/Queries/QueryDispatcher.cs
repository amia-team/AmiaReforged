using System.Collections.Concurrent;
using System.Reflection;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Central query dispatcher implementation.
/// Routes queries to their handlers via Anvil DI auto-discovery.
/// </summary>
[ServiceBinding(typeof(IQueryDispatcher))]
public sealed class QueryDispatcher : IQueryDispatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ConcurrentDictionary<Type, HandlerInvocation> _handlerCache = new();

    // Cached handler invocation data
    private sealed class HandlerInvocation
    {
        public object Handler { get; }
        public MethodInfo HandleMethod { get; }
        public Type QueryType { get; }
        public Type ResultType { get; }

        public HandlerInvocation(object handler, MethodInfo handleMethod, Type queryType, Type resultType)
        {
            Handler = handler;
            HandleMethod = handleMethod;
            QueryType = queryType;
            ResultType = resultType;
        }
    }

    /// <summary>
    /// Initializes the query dispatcher and pre-caches all query handler metadata.
    /// Handlers are automatically discovered via Anvil DI using the marker interface pattern.
    /// </summary>
    /// <param name="queryHandlers">All query handlers discovered via IQueryHandlerMarker.</param>
    public QueryDispatcher(IEnumerable<IQueryHandlerMarker> queryHandlers)
    {
        DiscoverAndCacheHandlers(queryHandlers);
    }

    /// <inheritdoc />
    public async Task<TResult> DispatchAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        Type queryType = typeof(TQuery);
        string queryTypeName = queryType.Name;
        string resultTypeName = typeof(TResult).Name;

        Log.Debug("Dispatching query: {QueryType} -> {ResultType}", queryTypeName, resultTypeName);

        try
        {
            // Get cached handler invocation
            if (!_handlerCache.TryGetValue(queryType, out HandlerInvocation? invocation))
            {
                string errorMessage = $"No handler registered for query type: {queryTypeName}";
                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Invoke the cached handler method
            Task<TResult> resultTask;
            try
            {
                resultTask = (Task<TResult>)invocation.HandleMethod.Invoke(
                    invocation.Handler,
                    new object[] { query, cancellationToken })!;
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // Unwrap TargetInvocationException to get the real exception
                throw tie.InnerException;
            }

            TResult result = await resultTask.ConfigureAwait(false);

            Log.Debug("Query {QueryType} executed successfully", queryTypeName);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error dispatching query: {QueryType}", queryTypeName);
            throw;
        }
    }

    private void DiscoverAndCacheHandlers(IEnumerable<IQueryHandlerMarker> queryHandlers)
    {
        foreach (IQueryHandlerMarker handler in queryHandlers)
        {
            Type handlerType = handler.GetType();
            IEnumerable<Type> handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

            foreach (Type handlerInterface in handlerInterfaces)
            {
                Type[] genericArgs = handlerInterface.GetGenericArguments();
                Type queryType = genericArgs[0];
                Type resultType = genericArgs[1];

                // Get the specific HandleAsync method for this query type
                MethodInfo? handleMethod = handlerInterface.GetMethod("HandleAsync");

                if (handleMethod == null)
                {
                    Log.Warn("Could not find HandleAsync method on {HandlerType} for {QueryType}",
                        handlerType.Name,
                        queryType.Name);
                    continue;
                }

                // Cache the handler instance and its method
                HandlerInvocation invocation = new HandlerInvocation(handler, handleMethod, queryType, resultType);
                _handlerCache[queryType] = invocation;

                Log.Info("Registered and cached query handler {HandlerType} for query {QueryType} -> {ResultType}",
                    handlerType.Name,
                    queryType.Name,
                    resultType.Name);
            }
        }

        Log.Info("Query dispatcher initialized with {Count} query handler(s)", _handlerCache.Count);
    }
}

