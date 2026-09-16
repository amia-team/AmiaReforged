using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Reputation;

/// <summary>
/// Records a faction reputation change for a character. Creates the faction
/// entry on first interaction. (F-6 audit: CQRS envelope over
/// <see cref="PlayerCodex.RecordReputationChange"/>; in-development feature,
/// no game callers yet.)
/// </summary>
public record AdjustReputationCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required string FactionId { get; init; }
    public required string FactionName { get; init; }
    public required int Delta { get; init; }
    public required string Reason { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<AdjustReputationCommand>))]
public sealed class AdjustReputationHandler : ICommandHandler<AdjustReputationCommand>
{
    private readonly IPlayerCodexRepository _codexRepository;
    private readonly IEventBus _eventBus;

    public AdjustReputationHandler(IPlayerCodexRepository codexRepository, IEventBus eventBus)
    {
        _codexRepository = codexRepository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(AdjustReputationCommand command, CancellationToken cancellationToken = default)
    {
        FactionId factionId;
        try
        {
            factionId = new FactionId(command.FactionId);
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        DateTime now = DateTime.UtcNow;
        PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
        codex ??= new PlayerCodex(command.CharacterId, now);

        try
        {
            codex.RecordReputationChange(factionId, command.FactionName, command.Delta, command.Reason, now);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return CommandResult.Fail(ex.Message);
        }

        await _codexRepository.SaveAsync(codex, cancellationToken);

        await _eventBus.PublishAsync(
            new ReputationChangedEvent(command.CharacterId, now, factionId, command.Delta, command.Reason),
            cancellationToken);

        return CommandResult.Ok();
    }
}
