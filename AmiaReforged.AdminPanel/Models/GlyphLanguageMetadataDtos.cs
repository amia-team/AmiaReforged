namespace AmiaReforged.AdminPanel.Models;

public sealed record GlyphLanguageMetadataDto(int LanguageVersion, IReadOnlyList<GlyphFunctionMetadataDto> Functions,
    IReadOnlyList<GlyphEventMetadataDto> Events, IReadOnlyList<GlyphContextMetadataDto> Contexts,
    IReadOnlyList<GlyphIndexerMetadataDto> Indexers,
    IReadOnlyList<GlyphReceiverMethodMetadataDto> ReceiverMethods)
{
    public IReadOnlyList<GlyphConstantMetadataDto> Constants { get; init; } = [];
    public IReadOnlyList<GlyphConstantDomainMetadataDto> ConstantDomains { get; init; } = [];
    public IReadOnlyList<string> Types { get; init; } = [];
    public IReadOnlyList<GlyphAggregateMetadataDto> Aggregates { get; init; } = [];
    public IReadOnlyList<string> Modules { get; init; } = [];
    public IReadOnlyDictionary<string, GlyphSourceSpanDto> SourceLocations { get; init; } = new Dictionary<string, GlyphSourceSpanDto>();
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
    public string Policy { get; init; } = "None";
    public string? Deprecated { get; init; }
}

public sealed record GlyphIndexerMetadataDto(string Name, string Getter, string Setter);

public sealed record GlyphConstantMetadataDto(string Name, string Namespace, string Type, object Value, string Source, string Description);

public sealed record GlyphCursorContextDto(string? Event, string? Stage);
