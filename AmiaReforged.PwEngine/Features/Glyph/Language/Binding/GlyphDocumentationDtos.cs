namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphDocumentationPackDto(string Name, string ArchiveDate, string License,
    string Notice, string LicenseText, string History, IReadOnlyDictionary<string, GlyphFunctionDocumentationDto> Functions);
public sealed record GlyphFunctionDocumentationDto(string Title, string SourceUrl, int Revision, string ArchiveDate,
    string Summary, string NativeSignature, IReadOnlyList<GlyphNativeParameterDocumentationDto> Parameters,
    IReadOnlyList<GlyphDocumentationSectionDto> Sections, IReadOnlyList<string> Credits, string OriginalSource);
public sealed record GlyphNativeParameterDocumentationDto(string Name, string Description);
public sealed record GlyphDocumentationSectionDto(string Title, IReadOnlyList<GlyphDocumentationBlockDto> Blocks);
public sealed record GlyphDocumentationBlockDto(string Kind, string Text);
