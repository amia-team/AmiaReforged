using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Commands;
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
    private readonly IQueryDispatcher _queries;
    private readonly ICommandDispatcher _commands;

    public CharacterSubsystem(
        ICharacterRepository characterRepository,
        IQueryDispatcher queries,
        ICommandDispatcher commands)
    {
        _characterRepository = characterRepository;
        _queries = queries;
        _commands = commands;
    }

    public Task<ICharacter?> GetCharacterAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetCharacterQuery, ICharacter?>(new GetCharacterQuery(characterId), ct);
    }

    public Task<CharacterStats?> GetCharacterStatsAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetCharacterStatsQuery, CharacterStats?>(new GetCharacterStatsQuery(characterId), ct);
    }

    public Task<CommandResult> UpdateCharacterStatsAsync(CharacterId characterId, CharacterStats stats, CancellationToken ct = default)
    {
        return _commands.DispatchAsync<UpdateCharacterStatsCommand>(
            new UpdateCharacterStatsCommand(characterId, stats.PlayTime), ct);
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

