using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;
[GlyphNode]
public sealed class ForEachEffectExecutor : ForEachExecutor
{
    public override string TypeId => "flow.for_each_effect";
    public override GlyphNodeDefinition CreateDefinition() => Definition(TypeId, GlyphDataType.Effect);
}
