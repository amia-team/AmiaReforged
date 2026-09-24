using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Concrete implementation of the Character subsystem.
/// Delegates to existing repositories and services.
/// </summary>
[ServiceBinding(typeof(ICharacterSubsystem))]
public sealed class CharacterSubsystem : ICharacterSubsystem
{
    /// <summary>
    /// Safe synchronous compatibility boundary. Every character lookup here is
    /// routed through <see cref="GetCharacterQuery"/>, whose handler reads an
    /// in-memory dictionary and completes without suspending on I/O or the thread
    /// pool. Awaiting such a completed task therefore runs inline, so blocking on
    /// it from a synchronous NWN-thread caller does not suspend the thread on any
    /// continuation. The synchronous <c>Get*Context</c> methods are retained only
    /// because their contract returns a context object that callers use
    /// synchronously; all of their reads go through the query dispatcher and none
    /// touch the repository directly.
    /// </summary>
    private readonly ICharacterRepository _characterRepository;
    private readonly ICharacterStatRepository _statRepository;
    private readonly IReputationRepository _reputationRepository;
    private readonly IQueryDispatcher _queries;

    public CharacterSubsystem(
        ICharacterRepository characterRepository,
        ICharacterStatRepository statRepository,
        IReputationRepository reputationRepository,
        IQueryDispatcher queries)
    {
        _characterRepository = characterRepository;
        _statRepository = statRepository;
        _reputationRepository = reputationRepository;
        _queries = queries;
    }

    public Task<ICharacter?> GetCharacterAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetCharacterQuery, ICharacter?>(new GetCharacterQuery(characterId), ct);
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
        ICharacter? character = _queries.DispatchAsync<GetCharacterQuery, ICharacter?>(
            new GetCharacterQuery(characterId)).GetAwaiter().GetResult();
        return character ?? throw new InvalidOperationException($"Character {characterId} not found");
    }

    public ICharacterIndustryContext GetIndustryContext(CharacterId characterId)
    {
        ICharacter? character = _queries.DispatchAsync<GetCharacterQuery, ICharacter?>(
            new GetCharacterQuery(characterId)).GetAwaiter().GetResult();
        return character ?? throw new InvalidOperationException($"Character {characterId} not found");
    }
}

