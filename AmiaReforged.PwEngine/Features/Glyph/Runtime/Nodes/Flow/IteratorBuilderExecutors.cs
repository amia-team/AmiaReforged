using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

[GlyphModule]
public sealed class IteratorBuilderModule : IGlyphModule
{
    public void Configure(GlyphModuleBuilder glyph)
    {
        foreach (var element in GlyphCollections.ListTypes)
            foreach (string operation in new[] { "new", "add", "finish" }) glyph.Add(new IteratorBuilderExecutor(operation, element));
    }
}

public sealed record GlyphListBuilder(GlyphDataType Element, string Nominal, List<object?> Items);

/// <summary>An execution-local buffer allows a fused collect to build its immutable result in linear time.</summary>
public sealed class IteratorBuilderExecutor(string operation, GlyphDataType element) : GlyphNodeBase
{
    public override string TypeId => "iterator." + operation + "_" + element.ToString().ToLowerInvariant();
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Iterator buffer " + operation, Category = "Compiler",
        Archetype = operation == "finish" ? GlyphNodeArchetype.PureFunction : GlyphNodeArchetype.Action,
        InputPins = operation switch
        {
            "new" => [Pins.ExecIn()], "add" => [Pins.ExecIn(), Pins.In("value", "Value", element)], _ => []
        },
        OutputPins = operation == "finish" ? [Pins.Out("value", "Values", GlyphDataType.List) with { ElementType = element }]
            : [Pins.ExecOut("exec_out", "Then")]
    };
    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        int slot = cx.Prop("slot", 0);
        string nominal = cx.Prop("element_type", GlyphCollections.TypeName(element));
        if (operation == "new") cx.Execution.CollectionBuilders[slot] = new(element, nominal, []);
        else
        {
            if (!cx.Execution.CollectionBuilders.TryGetValue(slot, out var builder) || builder.Element != element || builder.Nominal != nominal)
                throw new InvalidOperationException("Iterator buffer is missing or has the wrong element type.");
            if (operation == "add")
            {
                object item = GlyphCollections.Normalize(await cx.Raw("value"), element, nominal);
                cx.Execution.ChargeCollection(1, builder.Items.Count + 1);
                builder.Items.Add(item);
            }
            else
            {
                cx.Execution.ChargeCollection(builder.Items.Count, builder.Items.Count);
                var result = new GlyphListValue(element, builder.Items, nominal);
                cx.Execution.CollectionBuilders.Remove(slot);
                return GlyphNodeResult.Data(new() { ["value"] = result });
            }
        }
        return GlyphNodeResult.Continue("exec_out");
    }
}
