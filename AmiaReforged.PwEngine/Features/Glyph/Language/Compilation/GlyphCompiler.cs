using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
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

public static class GlyphLanguageVersion { public const int Current = 5; }
public sealed record GlyphCompilationOptions(string SourceId = "source.glyph", int LanguageVersion = GlyphLanguageVersion.Current, IReadOnlyList<GlyphModuleRevision>? DependencyLock = null);
public sealed record GlyphCompilationResult(IReadOnlyList<GlyphDiagnostic> Diagnostics,
    GlyphCompilationUnitSyntax? SyntaxTree, BoundProgram? BoundProgram, GlyphExecutable? Executable, string SourceHash)
{
    public bool Success => Executable != null && Diagnostics.Count == 0;
    public IReadOnlyDictionary<Guid, SourceSpan>? SourceMap => Executable?.SourceMap;
}
public sealed class GlyphCompiler(IGlyphNodeDefinitionRegistry registry, GlyphGlobalEnvironment? globals = null, GlyphModuleRegistry? modules = null)
{
    public GlyphModuleRegistry Modules { get; } = modules ?? new();
    public GlyphLanguageCatalog Catalog { get; } = new(registry);
    public GlyphGlobalEnvironment Globals { get; } = globals == null || ReferenceEquals(globals, Nwn.GlyphStandardLibrary.Environment) ? Nwn.GlyphStandardLibrary.Environment :
        GlyphGlobalEnvironment.FromDeclarations(globals.ByName.Values.ToArray(), out _, Nwn.GlyphStandardLibrary.Environment);
    public GlyphCompilationResult Compile(string source, GlyphCompilationOptions? options = null)
    {
        options ??= new();
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        SourceSpan start = new(options.SourceId, 0, 0, 1, 1);
        if (source.Length > 128 * 1024 || options.LanguageVersion is not (1 or 2 or 3 or 4 or 5))
            return new([new("GLYPH1007", "Unsupported language version or source exceeds 128 KiB.", start)], null, null, null, hash);
        GlyphLexer lexer = new(source, options.SourceId, options.LanguageVersion >= 2, options.LanguageVersion >= 3, options.LanguageVersion >= 4);
        GlyphParser parser = new(lexer.Lex(), options.LanguageVersion);
        var syntax = parser.Parse();
        List<GlyphDiagnostic> diagnostics = [..lexer.Diagnostics, ..parser.Diagnostics];
        if (diagnostics.Count > 0 || syntax == null) return new(diagnostics.AsReadOnly(), syntax, null, null, hash);

        if (syntax.ModuleName != null)
            return new([new("GLYPH2027", "Module libraries must be validated/published as modules, not event scripts.", syntax.Span)], syntax, null, null, hash);
        IGlyphModuleResolver resolver = options.DependencyLock == null ? Modules.Snapshot : new GlyphModuleSnapshot(options.DependencyLock);
        GlyphModuleBinding? moduleBinding = syntax.Imports.Count == 0 ? null : GlyphModuleBinding.Build(syntax, Globals, Catalog, resolver);
        GlyphGlobalEnvironment environment = moduleBinding?.Environment ?? (syntax.GlobalDeclarations.Count == 0 ? Globals :
            GlyphGlobalEnvironment.FromDeclarations(syntax.GlobalDeclarations, out _, Globals));
        diagnostics.AddRange(moduleBinding?.Diagnostics ?? environment.Diagnostics);
        if (diagnostics.Count > 0) return new(diagnostics.AsReadOnly(), syntax, null, null, hash);
        GlyphBinder binder = new(Catalog, environment, moduleBinding);
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
        GlyphExecutable? executable = diagnostics.Count == 0 ? new(ir, source, hash, map, options.LanguageVersion, moduleBinding?.Dependencies ?? []) : null;
        return new(diagnostics.AsReadOnly(), syntax, bound, executable, hash);
    }
    public GlyphModuleCompilationResult CompileModule(GlyphModuleRevision revision, IGlyphModuleResolver? resolver = null)
    {
        List<GlyphDiagnostic> diagnostics = [];
        var syntax = GlyphModuleBinding.Parse(revision.SourceText, $"{revision.Name}@{revision.RevisionId}.glyph", diagnostics, revision.LanguageVersion);
        if (syntax == null || diagnostics.Count > 0) return new(diagnostics, null, new Dictionary<string, IReadOnlyList<GlyphAvailabilityDto>>());
        var binding = GlyphModuleBinding.Build(syntax, Globals, Catalog, resolver ?? Modules.Snapshot, revision);
        diagnostics.AddRange(binding.Diagnostics);
        GlyphBinder binder = new(Catalog, binding.Environment, binding);
        var availability = diagnostics.Count == 0 ? binder.ValidateModuleFunctions() : new Dictionary<string, IReadOnlyList<GlyphAvailabilityDto>>();
        diagnostics.AddRange(binder.Diagnostics);
        return new(diagnostics.AsReadOnly(), binding, availability);
    }

}
