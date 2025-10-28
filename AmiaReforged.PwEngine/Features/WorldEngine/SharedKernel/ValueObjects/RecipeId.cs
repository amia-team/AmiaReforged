using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Strongly-typed identifier for recipes in the crafting system
/// </summary>
public readonly record struct RecipeId
{
    public string Value { get; }

    [JsonConstructor]
    public RecipeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("RecipeId cannot be null or whitespace", nameof(value));

        Value = value;
    }

    public static implicit operator string(RecipeId recipeId) => recipeId.Value;
    public static explicit operator RecipeId(string value) => new(value);

    public override string ToString() => Value;
}

