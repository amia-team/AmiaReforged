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
public sealed class StringContainsExecutor : GlyphPureNode
{
    public const string NodeTypeId = "logic.string_contains";

    /// <summary>Maximum number of pattern input pins.</summary>
    public const int MaxPatterns = 6;

    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        string text = await cx.InString("text");

        if (string.IsNullOrEmpty(text))
        {
            return Result(false, string.Empty);
        }

        for (int i = 0; i < MaxPatterns; i++)
        {
            string pattern = await cx.InString($"pattern_{i}");

            if (string.IsNullOrEmpty(pattern)) continue;

            if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return Result(true, pattern);
            }
        }

        return Result(false, string.Empty);
    }

    public override GlyphNodeDefinition CreateDefinition()
    {
        List<GlyphPin> inputs = [Pins.InString("text", "Text")];

        for (int i = 0; i < MaxPatterns; i++)
        {
            inputs.Add(Pins.InString($"pattern_{i}", $"Pattern {i}", ""));
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
                Pins.Out("result", "Result", GlyphDataType.Bool),
                Pins.Out("matched", "Matched Pattern", GlyphDataType.String),
            ]
        };
    }

    private static Dictionary<string, object?> Result(bool contains, string matchedPattern) =>
        new()
        {
            ["result"] = contains,
            ["matched"] = matchedPattern
        };
}
