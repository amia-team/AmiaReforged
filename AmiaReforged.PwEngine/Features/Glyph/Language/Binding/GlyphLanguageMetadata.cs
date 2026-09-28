using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphLanguageMetadataDto(int LanguageVersion, IReadOnlyList<GlyphFunctionMetadataDto> Functions,
    IReadOnlyList<GlyphEventMetadataDto> Events, IReadOnlyList<GlyphContextMetadataDto> Contexts,
    IReadOnlyList<GlyphIndexerMetadataDto> Indexers);
public sealed record GlyphParameterMetadataDto(string Name, string DisplayName, string Type, bool Required, string? DefaultValue);
public sealed record GlyphAvailabilityDto(string Event, string? Stage);
public sealed record GlyphFunctionMetadataDto(string Name, string CanonicalName, string Description, string ReturnType,
    string Kind, IReadOnlyList<GlyphParameterMetadataDto> Parameters, string? ImplicitArgument,
    string? RestrictToEventType, string? ScriptCategory, IReadOnlyList<string>? AllowedStages,
    IReadOnlyList<GlyphAvailabilityDto> AvailableIn);
public sealed record GlyphEventMetadataDto(string Name, string EventType, string Category, IReadOnlyList<string> Stages);
public sealed record GlyphContextMetadataDto(string Event, string? Stage, IReadOnlyList<GlyphFieldMetadataDto> Fields);
public sealed record GlyphFieldMetadataDto(string Name, string Type, string Description, string CanonicalName,
    string? Setter);
public sealed record GlyphIndexerMetadataDto(string Name, string Getter, string Setter);

public static class GlyphLanguageMetadata
{
    public static GlyphLanguageMetadataDto Create(GlyphLanguageCatalog catalog)
    {
        var scopes = GlyphLanguageCatalog.Events.SelectMany(e =>
            (e.Value == GlyphEventType.InteractionPipeline ? GlyphLanguageAliases.Stages.Select(s => (string?)s) : [null])
            .Select(stage => (Name: e.Key, Event: e.Value, Stage: stage,
                Entry: stage == null ? GlyphLanguageCatalog.EntryType(e.Value) : "stage.interaction_" + stage))).ToArray();

        bool Available(GlyphLanguageSymbol symbol, GlyphEventType evt, string? stage, string? receiver) =>
            (symbol.Definition.RestrictToEventType == null || symbol.Definition.RestrictToEventType == evt) &&
            (symbol.Definition.ScriptCategory == null || symbol.Definition.ScriptCategory == evt.GetCategory()) &&
            (symbol.AllowedStages == null || symbol.AllowedStages.Contains(stage)) &&
            (receiver == null || catalog.Registry.Get(stage == null ? GlyphLanguageCatalog.EntryType(evt) : "stage.interaction_" + stage)!
                .OutputPins.Any(p => p.DataType != GlyphDataType.Exec && p.Id == GlyphLanguageAliases.ContextPin(receiver, evt)));

        GlyphFunctionMetadataDto Function(GlyphLanguageSymbol symbol, string name, string? receiver) => new(
            name, symbol.Name, symbol.Definition.Description, symbol.ReturnType.Name, symbol.Strategy.ToString(),
            symbol.Parameters.Skip(receiver == null ? 0 : 1).Select(p => new GlyphParameterMetadataDto(
                p.Id, p.Name, GlyphTypeSymbol.From(p.DataType).Name, p.DefaultValue == null, p.DefaultValue)).ToArray(),
            receiver, symbol.Definition.RestrictToEventType?.ToString(), symbol.Definition.ScriptCategory?.ToString(),
            symbol.AllowedStages, scopes.Where(s => Available(symbol, s.Event, s.Stage, receiver))
                .Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray());

        var functions = catalog.Symbols.Select(s => Function(s, s.Name, null))
            .Concat(GlyphLanguageAliases.Calls.Select(a => Function(catalog.Find(a.Target)!, a.Name, a.ImplicitArgument)))
            .OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
        var contexts = scopes.Select(scope =>
        {
            var fields = catalog.Registry.Get(scope.Entry)!.OutputPins.Where(p => p.DataType != GlyphDataType.Exec)
                .SelectMany(pin => GlyphLanguageAliases.ContextNames(pin.Id, scope.Event).Select(name =>
                {
                    string? setter = GlyphLanguageAliases.Setters.GetValueOrDefault(name);
                    if (setter != null && !Available(catalog.Find(setter)!, scope.Event, scope.Stage, null)) setter = null;
                    return new GlyphFieldMetadataDto(name, GlyphTypeSymbol.From(pin.DataType).Name, pin.Name, pin.Id, setter);
                })).ToList();
            foreach (var alias in GlyphLanguageAliases.Properties)
            {
                var symbol = catalog.Find(alias.Target)!;
                if (Available(symbol, scope.Event, scope.Stage, alias.ImplicitArgument))
                    fields.Add(new(alias.Name, symbol.ReturnType.Name, symbol.Definition.Description, alias.Target, null));
            }
            return new GlyphContextMetadataDto(scope.Name, scope.Stage, fields.OrderBy(f => f.Name, StringComparer.Ordinal).ToArray());
        }).ToArray();
        return new(GlyphLanguageVersion.Current, functions,
            GlyphLanguageCatalog.Events.Select(e => new GlyphEventMetadataDto(e.Key, e.Value.ToString(), e.Value.GetCategory().ToString(),
                e.Value == GlyphEventType.InteractionPipeline ? GlyphLanguageAliases.Stages : [])).ToArray(), contexts,
            [new(GlyphLanguageAliases.MetadataName, GlyphLanguageAliases.MetadataGetter, GlyphLanguageAliases.MetadataSetter)]);
    }
}
