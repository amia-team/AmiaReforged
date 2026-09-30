using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;

namespace AmiaReforged.PwEngine.Features.Glyph.Platform;

/// <summary>One declaration drives entry outputs, wireless getters, aliases and compiler metadata.</summary>
public sealed class GlyphContextSchema(IEnumerable<ContextPinDescriptor> fields)
{
    public IReadOnlyList<ContextPinDescriptor> Fields { get; } = Array.AsReadOnly(fields.ToArray());
    public List<GlyphPin> CreateOutputPins() => Fields.Select(f => Pins.Out(f.PinId, f.DisplayName, f.DataType)).ToList();
    public Dictionary<string, object?> Read(GlyphExecutionContext context) =>
        Fields.ToDictionary(f => f.PinId, f => f.Accessor(context), StringComparer.Ordinal);
    public string Resolve(string path) => Fields.FirstOrDefault(f => f.Aliases?.Contains(path) == true)?.PinId ??
        (path.StartsWith("context.", StringComparison.Ordinal) ? path[8..] :
         path.StartsWith("chaos.", StringComparison.Ordinal) ? path[6..] : path);
    public IEnumerable<string> Names(string id) => new[] { id, "context." + id, "chaos." + id }
        .Concat(Fields.Single(f => f.PinId == id).Aliases ?? []).Distinct(StringComparer.Ordinal);
}
