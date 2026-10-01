using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

[GlyphModule]
public sealed class CollectionModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        foreach (var element in GlyphCollections.ListTypes)
            foreach (string operation in new[] { "new", "count", "contains", "any", "append", "with", "remove_at", "index", "union", "intersection", "difference", "complement" })
                glyph.Add(new CollectionExecutor(operation, GlyphDataType.List, element));
        foreach (var element in GlyphCollections.BasicTypes)
            foreach (var value in GlyphCollections.BasicTypes)
                foreach (string operation in new[] { "new", "count", "contains_key", "with", "without", "get", "index", "keys", "values" })
                    glyph.Add(new CollectionExecutor(operation, GlyphDataType.Dictionary, value, element));
    }
}

public sealed class CollectionExecutor(string operation, GlyphDataType kind, GlyphDataType value, GlyphDataType? key = null) : GlyphNodeBase
{
    public override string TypeId => Id(operation, kind, value, key);
    public static string Id(string operation, GlyphDataType kind, GlyphDataType value, GlyphDataType? key = null) =>
        "collection." + operation + "_" + RuntimeValueModule.Suffix(kind, kind == GlyphDataType.List ? value : null, key, kind == GlyphDataType.Dictionary ? value : null);
    private GlyphPin Collection(string name, GlyphPinDirection direction) => new()
    {
        Id = name, Name = name, Direction = direction, DataType = kind,
        ElementType = kind == GlyphDataType.List ? value : null, KeyType = key, ValueType = kind == GlyphDataType.Dictionary ? value : null
    };
    private GlyphPin Input(string name, GlyphDataType type) => Pins.In(name, name, type);
    public override GlyphNodeDefinition CreateDefinition()
    {
        List<GlyphPin> inputs = operation == "new" ? [] : [Collection("collection", GlyphPinDirection.Input)];
        if (kind == GlyphDataType.List)
        {
            if (operation is "index" or "with" or "remove_at") inputs.Add(Input("index", GlyphDataType.Int));
            if (operation is "with" or "append" or "contains" or "any") inputs.Add(Input("value", value));
            if (operation is "union" or "intersection" or "difference" or "complement") inputs.Add(Collection("other", GlyphPinDirection.Input));
        }
        else
        {
            if (operation is "index" or "with" or "without" or "get" or "contains_key") inputs.Add(Input("key", key!.Value));
            if (operation is "with" or "get") inputs.Add(Input(operation == "get" ? "fallback" : "value", value));
        }
        GlyphPin result = operation switch
        {
            "count" => Pins.Out("value", "Count", GlyphDataType.Int),
            "contains" or "contains_key" or "any" => Pins.Out("value", "Contains", GlyphDataType.Bool),
            "index" or "get" => Pins.Out("value", "Value", value),
            "keys" or "values" => Pins.Out("value", "Values", GlyphDataType.List) with { ElementType = operation == "keys" ? key : value },
            _ => Collection("value", GlyphPinDirection.Output)
        };
        return new() { TypeId = TypeId, DisplayName = operation, Category = "Compiler", Archetype = GlyphNodeArchetype.PureFunction,
            InputPins = inputs, OutputPins = [result] };
    }
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        object? result;
        if (kind == GlyphDataType.List)
        {
            string nominal = cx.Prop("element_type", GlyphCollections.TypeName(value));
            var list = operation == "new" ? new GlyphListValue(value, [], nominal) : GlyphCollections.Snapshot(await cx.Raw("collection"), value, cx.Execution, nominal);
            int index = operation is "index" or "with" or "remove_at" ? await cx.InInt("index") : 0;
            object? item = operation is "with" or "append" or "contains" or "any" ? GlyphCollections.Normalize(await cx.Raw("value"), value, nominal) : null;
            if (operation is "with" or "remove_at") _ = list[index];
            var other = operation is "union" or "intersection" or "difference" or "complement"
                ? GlyphCollections.Snapshot(await cx.Raw("other"), value, cx.Execution, nominal) : null;
            if (other != null) cx.Execution.ChargeCollection(list.Count + other.Count, System.Math.Max(list.Count, other.Count));
            result = operation switch
            {
                "new" => list,
                "count" => list.Count,
                "contains" or "any" => list.Contains(item, GlyphValueEquality.Instance),
                "index" => list[index],
                "append" => NewList(cx, list.Append(item), list.Count + 1),
                "with" => NewList(cx, list.Select((v, i) => i == index ? item : v), list.Count),
                "remove_at" => NewList(cx, list.Where((_, i) => i != index), list.Count - 1),
                "union" => Materialize(cx, list.Union(other!, GlyphValueEquality.Instance), nominal),
                "intersection" => Materialize(cx, list.Intersect(other!, GlyphValueEquality.Instance), nominal),
                "difference" => Materialize(cx, list.Except(other!, GlyphValueEquality.Instance), nominal),
                "complement" => Materialize(cx, other!.Except(list, GlyphValueEquality.Instance), nominal),
                _ => throw new InvalidOperationException("Unknown list operation.")
            };
        }
        else
        {
            var dictionary = operation == "new" ? new GlyphDictionaryValue(key!.Value, value)
                : await cx.Raw("collection") as GlyphDictionaryValue ?? throw new InvalidOperationException("Expected a dictionary.");
            if (dictionary.KeyType != key || dictionary.ValueType != value) throw new InvalidOperationException("Dictionary type mismatch.");
            object? lookup = operation is "index" or "with" or "without" or "get" or "contains_key" ? GlyphCollections.Normalize(await cx.Raw("key"), key!.Value) : null;
            switch (operation)
            {
                case "new": result = dictionary; break;
                case "count": result = dictionary.Count; break;
                case "contains_key": result = dictionary.Contains(lookup!); break;
                case "index": result = dictionary.Get(lookup!); break;
                case "get":
                    object fallback = GlyphCollections.Normalize(await cx.Raw("fallback"), value);
                    result = dictionary.Contains(lookup!) ? dictionary.Get(lookup!) : fallback; break;
                case "with":
                    cx.Execution.ChargeCollection(1, dictionary.Count + (dictionary.Contains(lookup!) ? 0 : 1));
                    result = dictionary.With(lookup!, GlyphCollections.Normalize(await cx.Raw("value"), value)); break;
                case "without": cx.Execution.ChargeCollection(1, dictionary.Count); result = dictionary.Without(lookup!); break;
                case "keys":
                case "values":
                    cx.Execution.ChargeCollection(dictionary.Count, dictionary.Count);
                    result = new GlyphListValue(operation == "keys" ? key!.Value : value, dictionary.Entries.Select(e => operation == "keys" ? e.Key : e.Value)); break;
                default: throw new InvalidOperationException("Unknown dictionary operation.");
            }
        }
        return GlyphNodeResult.Data(new() { ["value"] = result });
    }
    private GlyphListValue NewList(GlyphNodeContext cx, IEnumerable<object?> items, int count)
    { cx.Execution.ChargeCollection(count, count); return new(value, items, cx.Prop("element_type", GlyphCollections.TypeName(value))); }
    private GlyphListValue Materialize(GlyphNodeContext cx, IEnumerable<object?> items, string nominal)
    {
        List<object?> result = [];
        foreach (var item in items)
        {
            cx.Execution.CancellationToken.ThrowIfCancellationRequested();
            cx.Execution.ChargeCollection(1, result.Count + 1);
            result.Add(item);
        }
        return new(value, result, nominal);
    }
}
