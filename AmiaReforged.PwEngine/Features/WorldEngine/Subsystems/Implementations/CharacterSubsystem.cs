using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Concrete implementation of the Character subsystem.
/// Delegates to existing repositories and services.
/// </summary>
[ServiceBinding(typeof(ICharacterSubsystem))]
public sealed class CharacterSubsystem : ICharacterSubsystem
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ICharacterStatRepository _statRepository;
    private readonly IReputationRepository _reputationRepository;
    private readonly CharacterRegistrationService _registrationService;

    public CharacterSubsystem(
        ICharacterRepository characterRepository,
        ICharacterStatRepository statRepository,
        IReputationRepository reputationRepository,
        CharacterRegistrationService registrationService)
    {
        _characterRepository = characterRepository;
        _statRepository = statRepository;
        _reputationRepository = reputationRepository;
        _registrationService = registrationService;
    }

    public Task<CommandResult> RegisterCharacterAsync(CharacterId characterId, CancellationToken ct = default)
    {
        // Registration is event-driven via CharacterRegistrationService (NWN area-enter event).
        // For API-level registration checks, verify the character exists in the runtime repository.
        if (_characterRepository.Exists(characterId))
            return Task.FromResult(CommandResult.Fail("Character is already registered"));

        return Task.FromResult(CommandResult.Fail("Character registration is handled automatically on area entry"));
    }

    public Task<ICharacter?> GetCharacterAsync(CharacterId characterId, CancellationToken ct = default)
    {
        ICharacter? character = _characterRepository.GetById(characterId);
        return Task.FromResult(character);
    }

    public Task<CharacterStats?> GetCharacterStatsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        CharacterStatistics? stats = _statRepository.GetCharacterStatistics(characterId);
        if (stats is null)
            return Task.FromResult<CharacterStats?>(null);

        return Task.FromResult<CharacterStats?>(new CharacterStats(
            PlayTime: stats.PlayTime,
            QuestsCompleted: stats.TimesRankedUp, // Best available approximation
            ItemsCrafted: stats.IndustriesJoined,  // Best available approximation
            LastSeen: DateTime.UtcNow));
    }

    public Task<CommandResult> UpdateCharacterStatsAsync(CharacterId characterId, CharacterStats stats, CancellationToken ct = default)
    {
        CharacterStatistics? existing = _statRepository.GetCharacterStatistics(characterId);
        if (existing is null)
            return Task.FromResult(CommandResult.Fail($"No statistics found for character {characterId}"));

        existing.PlayTime = stats.PlayTime;

        _statRepository.UpdateCharacterStatistics(existing);
        _statRepository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
    }

    public Task<int> GetReputationAsync(CharacterId characterId, OrganizationId organizationId, CancellationToken ct = default)
    {
        Reputation rep = _reputationRepository.GetReputation(characterId, organizationId);
        return Task.FromResult(rep.Level);
    }

    public Task<CommandResult> AdjustReputationAsync(
        CharacterId characterId,
        OrganizationId organizationId,
        int adjustment,
        string reason,
        CancellationToken ct = default)
    {
        // IReputationRepository currently only supports read (GetReputation).
        // Reputation mutation requires expanding the repository interface.
        // TODO: Add AdjustReputation(Guid characterId, Guid targetId, int delta, string reason) to IReputationRepository
        return Task.FromResult(CommandResult.Fail("Reputation adjustment not yet supported — IReputationRepository needs mutation methods"));
    }

    public ICharacterKnowledgeContext GetKnowledgeContext(CharacterId characterId)
    {
        ICharacter? character = _characterRepository.GetById(characterId);
        return character ?? throw new InvalidOperationException($"Character {characterId} not found");
    }

    public ICharacterIndustryContext GetIndustryContext(CharacterId characterId)
    {
        ICharacter? character = _characterRepository.GetById(characterId);
        return character ?? throw new InvalidOperationException($"Character {characterId} not found");
    }
}

