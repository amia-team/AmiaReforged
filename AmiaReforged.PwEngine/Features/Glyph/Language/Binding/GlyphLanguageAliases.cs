using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphCallAlias(string Name, string Target, string? ImplicitArgument);

/// <summary>Source spellings shared by binding and editor metadata.</summary>
public static class GlyphLanguageAliases
{
    public static IReadOnlyList<string> Stages { get; } = Platform.GlyphEvents.Get(GlyphEventType.InteractionPipeline).Stages!.Select(s => s.Name).ToArray();
    private static readonly IReadOnlyList<GlyphNodeDefinition> Definitions =
        Platform.GlyphGeneratedRegistry.CreateExecutors().Select(e => e.CreateDefinition()).ToArray();
    private static IEnumerable<Platform.GlyphIntrinsicExport> Intrinsics => Definitions.SelectMany(d => d.Intrinsics);
    public static IReadOnlyList<GlyphCallAlias> Calls { get; } =
        Intrinsics.SelectMany(i => i.CallAliases ?? []).ToArray();
    public static IReadOnlyList<GlyphCallAlias> Properties { get; } =
        Intrinsics.SelectMany(i => i.PropertyAliases ?? []).ToArray();
    public static IReadOnlyDictionary<string, string> Setters { get; } =
        Intrinsics.Where(i => i.WritableAs != null).ToDictionary(i => i.WritableAs!, i => i.Name, StringComparer.Ordinal);
    private static Platform.GlyphIndexerDescriptor Metadata => Intrinsics.Single(i => i.Indexer?.Name == "metadata").Indexer!;
    public static string MetadataName => Metadata.Name;
    public static string MetadataGetter => Metadata.Getter;
    public static string MetadataSetter => Metadata.Setter;
    private static Platform.GlyphContextSchema? Context(GlyphEventType evt) =>
        Definitions
            .FirstOrDefault(d => d.TypeId == (evt == GlyphEventType.InteractionPipeline
                ? Platform.GlyphEvents.Get(evt).Stages![0].EntryTypeId : Platform.GlyphEvents.Get(evt).Entry()))?.ContextSchema;
    public static string ContextPin(string path, GlyphEventType evt) => Context(evt)?.Resolve(path) ?? path;
    public static IEnumerable<string> ContextNames(string pin, GlyphEventType evt) =>
        Context(evt)?.Fields.Any(f => f.PinId == pin) == true ? Context(evt)!.Names(pin) : [pin, "context." + pin, "chaos." + pin];
}
