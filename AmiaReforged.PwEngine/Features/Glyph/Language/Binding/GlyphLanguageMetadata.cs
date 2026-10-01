using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphLanguageMetadataDto(int LanguageVersion, IReadOnlyList<GlyphFunctionMetadataDto> Functions,
    IReadOnlyList<GlyphEventMetadataDto> Events, IReadOnlyList<GlyphContextMetadataDto> Contexts,
    IReadOnlyList<GlyphIndexerMetadataDto> Indexers,
    IReadOnlyList<GlyphReceiverMethodMetadataDto> ReceiverMethods)
{
    public IReadOnlyList<Nwn.GlyphNwnConstant> Constants { get; init; } = [];
    public IReadOnlyList<GlyphConstantDomainMetadataDto> ConstantDomains { get; init; } = [];
    public IReadOnlyList<string> Types { get; init; } = [];
    public IReadOnlyList<Modules.GlyphAggregateMetadata> Aggregates { get; init; } = [];
    public string? NwnApiVersion { get; init; }
    public GlyphDocumentationPackDto? Documentation { get; init; }
    public IReadOnlyList<GlyphWritableStateMetadataDto> WritableState { get; init; } = [];
}
public sealed record GlyphConstantDomainMetadataDto(string Name, IReadOnlyList<string> Types, int Count);
public sealed record GlyphWritableStateMetadataDto(string Name, string Type, string Setter,
    IReadOnlyList<GlyphAvailabilityDto> AvailableIn);
public sealed record GlyphParameterMetadataDto(string Name, string DisplayName, string Type, bool Required, string? DefaultValue)
{
    public string? SourceParameter { get; init; }
}
public sealed record GlyphAvailabilityDto(string Event, string? Stage);
public sealed record GlyphFunctionMetadataDto(string Name, string CanonicalName, string Description, string ReturnType,
    string Kind, IReadOnlyList<GlyphParameterMetadataDto> Parameters, string? ImplicitArgument,
    string? RestrictToEventType, string? ScriptCategory, IReadOnlyList<string>? AllowedStages,
    IReadOnlyList<GlyphAvailabilityDto> AvailableIn)
{
    public IReadOnlyList<string> TypeParameters { get; init; } = [];
    public string? DeclaringType { get; init; }
    public string? Source { get; init; }
    public string? DocumentationSource { get; init; }
    public string? Backend { get; init; }
    public string? Deprecated { get; init; }
    public string Category { get; init; } = "";
}
public sealed record GlyphEventMetadataDto(string Name, string EventType, string Category, IReadOnlyList<string> Stages);
public sealed record GlyphContextMetadataDto(string Event, string? Stage, IReadOnlyList<GlyphFieldMetadataDto> Fields);
public sealed record GlyphFieldMetadataDto(string Name, string Type, string Description, string CanonicalName,
    string? Setter);
public sealed record GlyphReceiverMethodMetadataDto(string Name, string ReceiverType, string CanonicalName,
    string Description, string ReturnType, string Kind, IReadOnlyList<GlyphParameterMetadataDto> Parameters,
    IReadOnlyList<GlyphAvailabilityDto> AvailableIn)
{
    public IReadOnlyList<string> TypeParameters { get; init; } = [];
    public string Policy { get; init; } = "None";
    public string? Deprecated { get; init; }
}

public sealed record GlyphIndexerMetadataDto(string Name, string Getter, string Setter);

public static class GlyphLanguageMetadata
{
    public static GlyphLanguageMetadataDto Create(GlyphLanguageCatalog catalog)
    {
        var scopes = Platform.GlyphEvents.All.SelectMany(e =>
            (e.Stages == null ? new[] { (string?)null } : e.Stages.Select(s => (string?)s.Name))
            .Select(stage => (Name: e.Name, Event: e.EventType, Stage: stage, Entry: e.Entry(stage)))).ToArray();

        bool Available(GlyphLanguageSymbol symbol, GlyphEventType evt, string? stage, string? receiver) =>
            (symbol.Definition.RestrictToEventType == null || symbol.Definition.RestrictToEventType == evt) &&
            (symbol.Definition.ScriptCategory == null || symbol.Definition.ScriptCategory == evt.GetCategory()) &&
            (symbol.AllowedStages == null || symbol.AllowedStages.Contains(stage)) &&
            (receiver == null || catalog.Registry.Get(Platform.GlyphEvents.Get(evt).Entry(stage))!
                .OutputPins.Any(p => p.DataType != GlyphDataType.Exec && p.Id == catalog.ContextPin(receiver, Platform.GlyphEvents.Get(evt).Entry(stage))));

        GlyphFunctionMetadataDto Function(GlyphLanguageSymbol symbol, string name, string? receiver) => new(
            name, symbol.Name, symbol.Definition.Description, symbol.ReturnType.Name, symbol.Strategy.ToString(),
            symbol.Parameters.Skip(receiver == null ? 0 : 1).Select(p => new GlyphParameterMetadataDto(
                p.Id, p.Name, GlyphTypeSymbol.From(p).Name, p.DefaultValue == null, p.DefaultValue) { SourceParameter = p.SourceParameter }).ToArray(),
            receiver, symbol.Definition.RestrictToEventType?.ToString(), symbol.Definition.ScriptCategory?.ToString(),
            symbol.AllowedStages, scopes.Where(s => Available(symbol, s.Event, s.Stage, receiver))
                .Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray())
        { Source = symbol.Definition.Source, DocumentationSource = symbol.Definition.Intrinsics.FirstOrDefault(e => e.Name == symbol.Name)?.DocumentationSource, Backend = symbol.Definition.Backend, Deprecated = symbol.Definition.Deprecated, Category = symbol.Definition.Category };

        var functions = catalog.Symbols.Select(s => Function(s, s.Name, null))
            .Concat(catalog.CallAliases.Select(a => Function(catalog.Find(a.Target)!, a.Name, a.ImplicitArgument)))
            .OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
        var option = GlyphBuiltins.Option;
        var optionScopes = scopes.Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray();
        functions = functions.Concat(option.Variants.Select(v => new GlyphFunctionMetadataDto("Option." + v.Name, "Option." + v.Name,
            "Optional value. Some carries a value; None carries no value. Object validity may change after construction.", "Option<T>", "Value",
            v.Fields.Select(f => new GlyphParameterMetadataDto(f.Name, f.Name, f.TypeName, true, null)).ToArray(), null, null, null, null, optionScopes)
            { TypeParameters = option.TypeParameters, DeclaringType = "Option<T>", Source = option.Span.SourceId, Category = "Language" })).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
        var contexts = scopes.Select(scope =>
        {
            var fields = catalog.Registry.Get(scope.Entry)!.OutputPins.Where(p => p.DataType != GlyphDataType.Exec)
                .SelectMany(pin => (catalog.Registry.Get(scope.Entry)!.ContextSchema?.Names(pin.Id) ?? new[] { pin.Id, "context." + pin.Id, "chaos." + pin.Id }).Select(name =>
                {
                    string? setter = catalog.Setters.GetValueOrDefault(name);
                    if (setter != null && !Available(catalog.Find(setter)!, scope.Event, scope.Stage, null)) setter = null;
                    return new GlyphFieldMetadataDto(name, GlyphTypeSymbol.From(pin.DataType).Name, pin.Name, pin.Id, setter);
                })).ToList();
            foreach (var alias in catalog.PropertyAliases)
            {
                var symbol = catalog.Find(alias.Target)!;
                if (Available(symbol, scope.Event, scope.Stage, alias.ImplicitArgument))
                    fields.Add(new(alias.Name, symbol.ReturnType.Name, symbol.Definition.Description, alias.Target, null));
            }
            return new GlyphContextMetadataDto(scope.Name, scope.Stage, fields.OrderBy(f => f.Name, StringComparer.Ordinal).ToArray());
        }).ToArray();
        var receiverMethods = catalog.ReceiverMethods.Select(rm =>
        {
            GlyphLanguageSymbol symbol = catalog.Find(rm.Target)
                ?? throw new InvalidOperationException($"Registered receiver method '{rm.Name}' has no target '{rm.Target}'.");
            return new GlyphReceiverMethodMetadataDto(
                rm.Name, GlyphTypeSymbol.From(rm.ReceiverType).Name, rm.Target, symbol.Definition.Description,
                symbol.ReturnType.Name, symbol.Strategy.ToString(),
                symbol.Parameters.Skip(1).Select(p => new GlyphParameterMetadataDto(
                    p.Id, p.Name, GlyphTypeSymbol.From(p).Name, p.DefaultValue == null, p.DefaultValue) { SourceParameter = p.SourceParameter })
                    .ToArray(),
                scopes.Where(s => Available(symbol, s.Event, s.Stage, null))
                    .Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray()) { Policy = rm.Policy.ToString(), Deprecated = symbol.Definition.Deprecated };
        }).ToArray();
        var collectionMethods = catalog.Registry.GetAll().Where(d => d.TypeId.StartsWith("collection.", StringComparison.Ordinal) &&
            d.InputPins.FirstOrDefault()?.DataType == GlyphDataType.Dictionary && !d.TypeId.StartsWith("collection.index_", StringComparison.Ordinal))
            .Select(d => new GlyphReceiverMethodMetadataDto(d.DisplayName, GlyphTypeSymbol.From(d.InputPins[0]).Name, d.TypeId,
                "Immutable collection operation; updates return a new value.", GlyphTypeSymbol.From(d.OutputPins[0]).Name, "Value",
                d.InputPins.Skip(1).Select(p => new GlyphParameterMetadataDto(p.Id, p.Name, GlyphTypeSymbol.From(p).Name, true, null)).ToArray(),
                scopes.Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray())).ToArray();
        var allScopes = scopes.Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray();
        var genericMethods = catalog.Registry.GetAll().Where(d => d.TypeId.StartsWith("collection.", StringComparison.Ordinal) &&
            d.InputPins.FirstOrDefault()?.DataType == GlyphDataType.List && d.InputPins[0].ElementType == GlyphDataType.Int &&
            !d.TypeId.StartsWith("collection.index_", StringComparison.Ordinal))
            .Select(d => new GlyphReceiverMethodMetadataDto(d.DisplayName, "List<T>", "language.list." + d.DisplayName,
                "Immutable list operation. Elements and other lists must have exactly the same type. Set operations return distinct values in first-occurrence order.",
                d.OutputPins[0].DataType == GlyphDataType.List ? "List<T>" : d.OutputPins[0].DataType == GlyphDataType.Int ? "Int" : "Bool", "Value",
                d.InputPins.Skip(1).Select(p => new GlyphParameterMetadataDto(p.Id, p.Name, p.Id switch { "other" => "List<T>", "index" => "Int", _ => "T" }, true, null)).ToArray(),
                allScopes) { TypeParameters = ["T"], Policy = "LanguageValue" }).ToArray();
        GlyphReceiverMethodMetadataDto Pipeline(string name, string receiver, string result, string description, params GlyphParameterMetadataDto[] parameters) =>
            new(name, receiver, "language.iterator." + name, description, result, "Value", parameters, allScopes)
            { TypeParameters = name == "map" ? ["T", "U"] : ["T"], Policy = "LanguageValue" };
        var pipelineMethods = new[]
        {
            Pipeline("iter", "List<T>", "Iterator<T>", "Create a lazy, reusable iterator recipe over a snapshot of the list."),
            Pipeline("filter", "Iterator<T>", "Iterator<T>", "Keep elements satisfying a Bool predicate. Capture local values when the recipe is constructed.", new GlyphParameterMetadataDto("predicate", "Predicate", "Fn<T, Bool>", true, null)),
            Pipeline("map", "Iterator<T>", "Iterator<U>", "Transform each element; infer the result element type from the selector.", new GlyphParameterMetadataDto("selector", "Selector", "Fn<T, U>", true, null)),
            Pipeline("filter", "List<T>", "Iterator<T>", "Start a lazy recipe retaining elements satisfying a Bool predicate.", new GlyphParameterMetadataDto("predicate", "Predicate", "Fn<T, Bool>", true, null)),
            Pipeline("map", "List<T>", "Iterator<U>", "Start a lazy recipe transforming each element.", new GlyphParameterMetadataDto("selector", "Selector", "Fn<T, U>", true, null)),
            Pipeline("collect", "List<T>", "List<T>", "Return an immutable snapshot of the list."),
            Pipeline("collect", "Iterator<T>", "List<T>", "Evaluate the recipe once into an immutable list, preserving order and duplicates."),
            Pipeline("any", "Iterator<T>", "Bool", "Stop at the first element satisfying the predicate; without a predicate, test whether any element exists.", new GlyphParameterMetadataDto("predicate", "Predicate", "Fn<T, Bool>", false, null)),
            Pipeline("any", "List<T>", "Bool", "Test a predicate with short-circuiting, or test whether the list is nonempty.", new GlyphParameterMetadataDto("predicate", "Predicate", "Fn<T, Bool>", false, null))
        };
        return new(GlyphLanguageVersion.Current, functions,
            Platform.GlyphEvents.All.Select(e => new GlyphEventMetadataDto(e.Name, e.EventType.ToString(), e.Category.ToString(),
                e.Stages?.Select(s => s.Name).ToArray() ?? [])).ToArray(), contexts,
            catalog.Indexers.Select(i => new GlyphIndexerMetadataDto(i.Name, i.Getter, i.Setter)).ToArray(),
            receiverMethods.Concat(collectionMethods).Concat(genericMethods).Concat(pipelineMethods).ToArray())
        {
            Documentation = Documentation.GlyphLexicon.ForSources(functions.Select(f => f.DocumentationSource ?? f.Source)),
            Constants = Nwn.GlyphNwnSurface.Constants,
            ConstantDomains = Nwn.GlyphNwnSurface.Constants.GroupBy(c => c.Namespace).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new GlyphConstantDomainMetadataDto(g.Key, g.Select(c => c.Type).Distinct().OrderBy(t => t, StringComparer.Ordinal).ToArray(), g.Count())).ToArray(),
            Aggregates = [new("Option", [], option.Variants) { TypeParameters = option.TypeParameters }],
            Types = new[] { "Option<T>", "Void", "Bool", "Int", "Float", "String", "Object", "Location", "Effect", "List<Effect>", "List<Location>", "List<T>", "Iterator<T>" }
                .Concat(Runtime.GlyphCollections.BasicTypes.Select(t => GlyphTypeSymbol.List(GlyphTypeSymbol.From(t)).Name))
                .Concat(Runtime.GlyphCollections.BasicTypes.SelectMany(k => Runtime.GlyphCollections.BasicTypes.Select(v => GlyphTypeSymbol.Dictionary(GlyphTypeSymbol.From(k), GlyphTypeSymbol.From(v)).Name))).ToArray(),
            NwnApiVersion = Nwn.GlyphNwnSurface.ApiVersion,
            WritableState = catalog.Setters.Select(pair =>
            {
                var setter = catalog.Find(pair.Value)!;
                return new GlyphWritableStateMetadataDto(pair.Key, GlyphTypeSymbol.From(setter.Parameters.Single().DataType).Name,
                    pair.Value, scopes.Where(s => Available(setter, s.Event, s.Stage, null))
                        .Select(s => new GlyphAvailabilityDto(s.Name, s.Stage)).ToArray());
            }).OrderBy(s => s.Name, StringComparer.Ordinal).ToArray()
        };
    }
}
