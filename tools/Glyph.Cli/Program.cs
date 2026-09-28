using AmiaReforged.PwEngine.Features.Glyph;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: Glyph.Cli <file.glyph> [file.glyph ...]");
    return 2;
}
// Registration only. No Anvil service container, game engine, or database is started.
NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry());
int exitCode = 0;
foreach (string file in args)
{
    try
    {
        var result = runtime.Compiler.Compile(await File.ReadAllTextAsync(file), new GlyphCompilationOptions(file));
        foreach (var error in result.Diagnostics)
            Console.Error.WriteLine($"{file}({error.Span.Line},{error.Span.Column}): {error.Code}: {error.Message}");
        if (!result.Success) exitCode = 1;
        else Console.WriteLine($"{file}: valid ({result.SourceHash[..12]})");
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Console.Error.WriteLine($"{file}: {ex.Message}"); exitCode = 2; }
}
return exitCode;
