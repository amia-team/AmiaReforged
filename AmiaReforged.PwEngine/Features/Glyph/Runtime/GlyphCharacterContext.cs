using AmiaReforged.PwEngine.Features.Encounters.Models;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Typed domain state. Legacy execution-context properties forward to this same instance.</summary>
public sealed class GlyphCharacterContext
{
    public string? CharacterId { get; set; }
}
