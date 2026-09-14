using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;

/// <summary>
/// Creates a new interaction definition. Fails if the tag already exists.
/// </summary>
public record CreateInteractionDefinitionCommand : ICommand
{
    public required InteractionDefinition Definition { get; init; }
}

/// <summary>
/// Updates an existing interaction definition. Fails if the tag is unknown.
/// </summary>
public record UpdateInteractionDefinitionCommand : ICommand
{
    public required string Tag { get; init; }
    public required InteractionDefinition Definition { get; init; }
}

/// <summary>
/// Creates or replaces an interaction definition (bulk-import semantics).
/// </summary>
public record UpsertInteractionDefinitionCommand : ICommand
{
    public required InteractionDefinition Definition { get; init; }
}

/// <summary>
/// Deletes an interaction definition. Fails if the tag is unknown.
/// </summary>
public record DeleteInteractionDefinitionCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateInteractionDefinitionCommand>))]
public sealed class CreateInteractionDefinitionHandler : ICommandHandler<CreateInteractionDefinitionCommand>
{
    private readonly IInteractionDefinitionRepository _repository;

    public CreateInteractionDefinitionHandler(IInteractionDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(CreateInteractionDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.Exists(command.Definition.Tag))
            return Task.FromResult(CommandResult.Fail(
                $"An interaction definition with tag '{command.Definition.Tag}' already exists"));

        _repository.Create(command.Definition);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Definition.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateInteractionDefinitionCommand>))]
public sealed class UpdateInteractionDefinitionHandler : ICommandHandler<UpdateInteractionDefinitionCommand>
{
    private readonly IInteractionDefinitionRepository _repository;

    public UpdateInteractionDefinitionHandler(IInteractionDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateInteractionDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        if (!_repository.Exists(command.Tag))
            return Task.FromResult(CommandResult.Fail($"No interaction definition with tag '{command.Tag}'"));

        _repository.Update(command.Definition);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<UpsertInteractionDefinitionCommand>))]
public sealed class UpsertInteractionDefinitionHandler : ICommandHandler<UpsertInteractionDefinitionCommand>
{
    private readonly IInteractionDefinitionRepository _repository;

    public UpsertInteractionDefinitionHandler(IInteractionDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpsertInteractionDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        // Repository Create is itself an upsert (replaces on matching tag).
        _repository.Create(command.Definition);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Definition.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteInteractionDefinitionCommand>))]
public sealed class DeleteInteractionDefinitionHandler : ICommandHandler<DeleteInteractionDefinitionCommand>
{
    private readonly IInteractionDefinitionRepository _repository;

    public DeleteInteractionDefinitionHandler(IInteractionDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteInteractionDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        bool deleted = _repository.Delete(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No interaction definition with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
