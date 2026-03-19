using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Logic;

/// <summary>
/// Returns <c>true</c> if the input text contains <em>any</em> of the supplied patterns
/// (case-insensitive ordinal comparison).  Up to <see cref="MaxPatterns"/> pattern pins are
/// available; only those that are connected (non-null / non-empty) are tested.
/// <para>
/// This is a <see cref="GlyphNodeArchetype.PureFunction"/> — it is lazily evaluated and cached
/// within a single execution pass.
/// </para>
/// </summary>
public class StringContainsExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "logic.string_contains";

    /// <summary>Maximum number of pattern input pins.</summary>
    public const int MaxPatterns = 6;

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? textValue = await resolveInput("text");
        string text = textValue?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(text))
        {
            return Result(false, string.Empty);
        }

        for (int i = 0; i < MaxPatterns; i++)
        {
            object? patternValue = await resolveInput($"pattern_{i}");
            string pattern = patternValue?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(pattern)) continue;

            if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return Result(true, pattern);
            }
        }

        return Result(false, string.Empty);
    }

    public GlyphNodeDefinition CreateDefinition()
    {
        List<GlyphPin> inputs =
        [
            new GlyphPin
            {
                Id = "text", Name = "Text", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input
            }
        ];

        for (int i = 0; i < MaxPatterns; i++)
        {
            inputs.Add(new GlyphPin
            {
                Id = $"pattern_{i}",
                Name = $"Pattern {i}",
                DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input,
                DefaultValue = ""
            });
        }

        return new GlyphNodeDefinition
        {
            TypeId = NodeTypeId,
            DisplayName = "String Contains",
            Category = "Math / Logic",
            Description =
                "Returns true if the input text contains any of the supplied patterns " +
                "(case-insensitive). Connect one or more Pattern pins; unconnected pins are skipped. " +
                "Also outputs the first matched pattern.",
            ColorClass = "node-math",
            Archetype = GlyphNodeArchetype.PureFunction,
            InputPins = inputs,
            OutputPins =
            [
                new GlyphPin
                {
                    Id = "result", Name = "Result", DataType = GlyphDataType.Bool,
                    Direction = GlyphPinDirection.Output
                },
                new GlyphPin
                {
                    Id = "matched", Name = "Matched Pattern", DataType = GlyphDataType.String,
                    Direction = GlyphPinDirection.Output
                }
            ]
        };
    }

    private static GlyphNodeResult Result(bool contains, string matchedPattern) =>
        GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = contains,
            ["matched"] = matchedPattern
        });
}
