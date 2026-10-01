using System.Collections.ObjectModel;
using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Nominal, immutable aggregate storage. Each field retains its static runtime type.</summary>
public sealed record GlyphAggregateFieldValue(GlyphDataType Type, string NominalType, object? Value);

public sealed class GlyphAggregateValue(string typeName, string? variantName,
    IReadOnlyDictionary<string, GlyphAggregateFieldValue>? fields = null)
{
    public string TypeName { get; } = typeName;
    public string? VariantName { get; } = variantName;
    public IReadOnlyDictionary<string, GlyphAggregateFieldValue> Fields { get; } =
        new ReadOnlyDictionary<string, GlyphAggregateFieldValue>(fields == null ? new Dictionary<string, GlyphAggregateFieldValue>() : new Dictionary<string, GlyphAggregateFieldValue>(fields));

    public GlyphAggregateValue With(string field, GlyphAggregateFieldValue value)
    {
        Dictionary<string, GlyphAggregateFieldValue> copy = new(Fields) { [field] = value };
        return new(TypeName, VariantName, copy);
    }
}

public sealed record GlyphLocalValue(GlyphDataType Type, string NominalType, object? Value);
