using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;

/// <summary>
/// Projects the stored <see cref="CharacterStatistics"/> onto the truthful
/// <see cref="CharacterStats"/> contract.
/// </summary>
/// <remarks>
/// This is a read-only handler: it performs no writes and returns
/// <c>null</c> when the character has no statistics record. Only
/// <see cref="CharacterStatistics.PlayTime"/> is mapped; the stored rank-up,
/// industry, death, and knowledge counters have no truthful projection and are
/// intentionally omitted.
/// </remarks>
[ServiceBinding(typeof(IQueryHandler<GetCharacterStatsQuery, CharacterStats?>))]
[ServiceBinding(typeof(IQueryHandlerMarker))]
public sealed class GetCharacterStatsQueryHandler : IQueryHandler<GetCharacterStatsQuery, CharacterStats?>
{
    private readonly ICharacterStatRepository _repository;

    public GetCharacterStatsQueryHandler(ICharacterStatRepository repository)
    {
        _repository = repository;
    }

    public Task<CharacterStats?> HandleAsync(GetCharacterStatsQuery query, CancellationToken cancellationToken = default)
    {
        CharacterStatistics? stats = _repository.GetCharacterStatistics(query.CharacterId);

        if (stats is null)
            return Task.FromResult<CharacterStats?>(null);

        return Task.FromResult<CharacterStats?>(new CharacterStats(stats.PlayTime));
    }
}
