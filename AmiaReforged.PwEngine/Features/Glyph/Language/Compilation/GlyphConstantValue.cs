using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

/// <summary>
/// The compile-time kind of a global constant. Mirrors the four literal shapes Glyph permits as a
/// constant initializer (Bool, Int, Float, String); there is no runtime/Object/location kind.
/// </summary>
public enum GlyphConstantKind
{
    Bool,
    Int,
    Float,
    String,
    Object,
}

/// <summary>
/// A fully resolved global constant: its declared name, its static compile-time kind, the boxed
/// literal/reference value, and the source span of the constant declaration. This is compiler-side,
/// immutable, resolved data — not a runtime node or executable — so it is safe to keep inside
/// <see cref="GlyphGlobalEnvironment"/> without leaking runtime state into normal Glyph binding.
/// </summary>
public sealed record GlyphConstantValue(
    string Name,
    GlyphConstantKind Kind,
    object Value,
    SourceSpan Span);
