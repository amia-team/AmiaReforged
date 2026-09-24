using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Commands;

/// <summary>
/// Persists the supported play-time update.
/// </summary>
/// <remarks>
/// Fails when the character has no statistics record. Only
/// <see cref="CharacterStatistics.PlayTime"/> is written; the stored rank-up,
/// industry, death, and knowledge counters are left untouched because they have
/// no supported projection. See task 022.
/// </remarks>
[ServiceBinding(typeof(ICommandHandler<UpdateCharacterStatsCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class UpdateCharacterStatsCommandHandler : ICommandHandler<UpdateCharacterStatsCommand>
{
    private readonly ICharacterStatRepository _repository;

    public UpdateCharacterStatsCommandHandler(ICharacterStatRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateCharacterStatsCommand command, CancellationToken cancellationToken = default)
    {
        CharacterStatistics? existing = _repository.GetCharacterStatistics(command.CharacterId);

        if (existing is null)
            return Task.FromResult(CommandResult.Fail($"No statistics found for character {command.CharacterId}"));

        existing.PlayTime = command.PlayTime;
        _repository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
    }
}
