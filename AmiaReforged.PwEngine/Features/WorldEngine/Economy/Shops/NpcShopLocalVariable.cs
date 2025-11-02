using System;
using System.Text.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed class NpcShopLocalVariable
{
    private NpcShopLocalVariable(
        string name,
        JsonLocalVariableType type,
        int? intValue,
        string? stringValue,
        string? jsonValue)
    {
        Name = name;
        Type = type;
        IntValue = intValue;
        StringValue = stringValue;
        JsonValue = jsonValue;
    }

    public string Name { get; }

    public JsonLocalVariableType Type { get; }

    public int? IntValue { get; }

    public string? StringValue { get; }

    public string? JsonValue { get; }

    public static NpcShopLocalVariable FromDefinition(JsonLocalVariableDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new ArgumentException("Local variable name must not be empty.", nameof(definition));
        }

        JsonElement value = definition.Value;

        return definition.Type switch
        {
            JsonLocalVariableType.Int => FromInt(definition.Name, value),
            JsonLocalVariableType.String => FromString(definition.Name, value),
            JsonLocalVariableType.Json => FromJson(definition.Name, value),
            _ => throw new ArgumentOutOfRangeException(nameof(definition), "Unsupported local variable type."),
        };
    }

    public void WriteTo(IItemLocalVariableWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        switch (Type)
        {
            case JsonLocalVariableType.Int when IntValue.HasValue:
                writer.SetInt(Name, IntValue.Value);
                break;
            case JsonLocalVariableType.String when StringValue != null:
                writer.SetString(Name, StringValue);
                break;
            case JsonLocalVariableType.Json when JsonValue != null:
                writer.SetJson(Name, JsonValue);
                break;
            default:
                throw new InvalidOperationException($"Local variable '{Name}' has no value for type '{Type}'.");
        }
    }

    private static NpcShopLocalVariable FromInt(string name, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int intValue))
        {
            throw new ArgumentException($"Local variable '{name}' must provide a numeric value for type 'Int'.");
        }

        return new NpcShopLocalVariable(name, JsonLocalVariableType.Int, intValue, null, null);
    }

    private static NpcShopLocalVariable FromString(string name, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"Local variable '{name}' must provide a string value for type 'String'.");
        }

        return new NpcShopLocalVariable(name, JsonLocalVariableType.String, null, value.GetString(), null);
    }

    private static NpcShopLocalVariable FromJson(string name, JsonElement value)
    {
        if (value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
        {
            throw new ArgumentException($"Local variable '{name}' must provide an object or array value for type 'Json'.");
        }

        return new NpcShopLocalVariable(name, JsonLocalVariableType.Json, null, null, value.GetRawText());
    }
}
