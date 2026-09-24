using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;

/// <summary>
/// Resolves a character from the runtime character repository.
/// </summary>
/// <remarks>
/// The runtime repository is an in-memory dictionary, so the handler completes
/// synchronously without yielding to the thread pool. This keeps the query safe
/// to block on from synchronous NWN-thread callers (see the compatibility note in
/// <see cref="CharacterSubsystem"/>).
/// </remarks>
[ServiceBinding(typeof(IQueryHandler<GetCharacterQuery, ICharacter?>))]
[ServiceBinding(typeof(IQueryHandlerMarker))]
public sealed class GetCharacterQueryHandler : IQueryHandler<GetCharacterQuery, ICharacter?>
{
    private readonly ICharacterRepository _repository;

    public GetCharacterQueryHandler(ICharacterRepository repository)
    {
        _repository = repository;
    }

    public Task<ICharacter?> HandleAsync(GetCharacterQuery query, CancellationToken cancellationToken = default)
    {
        ICharacter? character = _repository.GetById(query.CharacterId);
        return Task.FromResult(character);
    }
}
