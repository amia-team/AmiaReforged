using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

string? output = null, metadataFile = null, inputMetadata = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
    else if (args[i] == "--from-metadata" && i + 1 < args.Length) inputMetadata = args[++i];
    else if (args[i] == "--metadata" && i + 1 < args.Length) metadataFile = args[++i];
    else
    {
        Console.Error.WriteLine("Usage: Glyph.Docs [--output reference.md] [--metadata metadata.json] [--from-metadata server-metadata.json]");
        return 2;
    }
}
NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
var metadata = inputMetadata == null
    ? GlyphLanguageMetadata.Create(new GlyphBootstrap(new GlyphNodeDefinitionRegistry()).Compiler.Catalog)
    : JsonSerializer.Deserialize<GlyphLanguageMetadataDto>(await File.ReadAllTextAsync(inputMetadata),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
      ?? throw new InvalidDataException("Expected a Glyph language-metadata object.");
string markdown = GlyphApiReference.Generate(metadata);
if (output == null) Console.Write(markdown);
else await File.WriteAllTextAsync(output, markdown);
if (metadataFile != null)
    await File.WriteAllTextAsync(metadataFile, JsonSerializer.Serialize(metadata,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }) + "\n");
return 0;
