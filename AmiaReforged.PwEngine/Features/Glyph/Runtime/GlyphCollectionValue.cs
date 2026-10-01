using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

public sealed class GlyphListValue : IReadOnlyList<object?>
{
    private readonly ImmutableArray<object?> _items;
    public GlyphDataType ElementType { get; }
    public string ElementTypeName { get; }
    public int Count => _items.Length;
    public object? this[int index] => index >= 0 && index < Count ? _items[index]
        : throw new InvalidOperationException($"List index {index} is outside [0, {Count}).");
    internal GlyphListValue(GlyphDataType element, IEnumerable<object?> items, string? elementTypeName = null)
    { ElementType = element; ElementTypeName = elementTypeName ?? GlyphCollections.TypeName(element); _items = items.ToImmutableArray(); }
    public IEnumerator<object?> GetEnumerator() => ((IEnumerable<object?>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public sealed class GlyphDictionaryValue
{
    private readonly ImmutableDictionary<object, object?> _items;
    public GlyphDataType KeyType { get; }
    public GlyphDataType ValueType { get; }
    public int Count => _items.Count;
    internal GlyphDictionaryValue(GlyphDataType key, GlyphDataType value, ImmutableDictionary<object, object?>? items = null)
    { KeyType = key; ValueType = value; _items = items ?? ImmutableDictionary<object, object?>.Empty; }
    public bool Contains(object key) => _items.ContainsKey(key);
    public object? Get(object key) => _items.TryGetValue(key, out var value) ? value
        : throw new InvalidOperationException("Dictionary key was not found.");
    internal GlyphDictionaryValue With(object key, object? value) => new(KeyType, ValueType, _items.SetItem(key, value));
    internal GlyphDictionaryValue Without(object key) => new(KeyType, ValueType, _items.Remove(key));
    internal IEnumerable<KeyValuePair<object, object?>> Entries => _items;
}

public static class GlyphCollections
{
    public static IReadOnlyList<GlyphDataType> BasicTypes { get; } =
        [GlyphDataType.NwObject, GlyphDataType.String, GlyphDataType.Int, GlyphDataType.Float, GlyphDataType.Bool];

    public static IReadOnlyList<GlyphDataType> ListTypes { get; } = BasicTypes.Concat(
        new[] { GlyphDataType.Location, GlyphDataType.Effect, GlyphDataType.Aggregate, GlyphDataType.List, GlyphDataType.Dictionary }).ToArray();
    public static string TypeName(GlyphDataType type) => type == GlyphDataType.NwObject ? "Object" : type.ToString();

    public static object Normalize(object? value, GlyphDataType type, string? nominal = null) => type switch
    {
        GlyphDataType.Int when value is int or double => Convert.ToInt32(value, CultureInfo.InvariantCulture),
        GlyphDataType.Float when value is int or double => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        GlyphDataType.String when value is string text => text,
        GlyphDataType.Bool when value is bool boolean => boolean,
        GlyphDataType.NwObject when value is uint handle => Nwn.GlyphNwnValue.NormalizeObject(handle),
        GlyphDataType.Location when value is Nwn.GlyphNwnLocation location => location,
        GlyphDataType.Effect when value is Nwn.GlyphNwnEffect effect => effect,
        GlyphDataType.Aggregate when value is GlyphAggregateValue aggregate && aggregate.TypeName == nominal => aggregate,
        GlyphDataType.List when value is GlyphListValue list && $"List<{list.ElementTypeName}>" == nominal => list,
        GlyphDataType.Dictionary when value is GlyphDictionaryValue dictionary &&
            $"Dictionary<{TypeName(dictionary.KeyType)}, {TypeName(dictionary.ValueType)}>" == nominal => dictionary,
        _ => throw new InvalidOperationException($"Expected a {type} collection value.")
    };

    public static GlyphListValue Snapshot(object? raw, GlyphDataType element, GlyphExecutionContext execution, string? nominal = null)
    {
        nominal ??= TypeName(element);
        if (raw is GlyphListValue existing)
        {
            if (existing.ElementType != element || existing.ElementTypeName != nominal) throw new InvalidOperationException("List element type mismatch.");
            execution.ChargeCollection(0, existing.Count);
            return existing;
        }
        if (raw is not IEnumerable sequence) throw new InvalidOperationException("Expected a list.");
        List<object?> items = [];
        foreach (object? item in sequence)
        {
            execution.ChargeCollection(1, items.Count + 1);
            items.Add(Normalize(item, element, nominal));
        }
        return new(element, items, nominal);
    }
}
