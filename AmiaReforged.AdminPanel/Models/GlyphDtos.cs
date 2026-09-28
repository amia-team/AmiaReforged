namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// A saved Glyph source script definition.
/// </summary>
public record GlyphDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string EventType,
    string Category,
    string SourceText,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// A binding that links a Glyph definition to a spawn profile.
/// </summary>
public record GlyphBindingDto(
    Guid Id,
    Guid SpawnProfileId,
    Guid GlyphDefinitionId,
    string GlyphName,
    string EventType,
    int Priority);

/// <summary>
/// Request to create a new Glyph definition.
/// </summary>
public record CreateGlyphRequest(
    string Name,
    string EventType,
    string Category = "Encounter",
    string? Description = null,
    string? SourceText = null,
    bool IsActive = false);

/// <summary>
/// Request to update an existing Glyph definition (PATCH-style — null fields are not updated).
/// </summary>
public record UpdateGlyphRequest(
    string? Name = null,
    string? Description = null,
    string? EventType = null,
    string? Category = null,
    string? SourceText = null,
    bool? IsActive = null);

/// <summary>
/// Request to bind a Glyph definition to a spawn profile.
/// </summary>
public record CreateGlyphBindingRequest(
    Guid SpawnProfileId,
    Guid GlyphDefinitionId,
    int Priority = 0);

/// <summary>
/// A binding that links a Glyph definition to a trait tag.
/// </summary>
public record TraitGlyphBindingDto(
    Guid Id,
    string TraitTag,
    Guid GlyphDefinitionId,
    string GlyphName,
    string EventType,
    int Priority);

/// <summary>
/// Request to bind a Glyph definition to a trait tag.
/// </summary>
public record CreateTraitGlyphBindingRequest(
    string TraitTag,
    Guid GlyphDefinitionId,
    int Priority = 0);

/// <summary>
/// Combined response containing both spawn profile, trait, and interaction bindings for a definition.
/// </summary>
public record DefinitionBindingsDto(
    List<GlyphBindingDto> SpawnProfileBindings,
    List<TraitGlyphBindingDto> TraitBindings,
    List<InteractionGlyphBindingDto> InteractionBindings);

/// <summary>
/// A binding that links a Glyph definition to an interaction tag, with optional area scope.
/// </summary>
public record InteractionGlyphBindingDto(
    Guid Id,
    string InteractionTag,
    string? AreaResRef,
    Guid GlyphDefinitionId,
    string GlyphName,
    string EventType,
    int Priority);

/// <summary>
/// Request to bind a Glyph definition to an interaction tag.
/// </summary>
public record CreateInteractionGlyphBindingRequest(
    string InteractionTag,
    Guid GlyphDefinitionId,
    string? AreaResRef = null,
    int Priority = 0);

public record GlyphSourceSpanDto(string SourceId, int Start, int Length, int Line, int Column);
public record GlyphDiagnosticDto(string Code, string Message, GlyphSourceSpanDto Span);
public record CompileGlyphRequest(string SourceText, string? SourceId = null, int LanguageVersion = 1);
public record GlyphCompilationDto(bool Success, List<GlyphDiagnosticDto> Diagnostics, string? SourceHash = null);
public record GlyphVersionDto(Guid VersionId, Guid DefinitionId, DateTime ActivatedAt, Guid? PreviousVersionId,
    string SourceHash, int LanguageVersion, bool IsActive);
public record GlyphTraceDto(Guid DefinitionId, Guid VersionId, DateTime RecordedAt, string? Stage, int Steps, List<string> Entries);
