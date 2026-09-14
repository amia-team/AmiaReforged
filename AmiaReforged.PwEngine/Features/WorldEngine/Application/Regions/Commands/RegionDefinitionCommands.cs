using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Commands;

/// <summary>
/// Creates or replaces a region definition (matches the admin panel's upsert semantics).
/// </summary>
public record UpsertRegionCommand : ICommand
{
    public required RegionDefinition Definition { get; init; }
}

/// <summary>
/// Updates an existing region definition. Fails if the tag is unknown.
/// </summary>
public record UpdateRegionCommand : ICommand
{
    public required string Tag { get; init; }
    public required RegionDefinition Definition { get; init; }
}

/// <summary>
/// Deletes a region definition. Fails if the tag is unknown.
/// </summary>
public record DeleteRegionCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<UpsertRegionCommand>))]
public sealed class UpsertRegionHandler : ICommandHandler<UpsertRegionCommand>
{
    private readonly IRegionRepository _repository;

    public UpsertRegionHandler(IRegionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpsertRegionCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.Exists(command.Definition.Tag))
            _repository.Update(command.Definition);
        else
            _repository.Add(command.Definition);

        return Task.FromResult(CommandResult.OkWith("Tag", command.Definition.Tag.Value));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateRegionCommand>))]
public sealed class UpdateRegionHandler : ICommandHandler<UpdateRegionCommand>
{
    private readonly IRegionRepository _repository;

    public UpdateRegionHandler(IRegionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateRegionCommand command, CancellationToken cancellationToken = default)
    {
        if (!_repository.Exists(new RegionTag(command.Tag)))
            return Task.FromResult(CommandResult.Fail($"No region with tag '{command.Tag}'"));

        _repository.Update(command.Definition);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteRegionCommand>))]
public sealed class DeleteRegionHandler : ICommandHandler<DeleteRegionCommand>
{
    private readonly IRegionRepository _repository;

    public DeleteRegionHandler(IRegionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteRegionCommand command, CancellationToken cancellationToken = default)
    {
        bool deleted = _repository.Delete(new RegionTag(command.Tag));
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No region with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
