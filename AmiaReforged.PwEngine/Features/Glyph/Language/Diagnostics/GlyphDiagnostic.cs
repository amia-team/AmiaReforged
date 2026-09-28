namespace AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;

public readonly record struct SourceSpan(string SourceId, int Start, int Length, int Line, int Column);
public sealed record GlyphDiagnostic(string Code, string Message, SourceSpan Span);
