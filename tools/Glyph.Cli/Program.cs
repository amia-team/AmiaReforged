using AmiaReforged.PwEngine.Features.Glyph;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;

List<string> files = []; string? moduleRoot = null; int languageVersion = GlyphLanguageVersion.Current;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--module-root" && i + 1 < args.Length) moduleRoot = args[++i];
    else if (args[i] == "--language-version")
    {
        if (i + 1 >= args.Length || !int.TryParse(args[++i], out languageVersion) || languageVersion is not (1 or 2 or 3))
        { Console.Error.WriteLine("--language-version requires 1, 2, or 3."); return 2; }
    }
    else if (args[i].StartsWith("--", StringComparison.Ordinal)) { Console.Error.WriteLine("Unknown option, missing value, or unsupported language version."); return 2; }
    else files.Add(args[i]);
}
if (files.Count == 0)
{
    Console.Error.WriteLine("Usage: Glyph.Cli [--language-version 1|2|3] [--module-root directory] <file.glyph> [file.glyph ...]");
    return 2;
}
if (moduleRoot != null && languageVersion < 2) { Console.Error.WriteLine("Modules require language version 2 or 3."); return 2; }
// Registration only. No Anvil service container, game engine, or database is started.
NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry());
int exitCode = 0;
void Report(IEnumerable<GlyphDiagnostic> diagnostics)
{
    foreach (var error in diagnostics) Console.Error.WriteLine($"{error.Span.SourceId}({error.Span.Line},{error.Span.Column}): {error.Code}: {error.Message}");
}
try
{
    if (moduleRoot != null)
    {
        List<GlyphModuleRevision> revisions = [];
        foreach (string file in Directory.EnumerateFiles(moduleRoot, "*.glyph", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            string source = await File.ReadAllTextAsync(file); List<GlyphDiagnostic> diagnostics = [];
            var syntax = GlyphModuleBinding.Parse(source, file, diagnostics, languageVersion); Report(diagnostics);
            if (diagnostics.Count > 0 || syntax?.ModuleName == null)
            { Console.Error.WriteLine($"{file}: module root accepts standalone mod libraries."); return 1; }
            string hash = GlyphModuleRevision.Hash(source);
            revisions.Add(new(syntax.ModuleName, new Guid(Convert.FromHexString(hash)[..16]), source, hash, [], languageVersion));
        }
        if (revisions.Select(r => r.Name).Distinct(StringComparer.Ordinal).Count() != revisions.Count)
        { Console.Error.WriteLine("Module root contains duplicate module names."); return 1; }
        runtime.Compiler.Modules.Replace(new(revisions));
        foreach (var revision in revisions)
        {
            var validation = runtime.Compiler.CompileModule(revision); Report(validation.Diagnostics);
            if (!validation.Success) exitCode = 1;
        }
        if (exitCode != 0) return exitCode;
    }
    foreach (string file in files)
    {
        string source = await File.ReadAllTextAsync(file); List<GlyphDiagnostic> diagnostics = [];
        var syntax = GlyphModuleBinding.Parse(source, file, diagnostics, languageVersion);
        if (syntax?.ModuleName != null)
        {
            var validation = runtime.Compiler.CompileModule(GlyphModuleRevision.Create(syntax.ModuleName, source) with { LanguageVersion = languageVersion }); Report(validation.Diagnostics);
            if (!validation.Success) exitCode = 1; else Console.WriteLine($"{file}: valid module");
        }
        else
        {
            var result = runtime.Compiler.Compile(source, new GlyphCompilationOptions(file, languageVersion)); Report(result.Diagnostics);
            if (!result.Success) exitCode = 1;
            else Console.WriteLine($"{file}: valid ({result.Executable!.CompilationHash[..12]})");
        }
    }
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Console.Error.WriteLine(ex.Message); return 2; }
return exitCode;
