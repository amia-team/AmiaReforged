using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Typed domain state. Legacy execution-context properties forward to this same instance.</summary>
public sealed class TraitGlyphContext
{
    public string? TraitTag { get; set; }
    public uint TargetCreature { get; set; }
    public uint Killer { get; set; } = NWScript.OBJECT_INVALID;
    public List<GlyphNwnEffect> Effects { get; } = [];
}
