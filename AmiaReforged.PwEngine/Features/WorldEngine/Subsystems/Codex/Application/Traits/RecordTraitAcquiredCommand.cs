using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Traits;

/// <summary>
/// Records a newly acquired trait in a character's codex. Trait metadata is
/// snapshotted from the trait subsystem at acquisition time for offline display.
/// Fails if the trait is already recorded. (F-6 audit: CQRS envelope over
/// <see cref="PlayerCodex.RecordTraitAcquired"/>; in-development feature,
/// no game callers yet.)
/// </summary>
public record RecordTraitAcquiredCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required string TraitTag { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required TraitCategory Category { get; init; }
    public required string AcquisitionMethod { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<RecordTraitAcquiredCommand>))]
public sealed class RecordTraitAcquiredHandler : ICommandHandler<RecordTraitAcquiredCommand>
{
    private readonly IPlayerCodexRepository _codexRepository;
    private readonly IEventBus _eventBus;

    public RecordTraitAcquiredHandler(IPlayerCodexRepository codexRepository, IEventBus eventBus)
    {
        _codexRepository = codexRepository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(RecordTraitAcquiredCommand command, CancellationToken cancellationToken = default)
    {
        TraitTag traitTag;
        try
        {
            traitTag = new TraitTag(command.TraitTag);
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        DateTime now = DateTime.UtcNow;
        PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
        codex ??= new PlayerCodex(command.CharacterId, now);

        CodexTraitEntry entry = new()
        {
            TraitTag = traitTag,
            Name = command.Name,
            Description = command.Description,
            Category = command.Category,
            AcquisitionMethod = command.AcquisitionMethod,
            DateAcquired = now
        };

        try
        {
            codex.RecordTraitAcquired(entry, now);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return CommandResult.Fail(ex.Message);
        }

        await _codexRepository.SaveAsync(codex, cancellationToken);

        await _eventBus.PublishAsync(
            new TraitAcquiredEvent(command.CharacterId, now, traitTag, command.AcquisitionMethod),
            cancellationToken);

        return CommandResult.Ok();
    }
}
