#if SAILING_REGRESSION
using System.Reflection;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Sailing;
using Anvil.Services;

static class StartupChecks
{
    public static Func<Type, object> CreateResolver(Dictionary<Type, object> instances)
    {
        HashSet<Type> constructing = new();
        object Resolve(Type type)
        {
            if (instances.TryGetValue(type, out object? instance)) return instance;
            if (!constructing.Add(type)) throw new InvalidOperationException($"Startup dependency cycle: {type.Name}");
            ConstructorInfo constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            object?[] arguments = constructor.GetParameters().Select(p =>
                p.ParameterType.Namespace?.StartsWith("AmiaReforged.Core.Services.Sailing", StringComparison.Ordinal) == true ||
                instances.ContainsKey(p.ParameterType) ? Resolve(p.ParameterType) :
                p.HasDefaultValue ? p.DefaultValue : throw new InvalidOperationException($"Unexpected startup dependency: {p.ParameterType}")).ToArray();
            instance = constructor.Invoke(arguments);
            instances[type] = instance;
            constructing.Remove(type);
            return instance;
        }
        return Resolve;
    }

    public static void Verify(Action<bool, string> check)
    {
        // Construct the actual sailing graph without Anvil static injection or
        // any native module. Constructor-time API access would fail this test.
        Dictionary<Type, object> instances = new()
        {
            [typeof(EventService)] = new EventService([]),
            [typeof(DatabaseContextFactory)] = new DatabaseContextFactory(),
            [typeof(ShipPersistenceOptions)] = new ShipPersistenceOptions
                { Backend = SailingPersistenceBackend.Files, DataDirectory = null }
        };
        Func<Type, object> Resolve = CreateResolver(instances);

        Type[] services = typeof(HelmService).Assembly.GetTypes().Where(t =>
            t.Namespace?.StartsWith("AmiaReforged.Core.Services.Sailing", StringComparison.Ordinal) == true &&
            t.GetCustomAttributes<ServiceBindingAttribute>().Any()).ToArray();
        foreach (Type service in services)
        {
            Resolve(service);
            check(true, $"{service.Name} constructs without native module access.");
        }
        SailingBootstrapService bootstrap = (SailingBootstrapService)Resolve(typeof(SailingBootstrapService));
        ShipNavigationLoopService navigation = (ShipNavigationLoopService)Resolve(typeof(ShipNavigationLoopService));
        ShipRegistryService registry = (ShipRegistryService)Resolve(typeof(ShipRegistryService));
        check(!bootstrap.Initialization.IsCompleted && registry.GetShips().Count == 0,
            "Fleet registration and restoration wait for module load.");
        check(navigation.Completion.IsCompleted,
            "DI construction does not schedule a native navigation loop.");
        SailingFileStore files = (SailingFileStore)Resolve(typeof(SailingFileStore));
        bool deferredPath = false;
        try { _ = files.RootDirectory; } catch (InvalidOperationException) { deferredPath = true; }
        check(deferredPath, "Default file path resolution waits for native module readiness.");

        SailingModuleLifecycleService lifecycle = (SailingModuleLifecycleService)Resolve(typeof(SailingModuleLifecycleService));
        lifecycle.Dispose(); lifecycle.Dispose(); lifecycle.Init();
        check(true, "Disposed lifecycle cannot arm callbacks before module readiness.");
        foreach (IDisposable service in instances.Values.OfType<IDisposable>().Reverse())
        {
            service.Dispose(); service.Dispose();
            check(true, $"{service.GetType().Name} can be disposed twice before module load.");
            MethodInfo? initialize = service.GetType().GetMethod("InitializeModule", BindingFlags.Instance | BindingFlags.NonPublic);
            if (initialize?.GetParameters().Length == 1)
            {
                initialize.Invoke(service, [null]);
                check(true, $"{service.GetType().Name} cannot subscribe after disposal.");
            }
        }
        typeof(ShipNavigationLoopService).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(navigation, [Task.CompletedTask]);
        check(navigation.Completion.IsCompleted, "Disposed navigation cannot schedule a loop.");
        check(bootstrap.Initialization.IsCanceled, "Disposal releases pending startup waiters.");
    }
}
#endif
