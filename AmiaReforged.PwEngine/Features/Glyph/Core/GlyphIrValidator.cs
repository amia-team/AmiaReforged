namespace AmiaReforged.PwEngine.Features.Glyph.Core;

public sealed record GlyphIrDiagnostic(string Code, string Message, Guid? NodeId = null, Guid? EdgeId = null);

/// <summary>Checks the executable boundary independently of the source compiler.</summary>
public sealed class GlyphIrValidator(IGlyphNodeDefinitionRegistry registry)
{
    // These numeric coercions are implemented by the existing executor input accessors.
    public static bool CanConnect(GlyphDataType from, GlyphDataType to) => from == to ||
        (from is GlyphDataType.Int or GlyphDataType.Float && to is GlyphDataType.Int or GlyphDataType.Float);

    public IReadOnlyList<GlyphIrDiagnostic> Validate(GlyphGraph ir)
    {
        List<GlyphIrDiagnostic> errors = [];
        Dictionary<Guid, GlyphNodeDefinition> definitions = new();
        HashSet<Guid> ids = [];
        foreach (GlyphNodeInstance node in ir.Nodes)
        {
            if (!ids.Add(node.InstanceId))
                errors.Add(new("GLYPH4001", "Duplicate operation identity.", node.InstanceId));
            GlyphNodeDefinition? def = registry.Get(node.TypeId);
            if (def == null)
            {
                errors.Add(new("GLYPH4002", $"Unknown operation '{node.TypeId}'.", node.InstanceId));
                continue;
            }
            definitions[node.InstanceId] = def;
            if (def.RestrictToEventType is { } evt && evt != ir.EventType)
                errors.Add(new("GLYPH4003", $"'{node.TypeId}' is unavailable for {ir.EventType}.", node.InstanceId));
            if (def.ScriptCategory is { } category && category != ir.EventType.GetCategory())
                errors.Add(new("GLYPH4004", $"'{node.TypeId}' is unavailable in this category.", node.InstanceId));
            if (def.IsSingleton && ir.Nodes.Count(n => n.TypeId == node.TypeId) > 1)
                errors.Add(new("GLYPH4005", $"Duplicate singleton '{node.TypeId}'.", node.InstanceId));
        }
        bool hasEntry = Platform.GlyphEvents.All.FirstOrDefault(e => e.EventType == ir.EventType)?.Stages != null
            ? definitions.Values.Any(d => d.Archetype == GlyphNodeArchetype.PipelineStage)
            : ir.FindEntryNode() != null;
        if (!hasEntry) errors.Add(new("GLYPH4006", "Missing event entry point."));
        HashSet<(Guid, string)> inputs = [];
        HashSet<(Guid, string)> execOutputs = [];
        Dictionary<Guid, List<Guid>> dependencies = new();
        foreach (GlyphEdge edge in ir.Edges)
        {
            if (!ids.Contains(edge.SourceNodeId) || !ids.Contains(edge.TargetNodeId))
            {
                errors.Add(new("GLYPH4007", "Edge references a missing operation.", EdgeId: edge.Id));
                continue;
            }
            if (!definitions.TryGetValue(edge.SourceNodeId, out var source) ||
                !definitions.TryGetValue(edge.TargetNodeId, out var target)) continue;
            GlyphPin? output = source.OutputPins.FirstOrDefault(p => p.Id == edge.SourcePinId);
            GlyphPin? input = target.InputPins.FirstOrDefault(p => p.Id == edge.TargetPinId);
            if (output == null || input == null)
            {
                errors.Add(new("GLYPH4008", "Unknown pin or invalid pin direction.", EdgeId: edge.Id));
                continue;
            }
            if (output.Direction != GlyphPinDirection.Output || input.Direction != GlyphPinDirection.Input)
                errors.Add(new("GLYPH4009", "Invalid pin direction.", EdgeId: edge.Id));
            if (!CanConnect(output.DataType, input.DataType))
                errors.Add(new("GLYPH4010", $"Cannot connect {output.DataType} to {input.DataType}.", EdgeId: edge.Id));
            if (input.DataType != GlyphDataType.Exec && !inputs.Add((edge.TargetNodeId, input.Id)))
                errors.Add(new("GLYPH4011", "Multiple sources for a single-value input.", EdgeId: edge.Id));
            if (output.DataType == GlyphDataType.Exec && !execOutputs.Add((edge.SourceNodeId, output.Id)))
                errors.Add(new("GLYPH4012", "Execution output has multiple targets; use a sequence.", EdgeId: edge.Id));
            if (output.DataType != GlyphDataType.Exec)
            {
                if (!dependencies.TryGetValue(edge.TargetNodeId, out var list))
                    dependencies[edge.TargetNodeId] = list = [];
                list.Add(edge.SourceNodeId);
            }
        }
        // Lazy data evaluation cannot safely execute a dependency cycle.
        HashSet<Guid> visiting = [], visited = [];
        bool Cycle(Guid id)
        {
            if (visiting.Contains(id)) return true;
            if (!visited.Add(id)) return false;
            visiting.Add(id);
            if (dependencies.TryGetValue(id, out var next) && next.Any(Cycle)) return true;
            visiting.Remove(id);
            return false;
        }
        if (ids.Any(Cycle)) errors.Add(new("GLYPH4013", "Cyclic data dependency."));
        return errors.AsReadOnly();
    }
}
