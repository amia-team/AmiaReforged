using System.Reflection;
using System.Text;
using Anvil.Services;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests;

/// <summary>
/// Statically validates the Anvil service dependency graph without starting NWN
/// or constructing any services.
///
/// This catches circular construction dependencies such as:
///
/// IEventBus
///   -> AnvilEventBusService
///   -> IEnumerable<IEventHandlerMarker>
///   -> DialogueNodeEnteredEventHandler
///   -> QuestObjectiveResolutionService
///   -> IEventBus
///
/// Lazy&lt;T&gt; and Func&lt;T&gt; are intentionally treated as deferred dependencies
/// and therefore do not contribute construction-time edges.
/// </summary>
[TestFixture]
public sealed class AnvilServiceDependencyCycleTests
{
    private const string ApplicationAssemblyPrefix = "AmiaReforged.";

    /// <summary>
    /// Safety valve against pathological graphs containing a combinatorial
    /// number of simple cycles.
    ///
    /// In normal operation we should never come remotely close to this.
    /// </summary>
    private const int MaxReportedCycles = 512;

    [Test]
    public void Anvil_service_graph_has_no_eager_dependency_cycles()
    {
        IReadOnlyList<Assembly> assemblies = LoadApplicationAssemblies();

        ServiceGraph graph = BuildServiceGraph(assemblies);

        HashSet<Type> reachableServices = FindReachableServices(graph);

        CycleSearchResult cycleResult =
            FindAllDependencyCycles(graph, reachableServices);

        if (cycleResult.Cycles.Count == 0)
        {
            return;
        }

        Assert.Fail(BuildFailureMessage(
            graph,
            cycleResult.Cycles,
            cycleResult.Truncated));
    }

    private static ServiceGraph BuildServiceGraph(
        IReadOnlyList<Assembly> assemblies)
    {
        List<ServiceDefinition> services = assemblies
            .SelectMany(GetLoadableTypes)
            .Where(IsConcreteType)
            .Select(CreateServiceDefinition)
            .Where(definition => definition.Bindings.Count > 0)
            .OrderBy(definition => TypeName(definition.ImplementationType))
            .ToList();

        Dictionary<Type, List<Type>> implementationsByContract =
            BuildRegistrationMap(services);

        Dictionary<Type, List<DependencyEdge>> edges = services
            .ToDictionary(
                definition => definition.ImplementationType,
                _ => new List<DependencyEdge>());

        foreach (ServiceDefinition service in services)
        {
            AddConstructorDependencies(
                service.ImplementationType,
                implementationsByContract,
                edges);

            AddInjectedPropertyDependencies(
                service.ImplementationType,
                implementationsByContract,
                edges);
        }

        foreach (List<DependencyEdge> dependencyEdges in edges.Values)
        {
            dependencyEdges.Sort(CompareEdges);
        }

        HashSet<Type> eagerRoots = services
            .Where(service => !service.IsLazyService)
            .Select(service => service.ImplementationType)
            .ToHashSet();

        return new ServiceGraph(
            edges,
            eagerRoots,
            implementationsByContract);
    }

    private static ServiceDefinition CreateServiceDefinition(Type type)
    {
        ServiceBindingAttribute[] bindings = type
            .GetCustomAttributes<ServiceBindingAttribute>(inherit: true)
            .ToArray();

        Type[] contracts = bindings
            .Select(binding => binding.BindFrom)
            .Distinct()
            .ToArray();

        return new ServiceDefinition(
            type,
            contracts,
            IsLazyService(type),
            GetBindingPriority(type));
    }

    private static Dictionary<Type, List<Type>> BuildRegistrationMap(
        IReadOnlyList<ServiceDefinition> services)
    {
        Dictionary<Type, List<Type>> result = new();

        foreach (ServiceDefinition service in services)
        {
            foreach (Type contract in service.Bindings)
            {
                if (!result.TryGetValue(contract, out List<Type>? implementations))
                {
                    implementations = new List<Type>();
                    result.Add(contract, implementations);
                }

                if (!implementations.Contains(service.ImplementationType))
                {
                    implementations.Add(service.ImplementationType);
                }
            }
        }

        foreach (List<Type> implementations in result.Values)
        {
            implementations.Sort((left, right) =>
            {
                int priorityComparison =
                    GetBindingPriority(left).CompareTo(GetBindingPriority(right));

                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                return string.Compare(
                    TypeName(left),
                    TypeName(right),
                    StringComparison.Ordinal);
            });
        }

        return result;
    }

    private static void AddConstructorDependencies(
        Type implementationType,
        IReadOnlyDictionary<Type, List<Type>> implementationsByContract,
        Dictionary<Type, List<DependencyEdge>> edges)
    {
        ConstructorInfo? constructor = SelectConstructor(implementationType);

        if (constructor == null)
        {
            return;
        }

        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            string source =
                $"constructor parameter '{parameter.Name}: {TypeName(parameter.ParameterType)}'";

            AddDependency(
                implementationType,
                parameter.ParameterType,
                source,
                implementationsByContract,
                edges);
        }
    }

    private static void AddInjectedPropertyDependencies(
        Type implementationType,
        IReadOnlyDictionary<Type, List<Type>> implementationsByContract,
        Dictionary<Type, List<DependencyEdge>> edges)
    {
        foreach (PropertyInfo property in GetInjectableProperties(implementationType))
        {
            string source =
                $"[Inject] property '{property.Name}: {TypeName(property.PropertyType)}'";

            AddDependency(
                implementationType,
                property.PropertyType,
                source,
                implementationsByContract,
                edges);
        }
    }

    private static void AddDependency(
        Type sourceType,
        Type requestedType,
        string sourceDescription,
        IReadOnlyDictionary<Type, List<Type>> implementationsByContract,
        Dictionary<Type, List<DependencyEdge>> edges)
    {
        // Lazy<T>, Func<T>, Func<T1, T2>, etc. are runtime/deferred
        // dependencies rather than construction-time dependencies.
        if (IsDeferredDependency(requestedType))
        {
            return;
        }

        if (TryGetCollectionElementType(requestedType, out Type? elementType))
        {
            // IEnumerable<Lazy<T>> is also deferred.
            if (IsDeferredDependency(elementType))
            {
                return;
            }

            if (!implementationsByContract.TryGetValue(
                    elementType,
                    out List<Type>? implementations))
            {
                return;
            }

            // Enumerable/array injection resolves ALL matching services.
            foreach (Type implementation in implementations)
            {
                AddEdge(
                    sourceType,
                    implementation,
                    $"{sourceDescription} via aggregate binding {TypeName(elementType)}",
                    edges);
            }

            return;
        }

        if (!implementationsByContract.TryGetValue(
                requestedType,
                out List<Type>? directImplementations))
        {
            // Dependency is either:
            // - an Anvil/core service from another container,
            // - framework infrastructure,
            // - or otherwise outside the application service graph.
            //
            // It cannot form a PwEngine -> PwEngine construction cycle unless
            // it is registered as an application ServiceBinding.
            return;
        }

        if (directImplementations.Count == 0)
        {
            return;
        }

        // Anvil's default service selector prefers the highest-priority
        // registration. BuildRegistrationMap already sorts by priority.
        Type selectedImplementation = directImplementations[0];

        AddEdge(
            sourceType,
            selectedImplementation,
            $"{sourceDescription} via binding {TypeName(requestedType)}",
            edges);
    }

    private static void AddEdge(
        Type source,
        Type destination,
        string description,
        Dictionary<Type, List<DependencyEdge>> edges)
    {
        if (!edges.TryGetValue(source, out List<DependencyEdge>? sourceEdges))
        {
            return;
        }

        // Destination may theoretically belong to another scanned application
        // assembly. Ensure it exists as a graph node.
        if (!edges.ContainsKey(destination))
        {
            edges[destination] = new List<DependencyEdge>();
        }

        bool alreadyExists = sourceEdges.Any(edge =>
            edge.To == destination &&
            string.Equals(
                edge.Description,
                description,
                StringComparison.Ordinal));

        if (!alreadyExists)
        {
            sourceEdges.Add(new DependencyEdge(
                source,
                destination,
                description));
        }
    }

    private static ConstructorInfo? SelectConstructor(Type type)
    {
        ConstructorInfo[] constructors = type
            .GetConstructors(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)
            .Where(constructor => !constructor.IsPrivate)
            .OrderByDescending(constructor => constructor.GetParameters().Length)
            .ThenBy(constructor => constructor.MetadataToken)
            .ToArray();

        if (constructors.Length == 0)
        {
            return null;
        }

        /*
         * LightInject uses a "most resolvable constructor" strategy.
         *
         * PwEngine services overwhelmingly have a single construction
         * constructor. Choosing the largest non-private constructor here
         * closely matches that behavior without requiring a live container.
         *
         * If PwEngine begins using multiple meaningful constructors on
         * ServiceBinding classes, this method is the one place that should be
         * made more sophisticated.
         */
        return constructors[0];
    }

    private static IEnumerable<PropertyInfo> GetInjectableProperties(Type type)
    {
        for (Type? current = type;
             current != null && current != typeof(object);
             current = current.BaseType)
        {
            PropertyInfo[] properties = current.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            foreach (PropertyInfo property in properties)
            {
                if (property.GetCustomAttribute<InjectAttribute>(
                        inherit: true) != null)
                {
                    yield return property;
                }
            }
        }
    }

    private static bool IsDeferredDependency(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        Type genericDefinition = type.GetGenericTypeDefinition();

        if (genericDefinition == typeof(Lazy<>))
        {
            return true;
        }

        // Treat any System.Func<...> as a factory/deferred dependency.
        string? fullName = genericDefinition.FullName;

        return fullName != null &&
               fullName.StartsWith(
                   "System.Func`",
                   StringComparison.Ordinal);
    }

    private static bool TryGetCollectionElementType(
        Type type,
        out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (!type.IsGenericType)
        {
            elementType = null!;
            return false;
        }

        Type genericDefinition = type.GetGenericTypeDefinition();

        if (genericDefinition == typeof(IEnumerable<>) ||
            genericDefinition == typeof(ICollection<>) ||
            genericDefinition == typeof(IList<>) ||
            genericDefinition == typeof(IReadOnlyCollection<>) ||
            genericDefinition == typeof(IReadOnlyList<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static HashSet<Type> FindReachableServices(ServiceGraph graph)
    {
        HashSet<Type> reachable = new();
        Stack<Type> pending = new(graph.EagerRoots);

        while (pending.Count > 0)
        {
            Type current = pending.Pop();

            if (!reachable.Add(current))
            {
                continue;
            }

            if (!graph.Edges.TryGetValue(
                    current,
                    out List<DependencyEdge>? dependencies))
            {
                continue;
            }

            foreach (DependencyEdge dependency in dependencies)
            {
                if (!reachable.Contains(dependency.To))
                {
                    pending.Push(dependency.To);
                }
            }
        }

        return reachable;
    }

    private static CycleSearchResult FindAllDependencyCycles(
        ServiceGraph graph,
        HashSet<Type> reachableServices)
    {
        List<Type> orderedNodes = reachableServices
            .OrderBy(TypeName, StringComparer.Ordinal)
            .ToList();

        Dictionary<string, IReadOnlyList<DependencyEdge>> cycles =
            new(StringComparer.Ordinal);

        bool truncated = false;

        foreach (Type start in orderedNodes)
        {
            HashSet<Type> pathNodes = new()
            {
                start
            };

            List<DependencyEdge> pathEdges = new();

            Search(start);

            if (truncated)
            {
                break;
            }

            void Search(Type current)
            {
                if (truncated)
                {
                    return;
                }

                if (!graph.Edges.TryGetValue(
                        current,
                        out List<DependencyEdge>? outgoing))
                {
                    return;
                }

                foreach (DependencyEdge edge in outgoing)
                {
                    if (!reachableServices.Contains(edge.To))
                    {
                        continue;
                    }

                    if (edge.To == start)
                    {
                        List<DependencyEdge> cycle = new(pathEdges)
                        {
                            edge
                        };

                        string key = BuildCycleKey(cycle);

                        cycles.TryAdd(key, cycle);

                        if (cycles.Count >= MaxReportedCycles)
                        {
                            truncated = true;
                            return;
                        }

                        continue;
                    }

                    if (pathNodes.Contains(edge.To))
                    {
                        continue;
                    }

                    /*
                     * Only enumerate a cycle when "start" is the
                     * lexicographically-smallest node in that cycle.
                     *
                     * This prevents us from reporting:
                     *
                     * A -> B -> C -> A
                     * B -> C -> A -> B
                     * C -> A -> B -> C
                     *
                     * as three separate cycles.
                     */
                    if (string.Compare(
                            TypeName(edge.To),
                            TypeName(start),
                            StringComparison.Ordinal) < 0)
                    {
                        continue;
                    }

                    pathNodes.Add(edge.To);
                    pathEdges.Add(edge);

                    Search(edge.To);

                    pathEdges.RemoveAt(pathEdges.Count - 1);
                    pathNodes.Remove(edge.To);

                    if (truncated)
                    {
                        return;
                    }
                }
            }
        }

        List<IReadOnlyList<DependencyEdge>> orderedCycles = cycles
            .Values
            .OrderBy(
                BuildCycleKey,
                StringComparer.Ordinal)
            .ToList();

        return new CycleSearchResult(
            orderedCycles,
            truncated);
    }

    private static string BuildCycleKey(
        IReadOnlyList<DependencyEdge> cycle)
    {
        if (cycle.Count == 0)
        {
            return string.Empty;
        }

        IEnumerable<string> names = cycle
            .Select(edge => TypeName(edge.From))
            .Append(TypeName(cycle[^1].To));

        return string.Join(" -> ", names);
    }

    private static string BuildFailureMessage(
        ServiceGraph graph,
        IReadOnlyList<IReadOnlyList<DependencyEdge>> cycles,
        bool truncated)
    {
        StringBuilder builder = new();

        builder.AppendLine();
        builder.AppendLine(
            "Anvil construction-time dependency cycle(s) detected.");
        builder.AppendLine();

        builder.AppendLine(
            $"Scanned {graph.Edges.Count} application service implementation(s).");

        builder.AppendLine(
            $"Detected {(truncated ? "at least " : string.Empty)}{cycles.Count} eager cycle(s).");

        builder.AppendLine();

        builder.AppendLine(
            "Lazy<T> and Func<T> dependencies are intentionally excluded because they are deferred.");

        builder.AppendLine(
            "Enumerable/array dependencies are expanded to every registered implementation, matching the important Anvil/LightInject startup behavior.");

        if (truncated)
        {
            builder.AppendLine();
            builder.AppendLine(
                $"Cycle output was capped at {MaxReportedCycles}. " +
                "Raise MaxReportedCycles if you genuinely need more.");
        }

        builder.AppendLine();

        for (int index = 0; index < cycles.Count; index++)
        {
            IReadOnlyList<DependencyEdge> cycle = cycles[index];

            builder.AppendLine(
                $"Cycle {index + 1}:");

            builder.AppendLine(
                $"  {TypeName(cycle[0].From)}");

            foreach (DependencyEdge edge in cycle)
            {
                builder.AppendLine(
                    $"    -> {TypeName(edge.To)}");

                builder.AppendLine(
                    $"       via {edge.Description}");
            }

            builder.AppendLine();
        }

        builder.AppendLine(
            "Break a construction-time back-edge with a genuinely deferred dependency " +
            "(for example Lazy<T>) or by separating responsibilities.");

        return builder.ToString();
    }

    private static IReadOnlyList<Assembly> LoadApplicationAssemblies()
    {
        Assembly rootAssembly =
            typeof(AnvilServiceDependencyCycleTests).Assembly;

        Dictionary<string, Assembly> loaded =
            new(StringComparer.OrdinalIgnoreCase);

        Queue<Assembly> pending = new();

        AddAssembly(rootAssembly);

        while (pending.Count > 0)
        {
            Assembly assembly = pending.Dequeue();

            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                string? assemblyName = reference.Name;

                if (assemblyName == null ||
                    !assemblyName.StartsWith(
                        ApplicationAssemblyPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (loaded.ContainsKey(assemblyName))
                {
                    continue;
                }

                Assembly referencedAssembly;

                try
                {
                    referencedAssembly = Assembly.Load(reference);
                }
                catch (Exception exception) when (
                    exception is FileNotFoundException or
                    FileLoadException or
                    BadImageFormatException)
                {
                    throw new InvalidOperationException(
                        $"Could not load application assembly '{reference.FullName}' " +
                        "while validating the Anvil service graph.",
                        exception);
                }

                AddAssembly(referencedAssembly);
            }
        }

        return loaded
            .Values
            .OrderBy(
                assembly => assembly.GetName().Name,
                StringComparer.Ordinal)
            .ToArray();

        void AddAssembly(Assembly assembly)
        {
            string name =
                assembly.GetName().Name ??
                assembly.FullName ??
                Guid.NewGuid().ToString();

            if (!loaded.TryAdd(name, assembly))
            {
                return;
            }

            pending.Enqueue(assembly);
        }
    }

    private static IReadOnlyList<Type> GetLoadableTypes(
        Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            string loaderErrors = string.Join(
                Environment.NewLine,
                exception.LoaderExceptions
                    .Where(error => error != null)
                    .Select(error => $"  - {error!.Message}"));

            throw new InvalidOperationException(
                $"Could not inspect every type in assembly " +
                $"'{assembly.GetName().Name}'. " +
                $"The dependency graph would be incomplete." +
                Environment.NewLine +
                loaderErrors,
                exception);
        }
    }

    private static bool IsConcreteType(Type type)
    {
        return type is
        {
            IsClass: true,
            IsAbstract: false,
            ContainsGenericParameters: false
        };
    }

    /// <summary>
    /// Reads ServiceBindingOptionsAttribute.Lazy reflectively so the test does
    /// not become tightly coupled to a particular Anvil package revision.
    /// </summary>
    private static bool IsLazyService(Type type)
    {
        object? options = GetServiceBindingOptions(type);

        if (options == null)
        {
            return false;
        }

        PropertyInfo? lazyProperty =
            options.GetType().GetProperty("Lazy");

        return lazyProperty?.GetValue(options) is true;
    }

    /// <summary>
    /// Lower numeric values are higher priority in Anvil.
    /// Normal priority is 0.
    /// </summary>
    private static int GetBindingPriority(Type type)
    {
        object? options = GetServiceBindingOptions(type);

        if (options == null)
        {
            return 0;
        }

        PropertyInfo? priorityProperty =
            options.GetType().GetProperty("BindingPriority");

        object? value =
            priorityProperty?.GetValue(options);

        return value == null
            ? 0
            : Convert.ToInt32(value);
    }

    private static object? GetServiceBindingOptions(Type type)
    {
        return type
            .GetCustomAttributes(inherit: true)
            .FirstOrDefault(attribute =>
                string.Equals(
                    attribute.GetType().FullName,
                    "Anvil.Services.ServiceBindingOptionsAttribute",
                    StringComparison.Ordinal));
    }

    private static int CompareEdges(
        DependencyEdge left,
        DependencyEdge right)
    {
        int targetComparison = string.Compare(
            TypeName(left.To),
            TypeName(right.To),
            StringComparison.Ordinal);

        if (targetComparison != 0)
        {
            return targetComparison;
        }

        return string.Compare(
            left.Description,
            right.Description,
            StringComparison.Ordinal);
    }

    private static string TypeName(Type type)
    {
        if (type.IsArray)
        {
            return $"{TypeName(type.GetElementType()!)}[]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        Type genericDefinition =
            type.GetGenericTypeDefinition();

        string genericName =
            genericDefinition.FullName ??
            genericDefinition.Name;

        int tickIndex =
            genericName.IndexOf('`');

        if (tickIndex >= 0)
        {
            genericName =
                genericName[..tickIndex];
        }

        string arguments = string.Join(
            ", ",
            type.GetGenericArguments().Select(TypeName));

        return $"{genericName}<{arguments}>";
    }

    private sealed record ServiceDefinition(
        Type ImplementationType,
        IReadOnlyList<Type> Bindings,
        bool IsLazyService,
        int BindingPriority);

    private sealed record DependencyEdge(
        Type From,
        Type To,
        string Description);

    private sealed record ServiceGraph(
        Dictionary<Type, List<DependencyEdge>> Edges,
        HashSet<Type> EagerRoots,
        Dictionary<Type, List<Type>> ImplementationsByContract);

    private sealed record CycleSearchResult(
        IReadOnlyList<IReadOnlyList<DependencyEdge>> Cycles,
        bool Truncated);
}
