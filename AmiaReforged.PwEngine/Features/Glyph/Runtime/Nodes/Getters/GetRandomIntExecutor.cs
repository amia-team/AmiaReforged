using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Generates a random integer between Min (inclusive) and Max (inclusive).
/// </summary>
public class GetRandomIntExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.random_int";
    public string TypeId => NodeTypeId;

    private static readonly Random Rng = Random.Shared;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? minValue = await resolveInput("min");
        object? maxValue = await resolveInput("max");

        int min = Convert.ToInt32(minValue);
        int max = Convert.ToInt32(maxValue);

        if (min > max) (min, max) = (max, min);

        int result = Rng.Next(min, max + 1);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Random Int",
        Category = "Getters",
        Description = "Generates a random integer between Min and Max (inclusive).",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "min", Name = "Min", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "1" },
            new GlyphPin { Id = "max", Name = "Max", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "100" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "result", Name = "Result", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
