using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Creates a new workstation definition. Fails if the tag already exists.
/// </summary>
public record CreateWorkstationCommand : ICommand
{
    public required Workstation Workstation { get; init; }
}

/// <summary>
/// Updates an existing workstation definition. Fails if the tag is unknown.
/// </summary>
public record UpdateWorkstationCommand : ICommand
{
    public required string Tag { get; init; }
    public required Workstation Workstation { get; init; }
}

/// <summary>
/// Creates or replaces a workstation definition (bulk-import semantics).
/// </summary>
public record UpsertWorkstationCommand : ICommand
{
    public required Workstation Workstation { get; init; }
}

/// <summary>
/// Deletes a workstation definition. Fails if the tag is unknown.
/// </summary>
public record DeleteWorkstationCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateWorkstationCommand>))]
public sealed class CreateWorkstationHandler : ICommandHandler<CreateWorkstationCommand>
{
    private readonly IWorkstationRepository _repository;

    public CreateWorkstationHandler(IWorkstationRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(CreateWorkstationCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.WorkstationExists(command.Workstation.Tag.Value))
            return Task.FromResult(CommandResult.Fail(
                $"Workstation with tag '{command.Workstation.Tag.Value}' already exists"));

        _repository.Add(command.Workstation);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Workstation.Tag.Value));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateWorkstationCommand>))]
public sealed class UpdateWorkstationHandler : ICommandHandler<UpdateWorkstationCommand>
{
    private readonly IWorkstationRepository _repository;

    public UpdateWorkstationHandler(IWorkstationRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateWorkstationCommand command, CancellationToken cancellationToken = default)
    {
        Workstation? existing = _repository.GetByTag(new WorkstationTag(command.Tag));
        if (existing is null)
            return Task.FromResult(CommandResult.Fail($"No workstation with tag '{command.Tag}'"));

        _repository.Update(command.Workstation);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<UpsertWorkstationCommand>))]
public sealed class UpsertWorkstationHandler : ICommandHandler<UpsertWorkstationCommand>
{
    private readonly IWorkstationRepository _repository;

    public UpsertWorkstationHandler(IWorkstationRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpsertWorkstationCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.WorkstationExists(command.Workstation.Tag.Value))
            _repository.Update(command.Workstation);
        else
            _repository.Add(command.Workstation);

        return Task.FromResult(CommandResult.OkWith("Tag", command.Workstation.Tag.Value));
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteWorkstationCommand>))]
public sealed class DeleteWorkstationHandler : ICommandHandler<DeleteWorkstationCommand>
{
    private readonly IWorkstationRepository _repository;

    public DeleteWorkstationHandler(IWorkstationRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteWorkstationCommand command, CancellationToken cancellationToken = default)
    {
        bool deleted = _repository.Delete(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No workstation with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
