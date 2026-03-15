namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// DTOs for the PwEngine dependency graph visualization.
/// Mirrors the server-side DTOs from AmiaReforged.PwEngine.Features.DependencyGraph.
/// </summary>
public class DependencyGraphDto
{
    public List<NamespaceNodeDto> Namespaces { get; set; } = new();
    public List<TypeNodeDto> Types { get; set; } = new();
    public List<TypeEdgeDto> Edges { get; set; } = new();
    public DependencyGraphStats Stats { get; set; } = new();
}

public class NamespaceNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int TypeCount { get; set; }
}

public class TypeNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NamespaceId { get; set; } = string.Empty;
    public string Kind { get; set; } = "Class";
    public bool IsAbstract { get; set; }
    public bool IsStatic { get; set; }
    public string? BaseClass { get; set; }
    public List<string> Interfaces { get; set; } = new();
    public int InboundCount { get; set; }
    public int OutboundCount { get; set; }
}

public class TypeEdgeDto
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Label { get; set; }
}

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
