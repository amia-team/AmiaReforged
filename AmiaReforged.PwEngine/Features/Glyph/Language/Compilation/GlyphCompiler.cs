using System.Security.Cryptography;
using System.Text;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Lowering;
using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

public static class GlyphLanguageVersion { public const int Current = 1; }
public sealed record GlyphCompilationOptions(string SourceId = "source.glyph", int LanguageVersion = GlyphLanguageVersion.Current);
public sealed record GlyphCompilationResult(IReadOnlyList<GlyphDiagnostic> Diagnostics,
    GlyphCompilationUnitSyntax? SyntaxTree, BoundProgram? BoundProgram, GlyphExecutable? Executable, string SourceHash)
{
    public bool Success => Executable != null && Diagnostics.Count == 0;
    public IReadOnlyDictionary<Guid, SourceSpan>? SourceMap => Executable?.SourceMap;
}
public sealed class GlyphCompiler(IGlyphNodeDefinitionRegistry registry)
{
    public GlyphLanguageCatalog Catalog { get; } = new(registry);
    public GlyphCompilationResult Compile(string source, GlyphCompilationOptions? options = null)
    {
        options ??= new();
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        SourceSpan start = new(options.SourceId, 0, 0, 1, 1);
        if (source.Length > 128 * 1024 || options.LanguageVersion != GlyphLanguageVersion.Current)
            return new([new("GLYPH1007", "Unsupported language version or source exceeds 128 KiB.", start)], null, null, null, hash);
        GlyphLexer lexer = new(source, options.SourceId);
        GlyphParser parser = new(lexer.Lex());
        var syntax = parser.Parse();
        List<GlyphDiagnostic> diagnostics = [..lexer.Diagnostics, ..parser.Diagnostics];
        if (diagnostics.Count > 0 || syntax == null) return new(diagnostics.AsReadOnly(), syntax, null, null, hash);
        GlyphBinder binder = new(Catalog);
        BoundProgram? bound = binder.Bind(syntax);
        diagnostics.AddRange(binder.Diagnostics);
        if (diagnostics.Count > 0 || bound == null) return new(diagnostics.AsReadOnly(), syntax, bound, null, hash);
        if (!GlyphBoundLimits.IsWithinLimits(bound))
            return new([new("GLYPH1007", "Expanded program exceeds the expression depth or size limit.", start)], syntax, bound, null, hash);
        GlyphGraph ir;
        IReadOnlyDictionary<Guid, SourceSpan> map;
        try { (ir, map) = new GlyphLowerer().Lower(bound); }
        catch (GlyphLowerer.LimitExceededException)
        { return new([new("GLYPH1007", "Expanded program exceeds 4096 operations.", start)], syntax, bound, null, hash); }
        foreach (GlyphIrDiagnostic error in new GlyphIrValidator(registry).Validate(ir))
            diagnostics.Add(new(error.Code, error.Message, error.NodeId is { } id && map.TryGetValue(id, out var span) ? span : start));
        GlyphExecutable? executable = diagnostics.Count == 0 ? new(ir, source, hash, map, options.LanguageVersion) : null;
        return new(diagnostics.AsReadOnly(), syntax, bound, executable, hash);
    }
}
