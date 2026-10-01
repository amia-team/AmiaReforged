using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

public static class GlyphBuiltins
{
    private static readonly SourceSpan Span = new("Standard/option.glyph", 0, 0, 1, 1);
    public const string OptionSource = "type Option<T> { Some { value: T } None {} }";
    public static AdtDeclarationSyntax Option { get; } = new("Option",
        [new("Some", [new("value", "T", Span)], Span), new("None", [], Span)], Span)
        { TypeParameters = ["T"] };
}
