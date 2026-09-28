using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public sealed record GlyphPublishedVersion(Guid VersionId, DateTime ActivatedAt, Guid? PreviousVersionId,
    string SourceText, string SourceHash, int LanguageVersion)
{
    public static string Serialize(IReadOnlyList<GlyphProgramVersion> history) => JsonSerializer.Serialize(history.Select(v =>
        new GlyphPublishedVersion(v.VersionId, v.ActivatedAt, v.PreviousVersionId,
            v.Executable.SourceText, v.Executable.SourceHash, v.Executable.LanguageVersion)));

    /// <summary>Rebuild derived snapshots at startup; never compile the unactivated draft.</summary>
    public static void Restore(GlyphDefinition definition, GlyphCompiler compiler, GlyphRuntimeRegistry programs)
    {
        List<GlyphPublishedVersion> stored = JsonSerializer.Deserialize<List<GlyphPublishedVersion>>(definition.PublishedVersionsJson) ?? [];
        List<GlyphProgramVersion> restored = [];
        foreach (var version in stored)
        {
            var result = compiler.Compile(version.SourceText, new($"{definition.Id}.glyph", version.LanguageVersion));
            if (!result.Success || result.SourceHash != version.SourceHash)
                throw new InvalidOperationException($"Stored Glyph version {version.VersionId} failed validation.");
            restored.Add(new(version.VersionId, definition.Id, version.ActivatedAt, version.PreviousVersionId, result.Executable!));
        }
        programs.RestoreIfAbsent(definition.Id, restored, definition.IsActive);
    }
}
