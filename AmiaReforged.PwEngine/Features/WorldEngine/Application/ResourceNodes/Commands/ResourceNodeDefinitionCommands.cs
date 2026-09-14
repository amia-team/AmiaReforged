using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.ResourceNodes.Commands;

/// <summary>
/// Creates a new resource node definition.
/// </summary>
public record CreateResourceNodeCommand : ICommand
{
    public required ResourceNodeDefinition Definition { get; init; }
}

/// <summary>
/// Updates an existing resource node definition. Fails if the tag is unknown.
/// </summary>
public record UpdateResourceNodeCommand : ICommand
{
    public required string Tag { get; init; }
    public required ResourceNodeDefinition Definition { get; init; }
}

/// <summary>
/// Creates or replaces a resource node definition (bulk-import semantics).
/// </summary>
public record UpsertResourceNodeCommand : ICommand
{
    public required ResourceNodeDefinition Definition { get; init; }
}

/// <summary>
/// Deletes a resource node definition. Fails if the tag is unknown.
/// </summary>
public record DeleteResourceNodeCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateResourceNodeCommand>))]
public sealed class CreateResourceNodeHandler : ICommandHandler<CreateResourceNodeCommand>
{
    private readonly IResourceNodeDefinitionRepository _repository;

    public CreateResourceNodeHandler(IResourceNodeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(CreateResourceNodeCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.Exists(command.Definition.Tag))
            return Task.FromResult(CommandResult.Fail(
                $"Resource node with tag '{command.Definition.Tag}' already exists"));

        _repository.Create(command.Definition);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Definition.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateResourceNodeCommand>))]
public sealed class UpdateResourceNodeHandler : ICommandHandler<UpdateResourceNodeCommand>
{
    private readonly IResourceNodeDefinitionRepository _repository;

    public UpdateResourceNodeHandler(IResourceNodeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateResourceNodeCommand command, CancellationToken cancellationToken = default)
    {
        if (!_repository.Exists(command.Tag))
            return Task.FromResult(CommandResult.Fail($"No resource node with tag '{command.Tag}'"));

        _repository.Update(command.Definition);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<UpsertResourceNodeCommand>))]
public sealed class UpsertResourceNodeHandler : ICommandHandler<UpsertResourceNodeCommand>
{
    private readonly IResourceNodeDefinitionRepository _repository;

    public UpsertResourceNodeHandler(IResourceNodeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpsertResourceNodeCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.Exists(command.Definition.Tag))
            _repository.Update(command.Definition);
        else
            _repository.Create(command.Definition);

        return Task.FromResult(CommandResult.OkWith("Tag", command.Definition.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteResourceNodeCommand>))]
public sealed class DeleteResourceNodeHandler : ICommandHandler<DeleteResourceNodeCommand>
{
    private readonly IResourceNodeDefinitionRepository _repository;

    public DeleteResourceNodeHandler(IResourceNodeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteResourceNodeCommand command, CancellationToken cancellationToken = default)
    {
        bool deleted = _repository.Delete(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No resource node with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
