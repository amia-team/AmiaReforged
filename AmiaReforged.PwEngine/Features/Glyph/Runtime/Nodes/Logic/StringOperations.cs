using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Logic;

[GlyphModule]
public sealed class StringOperationModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        foreach (string operation in new[] { "split", "contains", "length" }) glyph.Add(new StringOperationExecutor(operation));
    }
}

public sealed class StringOperationExecutor(string operation) : GlyphPureNode
{
    public override string TypeId => "string." + operation;
    public override GlyphNodeDefinition CreateDefinition() => new GlyphIntrinsicDescriptor
    {
        TypeId = TypeId, DisplayName = operation, Category = "Text",
        Description = operation switch
        {
            "split" => "Split by a nonempty literal delimiter. Preserve whitespace and empty entries; return List<String>.",
            "contains" => "Whether text contains a fragment using ordinal, case-sensitive comparison.",
            _ => "The number of UTF-16 code units in the string."
        },
        Parameters = operation switch
        {
            "split" => [Pins.InString("text", "Text"), Pins.InString("delimiter", "Delimiter")],
            "contains" => [Pins.InString("text", "Text"), Pins.InString("fragment", "Fragment")],
            _ => [Pins.InString("text", "Text")]
        },
        Results = [operation == "split" ? Pins.Out("value", "Parts", GlyphDataType.List) with { ElementType = GlyphDataType.String }
            : Pins.Out("value", "Value", operation == "contains" ? GlyphDataType.Bool : GlyphDataType.Int)],
        Exports = [new(TypeId, "value", ReceiverMethods: [operation], ReceiverType: GlyphDataType.String, ReceiverPolicy: GlyphReceiverPolicy.LanguageValue)]
    }.CreateDefinition();

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        string text = await cx.InString("text");
        object result;
        if (operation == "split")
        {
            string delimiter = await cx.InString("delimiter");
            if (delimiter.Length == 0) throw new InvalidOperationException("String split requires a nonempty delimiter.");
            List<object?> parts = [];
            int start = 0;
            while (true)
            {
                cx.Execution.CancellationToken.ThrowIfCancellationRequested();
                int next = text.IndexOf(delimiter, start, StringComparison.Ordinal);
                cx.Execution.ChargeCollection(1, parts.Count + 1);
                parts.Add(next < 0 ? text[start..] : text[start..next]);
                if (next < 0) break;
                start = next + delimiter.Length;
            }
            result = new GlyphListValue(GlyphDataType.String, parts);
        }
        else result = operation == "contains" ? text.Contains(await cx.InString("fragment"), StringComparison.Ordinal) : text.Length;
        return new() { ["value"] = result };
    }
}
