using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
using LightInject;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;

/// <summary>
/// Central CQRS routing registry.
///
/// Production construction inspects Anvil/LightInject registrations once and
/// records lazy resolver routes. Handler instances are not eagerly constructed.
/// Dispatchers consume this registry and no longer perform DI discovery or
/// reflection themselves.
/// </summary>
[ServiceBinding(typeof(CqrsRegistry))]
public sealed class CqrsRegistry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ServiceContainer? _serviceContainer;

    public CommandHandlerRegistry Commands { get; } = new();
    public QueryHandlerRegistry Queries { get; } = new();

    /// <summary>
    /// Production constructor used by Anvil DI.
    /// </summary>
    public CqrsRegistry(IServiceManager serviceManager)
    {
        ArgumentNullException.ThrowIfNull(serviceManager);

        _serviceContainer = serviceManager.AnvilServiceContainer;

        DiscoverHandlerRegistrations();

        Log.Info(
            "CQRS registry initialized with {CommandCount} command route(s) and {QueryCount} query route(s)",
            Commands.Count,
            Queries.Count);
    }

    /// <summary>
    /// Creates an empty registry for tests and explicit/manual composition.
    /// </summary>
    internal CqrsRegistry()
    {
        _serviceContainer = null;
    }

    /// <summary>
    /// Registers every ICommandHandler&lt;TCommand&gt; implemented by a supplied
    /// handler instance. Supports test handlers that implement more than one
    /// closed command-handler interface.
    /// </summary>
    public void Register(ICommandHandlerMarker handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        Type handlerType = handler.GetType();

        foreach (Type handlerInterface in GetCommandHandlerInterfaces(handlerType))
        {
            Type commandType = handlerInterface.GetGenericArguments()[0];

            Commands.Register(
                commandType,
                handlerInterface,
                handlerType,
                () => handler);
        }
    }

    /// <summary>
    /// Registers every IQueryHandler&lt;TQuery, TResult&gt; implemented by a
    /// supplied handler instance. Supports test handlers that implement more
    /// than one closed query-handler interface.
    /// </summary>
    public void Register(IQueryHandlerMarker handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        Type handlerType = handler.GetType();

        foreach (Type handlerInterface in GetQueryHandlerInterfaces(handlerType))
        {
            Type[] genericArguments = handlerInterface.GetGenericArguments();

            Queries.Register(
                genericArguments[0],
                genericArguments[1],
                handlerInterface,
                handlerType,
                () => handler);
        }
    }

    private void DiscoverHandlerRegistrations()
    {
        if (_serviceContainer == null)
        {
            throw new InvalidOperationException(
                "Cannot discover CQRS handler registrations without an Anvil service container.");
        }

        IEnumerable<IGrouping<Type, ServiceRegistration>> handlerGroups =
            _serviceContainer.AvailableServices
                .Where(registration =>
                    registration.ImplementingType != null &&
                    IsCqrsHandler(registration.ImplementingType))
                .GroupBy(registration => registration.ImplementingType!)
                .OrderBy(
                    group => group.Key.FullName ?? group.Key.Name,
                    StringComparer.Ordinal);

        foreach (IGrouping<Type, ServiceRegistration> group in handlerGroups)
        {
            Type handlerType = group.Key;
            ServiceRegistration[] registrations = group.ToArray();

            RegisterCommandRoutes(handlerType, registrations);
            RegisterQueryRoutes(handlerType, registrations);
        }
    }

    private void RegisterCommandRoutes(
        Type handlerType,
        IReadOnlyCollection<ServiceRegistration> registrations)
    {
        foreach (Type handlerInterface in GetCommandHandlerInterfaces(handlerType))
        {
            ServiceRegistration? registration =
                SelectBestRegistration(
                    registrations,
                    handlerInterface,
                    handlerType,
                    typeof(ICommandHandlerMarker));

            if (registration == null)
            {
                Log.Warn(
                    "Command handler {HandlerType} implements {HandlerInterface} but has no usable DI registration",
                    handlerType.Name,
                    handlerInterface.Name);
                continue;
            }

            Type commandType = handlerInterface.GetGenericArguments()[0];

            Commands.Register(
                commandType,
                handlerInterface,
                handlerType,
                CreateResolver(registration));
        }
    }

    private void RegisterQueryRoutes(
        Type handlerType,
        IReadOnlyCollection<ServiceRegistration> registrations)
    {
        foreach (Type handlerInterface in GetQueryHandlerInterfaces(handlerType))
        {
            ServiceRegistration? registration =
                SelectBestRegistration(
                    registrations,
                    handlerInterface,
                    handlerType,
                    typeof(IQueryHandlerMarker));

            if (registration == null)
            {
                Log.Warn(
                    "Query handler {HandlerType} implements {HandlerInterface} but has no usable DI registration",
                    handlerType.Name,
                    handlerInterface.Name);
                continue;
            }

            Type[] genericArguments = handlerInterface.GetGenericArguments();

            Queries.Register(
                genericArguments[0],
                genericArguments[1],
                handlerInterface,
                handlerType,
                CreateResolver(registration));
        }
    }

    private Func<object> CreateResolver(ServiceRegistration registration)
    {
        if (_serviceContainer == null)
        {
            throw new InvalidOperationException(
                "Cannot create a handler resolver without an Anvil service container.");
        }

        Type serviceType = registration.ServiceType;
        string? serviceName = registration.ServiceName;

        return () =>
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return _serviceContainer.GetInstance(serviceType);
            }

            return _serviceContainer.GetInstance(serviceType, serviceName);
        };
    }

    private static ServiceRegistration? SelectBestRegistration(
        IReadOnlyCollection<ServiceRegistration> registrations,
        Type handlerInterface,
        Type handlerType,
        Type markerType)
    {
        if (registrations.Count == 0)
        {
            return null;
        }

        return registrations
            .OrderBy(registration =>
                GetRegistrationPriority(
                    registration,
                    handlerInterface,
                    handlerType,
                    markerType))
            .ThenBy(
                registration => registration.ServiceName,
                StringComparer.Ordinal)
            .First();
    }

    private static int GetRegistrationPriority(
        ServiceRegistration registration,
        Type handlerInterface,
        Type handlerType,
        Type markerType)
    {
        // Prefer the exact strongly typed handler registration.
        if (registration.ServiceType == handlerInterface)
        {
            return 0;
        }

        // Preserve compatibility with explicit legacy marker registrations.
        if (registration.ServiceType == markerType)
        {
            return 1;
        }

        // Concrete self-registration is also a valid resolution path.
        if (registration.ServiceType == handlerType)
        {
            return 2;
        }

        // Any remaining registration for the same implementation is usable.
        return 3;
    }

    private static bool IsCqrsHandler(Type type) =>
        typeof(ICommandHandlerMarker).IsAssignableFrom(type) ||
        typeof(IQueryHandlerMarker).IsAssignableFrom(type);

    private static IEnumerable<Type> GetCommandHandlerInterfaces(Type handlerType) =>
        handlerType.GetInterfaces()
            .Where(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() ==
                typeof(ICommandHandler<>));

    private static IEnumerable<Type> GetQueryHandlerInterfaces(Type handlerType) =>
        handlerType.GetInterfaces()
            .Where(interfaceType =>
                interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() ==
                typeof(IQueryHandler<,>));
}
