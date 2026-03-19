namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// A saved Glyph visual script definition.
/// </summary>
public record GlyphDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    string EventType,
    string Category,
    string GraphJson,
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
/// A node definition entry from the Glyph node catalog (for the editor palette).
/// </summary>
public record GlyphNodeCatalogEntryDto(
    string TypeId,
    string DisplayName,
    string Category,
    string Description,
    string ColorClass,
    bool IsSingleton,
    string? RestrictToEventType,
    string? ScriptCategory,
    List<GlyphPinDto> InputPins,
    List<GlyphPinDto> OutputPins,
    List<GlyphPropertyDefinitionDto> Properties,
    string? ContextSourceTypeId = null);

/// <summary>
/// A configurable property on a Glyph node (dropdown or free-text, shown in the property panel).
/// </summary>
public record GlyphPropertyDefinitionDto(
    string Id,
    string DisplayName,
    string DefaultValue,
    List<string> AllowedValues);

/// <summary>
/// A pin on a Glyph node definition.
/// </summary>
public record GlyphPinDto(
    string Id,
    string Name,
    string DataType,
    string Direction,
    string? DefaultValue,
    bool AllowMultipleConnections);

/// <summary>
/// Request to create a new Glyph definition.
/// </summary>
public record CreateGlyphRequest(
    string Name,
    string EventType,
    string Category = "Encounter",
    string? Description = null,
    string? GraphJson = null,
    bool IsActive = false);

/// <summary>
/// Request to update an existing Glyph definition (PATCH-style — null fields are not updated).
/// </summary>
public record UpdateGlyphRequest(
    string? Name = null,
    string? Description = null,
    string? EventType = null,
    string? Category = null,
    string? GraphJson = null,
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
