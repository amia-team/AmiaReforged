namespace AmiaReforged.AdminPanel.Models;

public sealed record GlyphLanguageMetadataDto(int LanguageVersion, IReadOnlyList<GlyphFunctionMetadataDto> Functions,
    IReadOnlyList<GlyphEventMetadataDto> Events, IReadOnlyList<GlyphContextMetadataDto> Contexts,
    IReadOnlyList<GlyphIndexerMetadataDto> Indexers,
    IReadOnlyList<GlyphReceiverMethodMetadataDto> ReceiverMethods);
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
public sealed record GlyphReceiverMethodMetadataDto(string Name, string ReceiverType, string CanonicalName,
    string Description, string ReturnType, string Kind, IReadOnlyList<GlyphParameterMetadataDto> Parameters,
    IReadOnlyList<GlyphAvailabilityDto> AvailableIn);
