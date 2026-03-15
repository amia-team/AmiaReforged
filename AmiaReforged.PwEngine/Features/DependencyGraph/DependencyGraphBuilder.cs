using System.Collections.Concurrent;
using System.Reflection;
using NLog;

namespace AmiaReforged.PwEngine.Features.DependencyGraph;

/// <summary>
/// Builds a dependency graph by reflecting over the PwEngine assembly (and optionally
/// other AmiaReforged assemblies). Analyses inheritance, interface implementation,
/// constructor parameters, and field/property types to produce edges.
/// Results are cached because the graph only changes on redeployment.
/// </summary>
public class DependencyGraphBuilder
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Prefixes of assemblies whose types should be included in the graph.
    /// </summary>
    private static readonly string[] IncludedAssemblyPrefixes = { "AmiaReforged" };

    private static readonly Lazy<DependencyGraphDto> CachedGraph = new(BuildGraph);

    /// <summary>
    /// Returns the (cached) dependency graph. Thread-safe via Lazy.
    /// </summary>
    public static DependencyGraphDto GetGraph() => CachedGraph.Value;

    /// <summary>
    /// Returns a filtered view of the cached graph, including only types whose
    /// namespace starts with the given prefix.
    /// </summary>
    public static DependencyGraphDto GetFilteredGraph(string namespacePrefix)
    {
        DependencyGraphDto full = GetGraph();

        HashSet<string> matchingTypeIds = full.Types
            .Where(t => t.FullName.StartsWith(namespacePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Id)
            .ToHashSet();

        List<TypeNodeDto> filteredTypes = full.Types
            .Where(t => matchingTypeIds.Contains(t.Id))
            .ToList();

        List<TypeEdgeDto> filteredEdges = full.Edges
            .Where(e => matchingTypeIds.Contains(e.Source) && matchingTypeIds.Contains(e.Target))
            .ToList();

        HashSet<string> usedNamespaces = filteredTypes.Select(t => t.NamespaceId).ToHashSet();
        List<NamespaceNodeDto> filteredNamespaces = full.Namespaces
            .Where(n => usedNamespaces.Contains(n.Id))
            .ToList();

        // Recompute inbound/outbound counts for filtered set
        ConcurrentDictionary<string, int> inbound = new();
        ConcurrentDictionary<string, int> outbound = new();
        foreach (TypeEdgeDto edge in filteredEdges)
        {
            inbound.AddOrUpdate(edge.Target, 1, (_, c) => c + 1);
            outbound.AddOrUpdate(edge.Source, 1, (_, c) => c + 1);
        }

        foreach (TypeNodeDto t in filteredTypes)
        {
            t.InboundCount = inbound.GetValueOrDefault(t.Id, 0);
            t.OutboundCount = outbound.GetValueOrDefault(t.Id, 0);
        }

        return new DependencyGraphDto
        {
            Namespaces = filteredNamespaces,
            Types = filteredTypes,
            Edges = filteredEdges,
            Stats = ComputeStats(filteredNamespaces, filteredTypes, filteredEdges)
        };
    }

    // ===================== Core Builder =====================

    private static DependencyGraphDto BuildGraph()
    {
        Logger.Info("Building PwEngine dependency graph via reflection...");

        // Collect all relevant types from loaded assemblies
        List<Type> allTypes = CollectTypes();
        Logger.Info("Found {Count} types across AmiaReforged assemblies", allTypes.Count);

        // Build lookup for fast membership checks
        HashSet<string> knownTypeNames = allTypes
            .Select(GetTypeId)
            .ToHashSet();

        // Build namespace nodes
        Dictionary<string, NamespaceNodeDto> namespaceMap = new();
        List<TypeNodeDto> typeNodes = new();
        List<TypeEdgeDto> edges = new();

        foreach (Type type in allTypes)
        {
            string typeId = GetTypeId(type);
            string ns = type.Namespace ?? "(global)";
            string nsId = $"ns:{ns}";

            // Ensure namespace node exists
            if (!namespaceMap.ContainsKey(nsId))
            {
                namespaceMap[nsId] = new NamespaceNodeDto
                {
                    Id = nsId,
                    FullName = ns,
                    Label = GetShortNamespace(ns),
                    TypeCount = 0
                };
            }

            namespaceMap[nsId].TypeCount++;

            // Create type node
            TypeNodeDto node = new()
            {
                Id = typeId,
                Label = GetShortTypeName(type),
                FullName = typeId,
                NamespaceId = nsId,
                Kind = GetTypeKind(type),
                IsAbstract = type.IsAbstract && !type.IsSealed && !type.IsInterface,
                IsStatic = type.IsAbstract && type.IsSealed,
                BaseClass = GetBaseClassName(type, knownTypeNames),
                Interfaces = GetInterfaceNames(type, knownTypeNames)
            };
            typeNodes.Add(node);

            // --- Build edges ---

            // 1. Inheritance
            if (type.BaseType != null && IsIncludedType(type.BaseType) && knownTypeNames.Contains(GetTypeId(type.BaseType)))
            {
                edges.Add(new TypeEdgeDto
                {
                    Source = typeId,
                    Target = GetTypeId(type.BaseType),
                    Relationship = "Inherits"
                });
            }

            // 2. Interface implementation (direct only, not inherited)
            foreach (Type iface in GetDirectInterfaces(type))
            {
                Type resolved = iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface;
                string ifaceId = GetTypeId(resolved);
                if (knownTypeNames.Contains(ifaceId))
                {
                    edges.Add(new TypeEdgeDto
                    {
                        Source = typeId,
                        Target = ifaceId,
                        Relationship = "Implements"
                    });
                }
            }

            // 3. Constructor parameter dependencies
            try
            {
                foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (ParameterInfo param in ctor.GetParameters())
                    {
                        foreach (string depId in ResolveTypeIds(param.ParameterType, knownTypeNames))
                        {
                            if (depId != typeId) // no self-edges
                            {
                                edges.Add(new TypeEdgeDto
                                {
                                    Source = typeId,
                                    Target = depId,
                                    Relationship = "ConstructorDep",
                                    Label = param.Name
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Could not inspect constructors of {Type}: {Error}", typeId, ex.Message);
            }

            // 4. Field and property dependencies
            try
            {
                BindingFlags memberFlags = BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

                foreach (FieldInfo field in type.GetFields(memberFlags))
                {
                    foreach (string depId in ResolveTypeIds(field.FieldType, knownTypeNames))
                    {
                        if (depId != typeId)
                        {
                            edges.Add(new TypeEdgeDto
                            {
                                Source = typeId,
                                Target = depId,
                                Relationship = "FieldDep",
                                Label = field.Name
                            });
                        }
                    }
                }

                foreach (PropertyInfo prop in type.GetProperties(memberFlags))
                {
                    foreach (string depId in ResolveTypeIds(prop.PropertyType, knownTypeNames))
                    {
                        if (depId != typeId)
                        {
                            edges.Add(new TypeEdgeDto
                            {
                                Source = typeId,
                                Target = depId,
                                Relationship = "FieldDep",
                                Label = prop.Name
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug("Could not inspect fields/properties of {Type}: {Error}", typeId, ex.Message);
            }
        }

        // Deduplicate edges (same source+target+relationship)
        // Also filter out any edges referencing types not in the graph
        HashSet<string> typeIdSet = typeNodes.Select(t => t.Id).ToHashSet();
        edges = edges
            .Where(e => typeIdSet.Contains(e.Source) && typeIdSet.Contains(e.Target))
            .GroupBy(e => (e.Source, e.Target, e.Relationship))
            .Select(g => g.First())
            .ToList();

        // Compute inbound/outbound counts
        ConcurrentDictionary<string, int> inboundCounts = new();
        ConcurrentDictionary<string, int> outboundCounts = new();
        foreach (TypeEdgeDto edge in edges)
        {
            inboundCounts.AddOrUpdate(edge.Target, 1, (_, c) => c + 1);
            outboundCounts.AddOrUpdate(edge.Source, 1, (_, c) => c + 1);
        }

        foreach (TypeNodeDto node in typeNodes)
        {
            node.InboundCount = inboundCounts.GetValueOrDefault(node.Id, 0);
            node.OutboundCount = outboundCounts.GetValueOrDefault(node.Id, 0);
        }

        List<NamespaceNodeDto> namespaces = namespaceMap.Values.OrderBy(n => n.FullName).ToList();
        DependencyGraphStats stats = ComputeStats(namespaces, typeNodes, edges);

        Logger.Info(
            "Dependency graph built: {Types} types, {Namespaces} namespaces, {Edges} edges",
            stats.TotalTypes, stats.TotalNamespaces, stats.TotalEdges);

        return new DependencyGraphDto
        {
            Namespaces = namespaces,
            Types = typeNodes,
            Edges = edges,
            Stats = stats
        };
    }

    // ===================== Helpers =====================

    private static List<Type> CollectTypes()
    {
        List<Type> result = new();

        // Get the PwEngine assembly
        Assembly pwEngine = typeof(DependencyGraphBuilder).Assembly;
        AddTypesFrom(pwEngine, result);

        // Also scan other loaded AmiaReforged assemblies
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm == pwEngine) continue;
            string? name = asm.GetName().Name;
            if (name != null && IncludedAssemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                AddTypesFrom(asm, result);
            }
        }

        return result;
    }

    private static void AddTypesFrom(Assembly assembly, List<Type> result)
    {
        try
        {
            foreach (Type type in assembly.GetTypes())
            {
                // Skip compiler-generated types, anonymous types, etc.
                if (type.FullName == null) continue;
                if (type.FullName.Contains('<')) continue; // Skip anonymous/display classes
                if (type.FullName.Contains('+')) continue; // Skip nested types initially
                if (!IsIncludedType(type)) continue;

                result.Add(type);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Some types may not be loadable — include what we can
            foreach (Type? type in ex.Types)
            {
                if (type?.FullName != null && !type.FullName.Contains('<') && !type.FullName.Contains('+') && IsIncludedType(type))
                {
                    result.Add(type);
                }
            }

            Logger.Warn("ReflectionTypeLoadException scanning {Assembly}: {Message}",
                assembly.GetName().Name, ex.Message);
        }
    }

    private static bool IsIncludedType(Type type)
    {
        string? ns = type.Namespace;
        if (ns == null) return false;
        return IncludedAssemblyPrefixes.Any(p => ns.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetTypeId(Type type)
    {
        if (type.IsGenericType)
        {
            Type def = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            return def.FullName ?? def.Name;
        }

        return type.FullName ?? type.Name;
    }

    private static string GetShortTypeName(Type type)
    {
        string name = type.Name;
        // Remove generic arity suffix (e.g., "Repository`1" → "Repository<T>")
        int backtick = name.IndexOf('`');
        if (backtick >= 0)
        {
            int arity = int.Parse(name[(backtick + 1)..]);
            string[] paramNames = type.GetGenericArguments().Select(t => t.Name).ToArray();
            name = $"{name[..backtick]}<{string.Join(", ", paramNames)}>";
        }

        return name;
    }

    private static string GetShortNamespace(string ns)
    {
        int lastDot = ns.LastIndexOf('.');
        return lastDot >= 0 ? ns[(lastDot + 1)..] : ns;
    }

    private static string GetTypeKind(Type type)
    {
        if (type.IsInterface) return "Interface";
        if (type.IsEnum) return "Enum";
        if (type.IsValueType) return "Struct";
        // Check for record: records have an EqualityContract property generated by compiler
        if (type.GetProperty("EqualityContract", BindingFlags.NonPublic | BindingFlags.Instance) != null)
            return "Record";
        return "Class";
    }

    private static string? GetBaseClassName(Type type, HashSet<string> knownTypeNames)
    {
        if (type.BaseType == null) return null;
        if (type.BaseType == typeof(object)) return null;
        if (type.BaseType == typeof(ValueType)) return null;
        if (type.BaseType == typeof(Enum)) return null;

        string baseId = GetTypeId(type.BaseType);
        return knownTypeNames.Contains(baseId) ? baseId : type.BaseType.Name;
    }

    private static List<string> GetInterfaceNames(Type type, HashSet<string> knownTypeNames)
    {
        return GetDirectInterfaces(type)
            .Select(i => i.IsGenericType ? i.GetGenericTypeDefinition() : i)
            .Select(GetTypeId)
            .Where(id => knownTypeNames.Contains(id))
            .ToList();
    }

    /// <summary>
    /// Get only interfaces directly implemented by a type, not inherited from a base class.
    /// </summary>
    private static IEnumerable<Type> GetDirectInterfaces(Type type)
    {
        HashSet<Type> allInterfaces = new(type.GetInterfaces());

        // Remove interfaces inherited from the base class
        if (type.BaseType != null)
        {
            foreach (Type baseInterface in type.BaseType.GetInterfaces())
            {
                allInterfaces.Remove(baseInterface);
            }
        }

        return allInterfaces;
    }

    /// <summary>
    /// Resolve a type (possibly generic) into known type IDs.
    /// Unwraps generics, nullable, arrays, etc.
    /// </summary>
    private static IEnumerable<string> ResolveTypeIds(Type type, HashSet<string> knownTypeNames)
    {
        // Unwrap nullable
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;

        // Unwrap arrays
        if (underlying.IsArray)
        {
            underlying = underlying.GetElementType()!;
        }

        // If generic, resolve the definition and all generic arguments
        if (underlying.IsGenericType)
        {
            Type def = underlying.GetGenericTypeDefinition();
            string defId = GetTypeId(def);
            if (knownTypeNames.Contains(defId))
            {
                yield return defId;
            }

            foreach (Type arg in underlying.GetGenericArguments())
            {
                foreach (string argId in ResolveTypeIds(arg, knownTypeNames))
                {
                    yield return argId;
                }
            }

            yield break;
        }

        string id = GetTypeId(underlying);
        if (knownTypeNames.Contains(id))
        {
            yield return id;
        }
    }

    private static DependencyGraphStats ComputeStats(
        List<NamespaceNodeDto> namespaces,
        List<TypeNodeDto> types,
        List<TypeEdgeDto> edges)
    {
        return new DependencyGraphStats
        {
            TotalTypes = types.Count,
            TotalNamespaces = namespaces.Count,
            TotalEdges = edges.Count,
            ClassCount = types.Count(t => t.Kind == "Class"),
            InterfaceCount = types.Count(t => t.Kind == "Interface"),
            StructCount = types.Count(t => t.Kind == "Struct"),
            EnumCount = types.Count(t => t.Kind == "Enum"),
            InheritsEdges = edges.Count(e => e.Relationship == "Inherits"),
            ImplementsEdges = edges.Count(e => e.Relationship == "Implements"),
            ConstructorDepEdges = edges.Count(e => e.Relationship == "ConstructorDep"),
            FieldDepEdges = edges.Count(e => e.Relationship == "FieldDep")
        };
    }
}
