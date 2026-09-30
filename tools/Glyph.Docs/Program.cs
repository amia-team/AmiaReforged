using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using AmiaReforged.PwEngine.Features.Glyph;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

string? output = null, metadataFile = null, inputMetadata = null, globalFile = null, coverageFile = null, snapshotFile = null, compareFile = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
    else if (args[i] == "--from-metadata" && i + 1 < args.Length) inputMetadata = args[++i];
    else if (args[i] == "--metadata" && i + 1 < args.Length) metadataFile = args[++i];
    else if (args[i] == "--global" && i + 1 < args.Length) globalFile = args[++i];
    else if (args[i] == "--nwn-coverage" && i + 1 < args.Length) coverageFile = args[++i];
    else if (args[i] == "--nwn-snapshot" && i + 1 < args.Length) snapshotFile = args[++i];
    else if (args[i] == "--compare-nwn" && i + 1 < args.Length) compareFile = args[++i];
    else
    {
        Console.Error.WriteLine("Usage: Glyph.Docs [--output reference.md] [--metadata metadata.json] [--from-metadata server-metadata.json] [--global global.glyph] [--nwn-coverage coverage.md] [--nwn-snapshot snapshot.json] [--compare-nwn baseline.json]");
        return 2;
    }
}
NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
var metadata = inputMetadata == null
    ? new GlyphBootstrap(new GlyphNodeDefinitionRegistry()).LanguageMetadata
    : JsonSerializer.Deserialize<GlyphLanguageMetadataDto>(await File.ReadAllTextAsync(inputMetadata),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
      ?? throw new InvalidDataException("Expected a Glyph language-metadata object.");
if (globalFile != null) await File.WriteAllTextAsync(globalFile, GlyphStandardLibrary.SourceDocument());
if (coverageFile != null) await File.WriteAllTextAsync(coverageFile, GlyphNwnCoverage.Report());
if (snapshotFile != null) await File.WriteAllTextAsync(snapshotFile, GlyphNwnCoverage.SnapshotJson());
if (compareFile != null)
{
    var previous = JsonSerializer.Deserialize<GlyphNwnSnapshot>(await File.ReadAllTextAsync(compareFile), GlyphNwnCoverage.JsonOptions)
        ?? throw new InvalidDataException("Expected an NWN API snapshot.");
    var changes = GlyphNwnCoverage.Compare(previous);
    foreach (var change in changes) Console.WriteLine(change);
    if (changes.Count == 0) Console.WriteLine("NWN API and bindings match the reviewed snapshot.");
    if (output == null && metadataFile == null && globalFile == null && coverageFile == null && snapshotFile == null) return changes.Count == 0 ? 0 : 1;
}
string markdown = GlyphApiReference.Generate(metadata);
if (output == null) Console.Write(markdown);
else await File.WriteAllTextAsync(output, markdown);
if (metadataFile != null)
    await File.WriteAllTextAsync(metadataFile, JsonSerializer.Serialize(metadata,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }) + "\n");
return 0;
