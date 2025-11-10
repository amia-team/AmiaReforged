using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways;

/// <summary>
/// Gateway for persona identity and relationship operations.
/// Simplifies access to persona data, character-to-player mappings, and aggregate persona information.
/// </summary>
public interface IPersonaGateway
{
    // === Basic Persona Lookup ===

    /// <summary>
    /// Gets a persona by its ID.
    /// </summary>
    Task<PersonaInfo?> GetPersonaAsync(PersonaId personaId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple personas by their IDs.
    /// </summary>
    Task<IReadOnlyDictionary<PersonaId, PersonaInfo>> GetPersonasAsync(
        IEnumerable<PersonaId> personaIds, CancellationToken ct = default);

    /// <summary>
    /// Checks if a persona exists.
    /// </summary>
    Task<bool> ExistsAsync(PersonaId personaId, CancellationToken ct = default);

    // === Player-Character Mappings ===

    /// <summary>
    /// Gets all characters owned by a player (by CD key).
    /// </summary>
    Task<IReadOnlyList<CharacterPersonaInfo>> GetPlayerCharactersAsync(
        string cdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets the player (CD key) who owns a specific character.
    /// </summary>
    Task<PlayerPersonaInfo?> GetCharacterOwnerAsync(
        CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets the player (CD key) who owns a specific character persona.
    /// </summary>
    Task<PlayerPersonaInfo?> GetCharacterOwnerAsync(
        PersonaId characterPersonaId, CancellationToken ct = default);

    /// <summary>
    /// Maps a character ID to its persona ID.
    /// </summary>
    Task<PersonaId?> GetCharacterPersonaIdAsync(
        CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Maps a persona ID to its character ID (if it's a character persona).
    /// </summary>
    Task<CharacterId?> GetPersonaCharacterIdAsync(
        PersonaId personaId, CancellationToken ct = default);

    // === Character Identity ===

    /// <summary>
    /// Gets detailed character identity information.
    /// </summary>
    Task<CharacterIdentityInfo?> GetCharacterIdentityAsync(
        CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed character identity information by persona ID.
    /// </summary>
    Task<CharacterIdentityInfo?> GetCharacterIdentityByPersonaAsync(
        PersonaId personaId, CancellationToken ct = default);

    // === Player Identity ===

    /// <summary>
    /// Gets player information by CD key.
    /// </summary>
    Task<PlayerPersonaInfo?> GetPlayerAsync(string cdKey, CancellationToken ct = default);

    /// <summary>
    /// Gets player information by player persona ID.
    /// </summary>
    Task<PlayerPersonaInfo?> GetPlayerByPersonaAsync(
        PersonaId personaId, CancellationToken ct = default);

    // === Aggregate Holdings (Future Enhancement) ===

    /// <summary>
    /// Gets all holdings (properties, rentals, etc.) for a persona.
    /// Note: Implementation to be added as property system is developed.
    /// </summary>
    Task<PersonaHoldingsInfo> GetPersonaHoldingsAsync(
        PersonaId personaId, CancellationToken ct = default);

    /// <summary>
    /// Gets aggregate holdings across all of a player's characters.
    /// Note: Implementation to be added as property system is developed.
    /// </summary>
    Task<PlayerAggregateHoldingsInfo> GetPlayerAggregateHoldingsAsync(
        string cdKey, CancellationToken ct = default);
}

// === DTOs for Persona Gateway ===

/// <summary>
/// Basic persona information.
/// </summary>
public record PersonaInfo
{
    public required PersonaId Id { get; init; }
    public required PersonaType Type { get; init; }
    public required string DisplayName { get; init; }
    public DateTime? CreatedUtc { get; init; }
    public DateTime? UpdatedUtc { get; init; }
}

/// <summary>
/// Character persona information with character-specific details.
/// </summary>
public record CharacterPersonaInfo : PersonaInfo
{
    public required CharacterId CharacterId { get; init; }
    public required string CdKey { get; init; }
}

/// <summary>
/// Player persona information.
/// </summary>
public record PlayerPersonaInfo : PersonaInfo
{
    public required string CdKey { get; init; }
    public DateTime? LastSeenUtc { get; init; }
    public int CharacterCount { get; init; }
}

/// <summary>
/// Detailed character identity information.
/// </summary>
public record CharacterIdentityInfo
{
    public required CharacterId CharacterId { get; init; }
    public required PersonaId PersonaId { get; init; }
    public required string FirstName { get; init; }
    public string? LastName { get; init; }
    public required string FullName { get; init; }
    public required string Description { get; init; }
    public required string CdKey { get; init; }
    public DateTime? CreatedUtc { get; init; }
    public DateTime? LastSeenUtc { get; init; }
}

/// <summary>
/// Holdings information for a persona (properties, rentals, etc.).
/// </summary>
public record PersonaHoldingsInfo
{
    public required PersonaId PersonaId { get; init; }
    public IReadOnlyList<PropertyHoldingInfo> OwnedProperties { get; init; } = Array.Empty<PropertyHoldingInfo>();
    public IReadOnlyList<RentalHoldingInfo> Rentals { get; init; } = Array.Empty<RentalHoldingInfo>();
    public int TotalHoldings => OwnedProperties.Count + Rentals.Count;
}

/// <summary>
/// Aggregate holdings across all of a player's characters.
/// </summary>
public record PlayerAggregateHoldingsInfo
{
    public required string CdKey { get; init; }
    public IReadOnlyList<CharacterHoldingsSummary> CharacterHoldings { get; init; } = Array.Empty<CharacterHoldingsSummary>();
    public int TotalOwnedProperties { get; init; }
    public int TotalRentals { get; init; }
    public int TotalHoldings => TotalOwnedProperties + TotalRentals;
}

/// <summary>
/// Summary of holdings for a single character.
/// </summary>
public record CharacterHoldingsSummary
{
    public required CharacterId CharacterId { get; init; }
    public required string CharacterName { get; init; }
    public int OwnedPropertyCount { get; init; }
    public int RentalCount { get; init; }
}

/// <summary>
/// Property holding information (placeholder for future implementation).
/// </summary>
public record PropertyHoldingInfo
{
    public required string PropertyId { get; init; }
    public required string PropertyName { get; init; }
    public required string Location { get; init; }
    public DateTime? AcquiredUtc { get; init; }
}

/// <summary>
/// Rental holding information (placeholder for future implementation).
/// </summary>
public record RentalHoldingInfo
{
    public required string RentalId { get; init; }
    public required string RentalName { get; init; }
    public required string Location { get; init; }
    public DateTime? RentalStartUtc { get; init; }
    public DateTime? RentalEndUtc { get; init; }
}

