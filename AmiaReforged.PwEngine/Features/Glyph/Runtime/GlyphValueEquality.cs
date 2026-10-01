namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Value equality for immutable Glyph values, shared by all membership and set operations.</summary>
public sealed class GlyphValueEquality : IEqualityComparer<object?>
{
    public static GlyphValueEquality Instance { get; } = new();
    public new bool Equals(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null || a.GetType() != b.GetType()) return false;
        return (a, b) switch
        {
            (GlyphAggregateValue x, GlyphAggregateValue y) => x.TypeName == y.TypeName && x.VariantName == y.VariantName &&
                x.Fields.Count == y.Fields.Count && x.Fields.All(f => y.Fields.TryGetValue(f.Key, out var other) &&
                    f.Value.Type == other.Type && f.Value.NominalType == other.NominalType && Equals(f.Value.Value, other.Value)),
            (GlyphListValue x, GlyphListValue y) => x.ElementTypeName == y.ElementTypeName && x.SequenceEqual(y, this),
            (GlyphDictionaryValue x, GlyphDictionaryValue y) => x.KeyType == y.KeyType && x.ValueType == y.ValueType &&
                x.Count == y.Count && x.Entries.All(e => y.Contains(e.Key) && Equals(e.Value, y.Get(e.Key))),
            _ => a.Equals(b)
        };
    }
    public int GetHashCode(object? value)
    {
        if (value == null) return 0;
        if (value is GlyphAggregateValue aggregate)
        {
            int fields = 0;
            foreach (var f in aggregate.Fields) fields ^= HashCode.Combine(f.Key, f.Value.Type, f.Value.NominalType, GetHashCode(f.Value.Value));
            return HashCode.Combine(aggregate.TypeName, aggregate.VariantName, fields);
        }
        if (value is GlyphListValue list)
        {
            var hash = new HashCode(); hash.Add(list.ElementTypeName);
            foreach (var item in list) hash.Add(GetHashCode(item));
            return hash.ToHashCode();
        }
        if (value is GlyphDictionaryValue dictionary)
        {
            int entries = 0;
            foreach (var entry in dictionary.Entries) entries ^= HashCode.Combine(entry.Key, GetHashCode(entry.Value));
            return HashCode.Combine(dictionary.KeyType, dictionary.ValueType, entries);
        }
        return value.GetHashCode();
    }
}
