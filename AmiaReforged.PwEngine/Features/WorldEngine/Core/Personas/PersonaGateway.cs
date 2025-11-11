using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Core.Personas;

/// <summary>
/// Implementation of the Persona Gateway.
/// Provides simplified access to persona identity data and character-player mappings.
/// </summary>
[ServiceBinding(typeof(IPersonaGateway))]
public sealed class PersonaGateway : IPersonaGateway
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IPersistentCharacterRepository _characterRepository;
    private readonly IPersistentPlayerPersonaRepository _playerRepository;

    public PersonaGateway(
        IPersonaRepository personaRepository,
        IPersistentCharacterRepository characterRepository,
        IPersistentPlayerPersonaRepository playerRepository)
    {
        _personaRepository = personaRepository;
        _characterRepository = characterRepository;
        _playerRepository = playerRepository;
    }

    // === Basic Persona Lookup ===

    public Task<PersonaInfo?> GetPersonaAsync(PersonaId personaId, CancellationToken ct = default)
    {
        if (!_personaRepository.TryGetPersona(personaId, out Persona? persona) || persona == null)
        {
            return Task.FromResult<PersonaInfo?>(null);
        }

        PersonaInfo info = MapToPersonaInfo(persona);
        return Task.FromResult<PersonaInfo?>(info);
    }

    public Task<IReadOnlyDictionary<PersonaId, PersonaInfo>> GetPersonasAsync(
        IEnumerable<PersonaId> personaIds, CancellationToken ct = default)
    {
        Dictionary<PersonaId, Persona> personas = _personaRepository.GetPersonas(personaIds);
        Dictionary<PersonaId, PersonaInfo> result = personas
            .ToDictionary(kvp => kvp.Key, kvp => MapToPersonaInfo(kvp.Value));

        return Task.FromResult<IReadOnlyDictionary<PersonaId, PersonaInfo>>(result);
    }

    public Task<bool> ExistsAsync(PersonaId personaId, CancellationToken ct = default)
    {
        bool exists = _personaRepository.Exists(personaId);
        return Task.FromResult(exists);
    }

    // === Player-Character Mappings ===

    public Task<IReadOnlyList<CharacterPersonaInfo>> GetPlayerCharactersAsync(
        string cdKey, CancellationToken ct = default)
    {
        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);
        List<PersistedCharacter> characters = _characterRepository.GetCharactersByCdKey(normalizedCdKey);

        List<CharacterPersonaInfo> result = characters
            .Select(c => MapToCharacterPersonaInfo(c))
            .ToList();

        return Task.FromResult<IReadOnlyList<CharacterPersonaInfo>>(result);
    }

    public Task<PlayerPersonaInfo?> GetCharacterOwnerAsync(CharacterId characterId, CancellationToken ct = default)
    {
        PersistedCharacter? character = _characterRepository.GetByGuid(characterId.Value);
        if (character == null)
        {
            return Task.FromResult<PlayerPersonaInfo?>(null);
        }

        return GetPlayerAsync(character.CdKey, ct);
    }

    public Task<PlayerPersonaInfo?> GetCharacterOwnerAsync(PersonaId characterPersonaId, CancellationToken ct = default)
    {
        if (characterPersonaId.Type != PersonaType.Character)
        {
            return Task.FromResult<PlayerPersonaInfo?>(null);
        }

        PersistedCharacter? character = _characterRepository.GetByPersonaId(characterPersonaId);
        if (character == null)
        {
            return Task.FromResult<PlayerPersonaInfo?>(null);
        }

        return GetPlayerAsync(character.CdKey, ct);
    }

    public Task<PersonaId?> GetCharacterPersonaIdAsync(CharacterId characterId, CancellationToken ct = default)
    {
        PersistedCharacter? character = _characterRepository.GetByGuid(characterId.Value);
        if (character == null || string.IsNullOrEmpty(character.PersonaIdString))
        {
            return Task.FromResult<PersonaId?>(null);
        }

        try
        {
            PersonaId personaId = PersonaId.Parse(character.PersonaIdString);
            return Task.FromResult<PersonaId?>(personaId);
        }
        catch
        {
            return Task.FromResult<PersonaId?>(null);
        }
    }

    public Task<CharacterId?> GetPersonaCharacterIdAsync(PersonaId personaId, CancellationToken ct = default)
    {
        if (personaId.Type != PersonaType.Character)
        {
            return Task.FromResult<CharacterId?>(null);
        }

        PersistedCharacter? character = _characterRepository.GetByPersonaId(personaId);
        if (character == null)
        {
            return Task.FromResult<CharacterId?>(null);
        }

        return Task.FromResult<CharacterId?>(character.CharacterId);
    }

    // === Character Identity ===

    public Task<CharacterIdentityInfo?> GetCharacterIdentityAsync(
        CharacterId characterId, CancellationToken ct = default)
    {
        PersistedCharacter? character = _characterRepository.GetByGuid(characterId.Value);
        if (character == null)
        {
            return Task.FromResult<CharacterIdentityInfo?>(null);
        }

        CharacterIdentityInfo info = MapToCharacterIdentityInfo(character);
        return Task.FromResult<CharacterIdentityInfo?>(info);
    }

    public Task<CharacterIdentityInfo?> GetCharacterIdentityByPersonaAsync(
        PersonaId personaId, CancellationToken ct = default)
    {
        if (personaId.Type != PersonaType.Character)
        {
            return Task.FromResult<CharacterIdentityInfo?>(null);
        }

        PersistedCharacter? character = _characterRepository.GetByPersonaId(personaId);
        if (character == null)
        {
            return Task.FromResult<CharacterIdentityInfo?>(null);
        }

        CharacterIdentityInfo info = MapToCharacterIdentityInfo(character);
        return Task.FromResult<CharacterIdentityInfo?>(info);
    }

    // === Player Identity ===

    public Task<PlayerPersonaInfo?> GetPlayerAsync(string cdKey, CancellationToken ct = default)
    {
        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);
        PlayerPersonaRecord? record = _playerRepository.GetByCdKey(normalizedCdKey);

        if (record == null)
        {
            return Task.FromResult<PlayerPersonaInfo?>(null);
        }

        // Count characters for this player
        List<PersistedCharacter> characters = _characterRepository.GetCharactersByCdKey(normalizedCdKey);

        PlayerPersonaInfo info = new()
        {
            Id = PersonaId.FromPlayerCdKey(normalizedCdKey),
            Type = PersonaType.Player,
            DisplayName = record.DisplayName,
            CdKey = normalizedCdKey,
            CreatedUtc = record.CreatedUtc,
            UpdatedUtc = record.UpdatedUtc,
            LastSeenUtc = record.LastSeenUtc,
            CharacterCount = characters.Count
        };

        return Task.FromResult<PlayerPersonaInfo?>(info);
    }

    public Task<PlayerPersonaInfo?> GetPlayerByPersonaAsync(PersonaId personaId, CancellationToken ct = default)
    {
        if (personaId.Type != PersonaType.Player)
        {
            return Task.FromResult<PlayerPersonaInfo?>(null);
        }

        // Extract CD key from persona ID
        string cdKey = personaId.Value;
        return GetPlayerAsync(cdKey, ct);
    }

    // === Holdings (Placeholder Implementation) ===

    public Task<PersonaHoldingsInfo> GetPersonaHoldingsAsync(
        PersonaId personaId, CancellationToken ct = default)
    {
        // TODO: Implement when property/rental system is in place
        PersonaHoldingsInfo holdings = new()
        {
            PersonaId = personaId,
            OwnedProperties = Array.Empty<PropertyHoldingInfo>(),
            Rentals = Array.Empty<RentalHoldingInfo>()
        };

        return Task.FromResult(holdings);
    }

    public Task<PlayerAggregateHoldingsInfo> GetPlayerAggregateHoldingsAsync(
        string cdKey, CancellationToken ct = default)
    {
        // TODO: Implement when property/rental system is in place
        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);

        PlayerAggregateHoldingsInfo holdings = new()
        {
            CdKey = normalizedCdKey,
            CharacterHoldings = Array.Empty<CharacterHoldingsSummary>(),
            TotalOwnedProperties = 0,
            TotalRentals = 0
        };

        return Task.FromResult(holdings);
    }

    // === Private Mapping Helpers ===

    private PersonaInfo MapToPersonaInfo(Persona persona)
    {
        return persona switch
        {
            CharacterPersona cp => MapToCharacterPersonaInfo(cp),
            PlayerPersona pp => MapToPlayerPersonaInfo(pp),
            _ => new PersonaInfo
            {
                Id = persona.Id,
                Type = persona.Type,
                DisplayName = persona.DisplayName
            }
        };
    }

    private CharacterPersonaInfo MapToCharacterPersonaInfo(CharacterPersona persona)
    {
        // Try to get CD key from database
        PersistedCharacter? character = _characterRepository.GetByGuid(persona.CharacterId.Value);
        string cdKey = character?.CdKey ?? string.Empty;

        return new CharacterPersonaInfo
        {
            Id = persona.Id,
            Type = persona.Type,
            DisplayName = persona.DisplayName,
            CharacterId = persona.CharacterId,
            CdKey = cdKey
        };
    }

    private CharacterPersonaInfo MapToCharacterPersonaInfo(PersistedCharacter character)
    {
        PersonaId personaId;
        if (!string.IsNullOrEmpty(character.PersonaIdString))
        {
            try
            {
                personaId = PersonaId.Parse(character.PersonaIdString);
            }
            catch
            {
                personaId = PersonaId.FromCharacter(character.CharacterId);
            }
        }
        else
        {
            personaId = PersonaId.FromCharacter(character.CharacterId);
        }

        string displayName = string.IsNullOrEmpty(character.LastName)
            ? character.FirstName
            : $"{character.FirstName} {character.LastName}";

        return new CharacterPersonaInfo
        {
            Id = personaId,
            Type = PersonaType.Character,
            DisplayName = displayName,
            CharacterId = character.CharacterId,
            CdKey = character.CdKey
        };
    }

    private PlayerPersonaInfo MapToPlayerPersonaInfo(PlayerPersona persona)
    {
        List<PersistedCharacter> characters = _characterRepository.GetCharactersByCdKey(persona.CdKey);

        return new PlayerPersonaInfo
        {
            Id = persona.Id,
            Type = persona.Type,
            DisplayName = persona.DisplayName,
            CdKey = persona.CdKey,
            CreatedUtc = persona.CreatedUtc,
            UpdatedUtc = persona.UpdatedUtc,
            LastSeenUtc = persona.LastSeenUtc,
            CharacterCount = characters.Count
        };
    }

    private CharacterIdentityInfo MapToCharacterIdentityInfo(PersistedCharacter character)
    {
        PersonaId personaId;
        if (!string.IsNullOrEmpty(character.PersonaIdString))
        {
            try
            {
                personaId = PersonaId.Parse(character.PersonaIdString);
            }
            catch
            {
                personaId = PersonaId.FromCharacter(character.CharacterId);
            }
        }
        else
        {
            personaId = PersonaId.FromCharacter(character.CharacterId);
        }

        string fullName = string.IsNullOrEmpty(character.LastName)
            ? character.FirstName
            : $"{character.FirstName} {character.LastName}";

        return new CharacterIdentityInfo
        {
            CharacterId = character.CharacterId,
            PersonaId = personaId,
            FirstName = character.FirstName,
            LastName = character.LastName,
            FullName = fullName,
            Description = string.Empty, // TODO: Add description field to database
            CdKey = character.CdKey,
            CreatedUtc = null, // TODO: Add created timestamp to database
            LastSeenUtc = null // TODO: Add last seen timestamp to database
        };
    }
}

