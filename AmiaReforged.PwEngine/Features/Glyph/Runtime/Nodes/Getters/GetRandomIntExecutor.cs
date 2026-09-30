using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Generates a random integer between Min (inclusive) and Max (inclusive).
/// </summary>
[GlyphNode]
public partial class GetRandomIntExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.random_int";
    public string TypeId => NodeTypeId;

    private static readonly Random Rng = Random.Shared;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? minValue = await resolveInput(Inputs.Min);
        object? maxValue = await resolveInput(Inputs.Max);

        int min = Convert.ToInt32(minValue);
        int max = Convert.ToInt32(maxValue);

        if (min > max) (min, max) = (max, min);

        int result = Rng.Next(min, max + 1);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = result
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("random", "result", null)
        ],
        DisplayName = "Random Int",
        Category = "Getters",
        Description = "Generates a random integer between Min and Max (inclusive).",
        ColorClass = "node-getter",
        Parameters =
        [
            Pins.In("min", "Min", GlyphDataType.Int, "1"),
            Pins.In("max", "Max", GlyphDataType.Int, "100")
        ],
        Results =
        [
            Pins.Out("result", "Result", GlyphDataType.Int)
        ]
    };
}
