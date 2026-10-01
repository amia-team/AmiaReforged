using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>Typed compiler operations share implementations but expose concrete IR pin types.
/// These operations have no source intrinsics and do not enter the generated API catalog.</summary>
[GlyphModule]
public sealed class RuntimeValueModule : IGlyphModule
{
    public static string Suffix(GlyphDataType type, GlyphDataType? elementType = null, GlyphDataType? keyType = null, GlyphDataType? valueType = null) =>
        type == GlyphDataType.List ? "list_" + (elementType ?? GlyphDataType.NwObject).ToString().ToLowerInvariant()
        : type == GlyphDataType.Dictionary ? "dictionary_" + keyType!.Value.ToString().ToLowerInvariant() + "_" + valueType!.Value.ToString().ToLowerInvariant()
        : type.ToString().ToLowerInvariant();

    public void Configure(GlyphModuleBuilder glyph)
    {
        foreach (GlyphDataType type in Enum.GetValues<GlyphDataType>().Where(t => t is not (GlyphDataType.Exec or GlyphDataType.List or GlyphDataType.Dictionary)))
        {
            Register(glyph, type, null);
            Register(glyph, GlyphDataType.List, type);
            if (type is not (GlyphDataType.NwObject or GlyphDataType.Effect)) glyph.Add(new TypedForEachExecutor(type));
        }
        foreach (var element in new[] { GlyphDataType.List, GlyphDataType.Dictionary })
        {
            Register(glyph, GlyphDataType.List, element);
            glyph.Add(new TypedForEachExecutor(element));
        }
        foreach (var key in GlyphCollections.BasicTypes)
        foreach (var value in GlyphCollections.BasicTypes)
        foreach (string operation in new[] { "write", "read", "field", "with_field" })
            glyph.Add(new RuntimeValueExecutor(operation, GlyphDataType.Dictionary, null, key, value));
    }
    private static void Register(GlyphModuleBuilder glyph, GlyphDataType type, GlyphDataType? element)
    {
        foreach (string operation in new[] { "write", "read", "field", "with_field" })
            glyph.Add(new RuntimeValueExecutor(operation, type, element));
    }

    private sealed class TypedForEachExecutor(GlyphDataType elementType) : ForEachExecutor
    {
        public override string TypeId => "flow.for_each_" + elementType.ToString().ToLowerInvariant();
        public override GlyphNodeDefinition CreateDefinition() => Definition(TypeId, elementType);
    }
}

public sealed class RuntimeValueExecutor(string operation, GlyphDataType type, GlyphDataType? elementType, GlyphDataType? keyType = null, GlyphDataType? valueType = null) : GlyphNodeBase
{
    public override string TypeId => operation switch
    {
        "write" => "local.write_", "read" => "local.read_", "field" => "aggregate.field_", _ => "aggregate.with_"
    } + RuntimeValueModule.Suffix(type, elementType, keyType, valueType);

    private GlyphPin Input(string name) => Pins.In(name, name, type) with { ElementType = elementType, KeyType = keyType, ValueType = valueType };
    private GlyphPin Output() => Pins.Out("value", "Value", type) with { ElementType = elementType, KeyType = keyType, ValueType = valueType };
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = operation, Category = "Compiler",
        CacheOutputs = operation != "read",
        Archetype = operation == "write" ? GlyphNodeArchetype.Action : GlyphNodeArchetype.PureFunction,
        InputPins = operation switch
        {
            "write" => [Pins.ExecIn(), Input("value")],
            "field" => [Pins.In("aggregate", "Aggregate", GlyphDataType.Aggregate)],
            "with_field" => [Pins.In("aggregate", "Aggregate", GlyphDataType.Aggregate), Input("field_value")],
            _ => []
        },
        OutputPins = operation switch
        {
            "write" => [Pins.ExecOut("exec_out", "Then")],
            "with_field" => [Pins.Out("value", "Value", GlyphDataType.Aggregate)],
            _ => [Output()]
        }
    };

    private async Task<object?> ReadValue(GlyphNodeContext cx, string pin) => type switch
    {
        GlyphDataType.Int => await cx.InInt(pin), GlyphDataType.Float => await cx.InFloat(pin),
        GlyphDataType.Bool => await cx.InBool(pin), GlyphDataType.String => await cx.InString(pin),
        GlyphDataType.NwObject => await cx.InObject(pin),
        GlyphDataType.List when elementType is { } element && cx.Prop("snapshot", false) => GlyphCollections.Snapshot(await cx.Raw(pin), element, cx.Execution, Language.Syntax.GlyphTypeNames.Parse(cx.Prop("nominal", $"List<{GlyphCollections.TypeName(element)}>" )).Arguments.Single()),
        GlyphDataType.Dictionary => DictionaryValue(await cx.Raw(pin)), _ => await cx.Raw(pin)
    };
    private GlyphDictionaryValue DictionaryValue(object? raw) => raw is GlyphDictionaryValue dictionary && dictionary.KeyType == keyType && dictionary.ValueType == valueType
        ? dictionary : throw new InvalidOperationException("Dictionary type mismatch.");
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        string nominal = cx.Prop("nominal", type.ToString());
        int slot = cx.Prop("slot", 0);
        if (operation == "write")
        {
            object? value = await ReadValue(cx, "value");
            if (value is GlyphAggregateValue aggregate && aggregate.TypeName != nominal)
                throw new InvalidOperationException($"Aggregate '{aggregate.TypeName}' cannot be stored as '{nominal}'.");
            if (cx.Execution.Locals.TryGetValue(slot, out var previous) && (previous.Type != type || previous.NominalType != nominal))
                throw new InvalidOperationException("A runtime local cannot change type.");
            cx.Execution.Locals[slot] = new(type, nominal, value);
            return new GlyphNodeResult { NextExecPinId = "exec_out", WrittenLocal = slot };
        }
        object? result;
        if (operation == "read")
        {
            if (!cx.Execution.Locals.TryGetValue(slot, out var local))
                throw new InvalidOperationException($"Runtime local {slot} was read before initialization.");
            if (local.Type != type || local.NominalType != nominal)
                throw new InvalidOperationException("Runtime local type mismatch.");
            result = local.Value;
        }
        else
        {
            var aggregate = await cx.Raw("aggregate") as GlyphAggregateValue
                ?? throw new InvalidOperationException("Expected a runtime aggregate.");
            string field = cx.Prop("field", "");
            if (operation == "field")
            {
                if (!aggregate.Fields.TryGetValue(field, out var stored) || stored.Type != type || stored.NominalType != nominal)
                    throw new InvalidOperationException($"Missing or mistyped aggregate field '{field}'.");
                result = stored.Value;
            }
            else result = aggregate.With(field, new(type, nominal, await ReadValue(cx, "field_value")));
        }
        return GlyphNodeResult.Data(new() { ["value"] = result });
    }
}
