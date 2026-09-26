using System.Collections.Concurrent;
using System.Reflection;
using Anvil.Services;
using LightInject;
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

    private readonly ServiceContainer? _serviceContainer;
    private readonly ConcurrentDictionary<Type, HandlerInvocation> _handlerCache = new();

    private sealed class HandlerInvocation(
        Type? serviceType,
        string? serviceName,
        object? handler,
        Type handlerType,
        MethodInfo handleMethod,
        Type queryType,
        Type resultType)
    {
        public Type? ServiceType { get; } = serviceType;
        public string? ServiceName { get; } = serviceName;
        public object? Handler { get; } = handler;
        public Type HandlerType { get; } = handlerType;
        public MethodInfo HandleMethod { get; } = handleMethod;
        public Type QueryType { get; } = queryType;
        public Type ResultType { get; } = resultType;
    }

    /// <summary>
    /// Initializes the production query dispatcher and discovers handlers from
    /// the Anvil service container.
    /// </summary>
    public QueryDispatcher(IServiceManager serviceManager)
    {
        ArgumentNullException.ThrowIfNull(serviceManager);

        _serviceContainer = serviceManager.AnvilServiceContainer;

        DiscoverAndCacheHandlers();
    }

    /// <summary>
    /// Initializes a query dispatcher with directly supplied handlers.
    /// Intended for tests and local fixtures.
    /// </summary>
    /// <param name="queryHandlers">Query handler instances to register.</param>
    internal QueryDispatcher(IEnumerable<IQueryHandlerMarker> queryHandlers)
    {
        ArgumentNullException.ThrowIfNull(queryHandlers);

        _serviceContainer = null;

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

        Log.Debug(
            "Dispatching query: {QueryType} -> {ResultType}",
            queryTypeName,
            resultTypeName);

        try
        {
            if (!_handlerCache.TryGetValue(
                    queryType,
                    out HandlerInvocation? invocation))
            {
                string errorMessage =
                    $"No handler registered for query type: {queryTypeName}";

                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            object handler = ResolveHandler(invocation);

            Task<TResult> resultTask;

            try
            {
                resultTask =
                    (Task<TResult>)invocation.HandleMethod.Invoke(
                        handler,
                        new object[] { query, cancellationToken })!;
            }
            catch (TargetInvocationException tie)
                when (tie.InnerException != null)
            {
                throw tie.InnerException;
            }

            TResult result =
                await resultTask.ConfigureAwait(false);

            Log.Debug(
                "Query {QueryType} executed successfully",
                queryTypeName);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error dispatching query: {QueryType}",
                queryTypeName);

            throw;
        }
    }

    private void DiscoverAndCacheHandlers()
    {
        if (_serviceContainer == null)
        {
            throw new InvalidOperationException(
                "Cannot discover DI query handler registrations without an Anvil service container.");
        }

        IEnumerable<IGrouping<Type, ServiceRegistration>> handlerRegistrationGroups =
            _serviceContainer.AvailableServices
                .Where(registration =>
                    registration.ImplementingType != null &&
                    typeof(IQueryHandlerMarker).IsAssignableFrom(
                        registration.ImplementingType))
                .GroupBy(registration => registration.ImplementingType!)
                .OrderBy(
                    group => group.Key.FullName,
                    StringComparer.Ordinal);

        foreach (IGrouping<Type, ServiceRegistration> registrationGroup
                 in handlerRegistrationGroups)
        {
            CacheHandlerMetadata(
                registrationGroup.Key,
                registrationGroup.ToArray(),
                handler: null);
        }

        Log.Info(
            "Query dispatcher initialized with {Count} query handler registration(s)",
            _handlerCache.Count);
    }

    private void DiscoverAndCacheHandlers(
        IEnumerable<IQueryHandlerMarker> queryHandlers)
    {
        foreach (IQueryHandlerMarker handler in queryHandlers)
        {
            CacheHandlerMetadata(
                handler.GetType(),
                registrations: null,
                handler);
        }

        Log.Debug(
            "Query dispatcher initialized with {Count} directly supplied test handler(s)",
            _handlerCache.Count);
    }

    private void CacheHandlerMetadata(
        Type handlerType,
        IReadOnlyCollection<ServiceRegistration>? registrations,
        object? handler)
    {
        IEnumerable<Type> handlerInterfaces =
            handlerType.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() ==
                    typeof(IQueryHandler<,>));

        foreach (Type handlerInterface in handlerInterfaces)
        {
            Type[] genericArgs =
                handlerInterface.GetGenericArguments();

            Type queryType = genericArgs[0];
            Type resultType = genericArgs[1];

            MethodInfo? handleMethod =
                handlerInterface.GetMethod(
                    nameof(IQueryHandler<IQuery<object>, object>.HandleAsync));

            if (handleMethod == null)
            {
                Log.Warn(
                    "Could not find HandleAsync method on {HandlerType} for {QueryType}",
                    handlerType.Name,
                    queryType.Name);

                continue;
            }

            Type? serviceType = null;
            string? serviceName = null;

            if (handler == null)
            {
                ServiceRegistration? registration =
                    SelectBestRegistration(
                        registrations,
                        handlerInterface,
                        handlerType);

                if (registration == null)
                {
                    Log.Warn(
                        "Query handler {HandlerType} implements {HandlerInterface} " +
                        "but has no usable DI registration",
                        handlerType.Name,
                        handlerInterface.Name);

                    continue;
                }

                serviceType = registration.ServiceType;
                serviceName = registration.ServiceName;
            }

            HandlerInvocation invocation = new(
                serviceType,
                serviceName,
                handler,
                handlerType,
                handleMethod,
                queryType,
                resultType);

            if (_handlerCache.TryGetValue(
                    queryType,
                    out HandlerInvocation? existing))
            {
                Log.Warn(
                    "Multiple query handlers registered for {QueryType}: " +
                    "{ExistingHandler} and {NewHandler}. " +
                    "Using {NewHandler}.",
                    queryType.Name,
                    existing.HandlerType.Name,
                    handlerType.Name,
                    handlerType.Name);
            }

            _handlerCache[queryType] = invocation;

            Log.Info(
                "Registered query handler {HandlerType} for query {QueryType} -> {ResultType}",
                handlerType.Name,
                queryType.Name,
                resultType.Name);
        }
    }

    private static ServiceRegistration? SelectBestRegistration(
        IReadOnlyCollection<ServiceRegistration>? registrations,
        Type handlerInterface,
        Type handlerType)
    {
        if (registrations == null || registrations.Count == 0)
        {
            return null;
        }

        return registrations
            .OrderBy(registration =>
                GetRegistrationPriority(
                    registration,
                    handlerInterface,
                    handlerType))
            .ThenBy(
                registration => registration.ServiceName,
                StringComparer.Ordinal)
            .First();
    }

    private static int GetRegistrationPriority(
        ServiceRegistration registration,
        Type handlerInterface,
        Type handlerType)
    {
        // Prefer the exact strongly typed query-handler registration.
        if (registration.ServiceType == handlerInterface)
        {
            return 0;
        }

        // Then support explicit marker registrations used by older handlers.
        if (registration.ServiceType == typeof(IQueryHandlerMarker))
        {
            return 1;
        }

        // Concrete self-registration is also sufficient.
        if (registration.ServiceType == handlerType)
        {
            return 2;
        }

        // Any remaining registration for the implementing type can still
        // resolve the same handler instance.
        return 3;
    }

    private object ResolveHandler(HandlerInvocation invocation)
    {
        if (invocation.Handler != null)
        {
            return invocation.Handler;
        }

        if (_serviceContainer == null)
        {
            throw new InvalidOperationException(
                $"Handler {invocation.HandlerType.Name} has no service container available.");
        }

        if (invocation.ServiceType == null)
        {
            throw new InvalidOperationException(
                $"Handler {invocation.HandlerType.Name} has no resolvable service registration.");
        }

        if (string.IsNullOrWhiteSpace(invocation.ServiceName))
        {
            return _serviceContainer.GetInstance(
                invocation.ServiceType);
        }

        return _serviceContainer.GetInstance(
            invocation.ServiceType,
            invocation.ServiceName);
    }
}
