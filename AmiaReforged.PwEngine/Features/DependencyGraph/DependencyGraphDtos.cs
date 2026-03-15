namespace AmiaReforged.PwEngine.Features.DependencyGraph;

/// <summary>
/// Represents the full dependency graph for the PwEngine assembly.
/// Nodes are classes/interfaces/structs/enums grouped by namespace (compound nodes).
/// Edges represent inheritance, implementation, constructor injection, and field/property references.
/// </summary>
public class DependencyGraphDto
{
    /// <summary>
    /// Namespace compound nodes (parents in the graph).
    /// </summary>
    public List<NamespaceNodeDto> Namespaces { get; set; } = new();

    /// <summary>
    /// Type nodes (children inside namespace groups).
    /// </summary>
    public List<TypeNodeDto> Types { get; set; } = new();

    /// <summary>
    /// Edges between type nodes.
    /// </summary>
    public List<TypeEdgeDto> Edges { get; set; } = new();

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public DependencyGraphStats Stats { get; set; } = new();
}

/// <summary>
/// A namespace group node (compound parent).
/// </summary>
public class NamespaceNodeDto
{
    /// <summary>
    /// Unique ID for the namespace node (e.g. "ns:AmiaReforged.PwEngine.Features.Crafting").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Full namespace name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Short display label (last segment of namespace).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Number of types in this namespace.
    /// </summary>
    public int TypeCount { get; set; }
}

/// <summary>
/// A type node (class, interface, struct, or enum).
/// </summary>
public class TypeNodeDto
{
    /// <summary>
    /// Unique ID for the type node (full type name).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Short display name (type name without namespace).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Full type name including namespace.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the parent namespace node.
    /// </summary>
    public string NamespaceId { get; set; } = string.Empty;

    /// <summary>
    /// Kind of type: Class, Interface, Struct, Enum, Record.
    /// </summary>
    public string Kind { get; set; } = "Class";

    /// <summary>
    /// Whether the type is abstract.
    /// </summary>
    public bool IsAbstract { get; set; }

    /// <summary>
    /// Whether the type is static (abstract + sealed).
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// Base class name (null if System.Object or none).
    /// </summary>
    public string? BaseClass { get; set; }

    /// <summary>
    /// Implemented interface names.
    /// </summary>
    public List<string> Interfaces { get; set; } = new();

    /// <summary>
    /// Number of inbound dependency edges.
    /// </summary>
    public int InboundCount { get; set; }

    /// <summary>
    /// Number of outbound dependency edges.
    /// </summary>
    public int OutboundCount { get; set; }
}

/// <summary>
/// An edge between two type nodes.
/// </summary>
public class TypeEdgeDto
{
    /// <summary>
    /// Source type node ID (the dependent).
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Target type node ID (the dependency).
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Relationship type: Inherits, Implements, ConstructorDep, FieldDep.
    /// </summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// Optional label for display (e.g., field name, parameter name).
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// Summary statistics for the dependency graph.
/// </summary>
public class DependencyGraphStats
{
    public int TotalTypes { get; set; }
    public int TotalNamespaces { get; set; }
    public int TotalEdges { get; set; }
    public int ClassCount { get; set; }
    public int InterfaceCount { get; set; }
    public int StructCount { get; set; }
    public int EnumCount { get; set; }
    public int InheritsEdges { get; set; }
    public int ImplementsEdges { get; set; }
    public int ConstructorDepEdges { get; set; }
    public int FieldDepEdges { get; set; }
}
