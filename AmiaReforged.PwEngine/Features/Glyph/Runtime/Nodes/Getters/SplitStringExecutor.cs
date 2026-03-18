using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Splits a string by a delimiter and returns the parts as a list.
/// Useful for parsing object variable values like "tag1,tag2,tag3" for iteration
/// with the <see cref="Nodes.Flow.ForEachExecutor"/> node.
/// Empty entries are removed and parts are trimmed.
/// </summary>
public class SplitStringExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.split_string";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? inputValue = await resolveInput("input");
        object? delimiterValue = await resolveInput("delimiter");

        string input = inputValue?.ToString() ?? string.Empty;
        string delimiter = delimiterValue?.ToString() ?? ",";

        if (string.IsNullOrEmpty(delimiter)) delimiter = ",";

        List<string> parts;
        if (string.IsNullOrEmpty(input))
        {
            parts = [];
        }
        else
        {
            parts = input
                .Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["parts"] = parts,
            ["count"] = parts.Count
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Split String",
        Category = "Getters",
        Description = "Splits a string by a delimiter and returns the parts as a list. " +
                      "Empty entries are removed and parts are trimmed. Default delimiter is comma.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "input", Name = "Input", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input },
            new GlyphPin
            {
                Id = "delimiter", Name = "Delimiter", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input, DefaultValue = ","
            }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "parts", Name = "Parts", DataType = GlyphDataType.List, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "count", Name = "Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
