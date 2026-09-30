using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;

public abstract class ScalarEqualityExecutor<T>(string typeId, GlyphDataType type) : GlyphPureNode
{
    public override string TypeId => typeId;
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = "Scalar equality", Category = "Math / Logic",
        InputPins = [Pins.In("a", "A", type), Pins.In("b", "B", type), Pins.InString("operator", "Operator", "==")],
        OutputPins = [Pins.Out("result", "Result", GlyphDataType.Bool)]
    };
    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        T a = await cx.In<T>("a"), b = await cx.In<T>("b");
        bool equal = EqualityComparer<T>.Default.Equals(a, b);
        return new() { ["result"] = await cx.InString("operator", "==") == "!=" ? !equal : equal };
    }
}
[GlyphNode]
public sealed class StringEqualityExecutor() : ScalarEqualityExecutor<string>("math.equal_string", GlyphDataType.String);
[GlyphNode]
public sealed class BoolEqualityExecutor() : ScalarEqualityExecutor<bool>("math.equal_bool", GlyphDataType.Bool);
[GlyphNode]
public sealed class ObjectEqualityExecutor() : ScalarEqualityExecutor<uint>("math.equal_object", GlyphDataType.NwObject);
