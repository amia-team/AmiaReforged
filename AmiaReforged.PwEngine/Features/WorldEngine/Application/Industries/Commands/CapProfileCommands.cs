using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Creates a new knowledge cap profile. Fails if the tag already exists.
/// </summary>
public record CreateCapProfileCommand : ICommand
{
    public required KnowledgeCapProfile Profile { get; init; }
}

/// <summary>
/// Updates an existing knowledge cap profile. Fails if the tag is unknown.
/// </summary>
public record UpdateCapProfileCommand : ICommand
{
    public required string Tag { get; init; }
    public required KnowledgeCapProfile Profile { get; init; }
}

/// <summary>
/// Deletes a knowledge cap profile. Fails when unknown or still assigned to characters.
/// </summary>
public record DeleteCapProfileCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateCapProfileCommand>))]
public sealed class CreateCapProfileHandler : ICommandHandler<CreateCapProfileCommand>
{
    private readonly IKnowledgeCapProfileRepository _repository;

    public CreateCapProfileHandler(IKnowledgeCapProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(CreateCapProfileCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.GetByTag(command.Profile.Tag) != null)
            return Task.FromResult(CommandResult.Fail($"Cap profile '{command.Profile.Tag}' already exists"));

        _repository.Add(command.Profile);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Profile.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateCapProfileCommand>))]
public sealed class UpdateCapProfileHandler : ICommandHandler<UpdateCapProfileCommand>
{
    private readonly IKnowledgeCapProfileRepository _repository;

    public UpdateCapProfileHandler(IKnowledgeCapProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateCapProfileCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.GetByTag(command.Tag) == null)
            return Task.FromResult(CommandResult.Fail($"Cap profile '{command.Tag}' not found"));

        _repository.Update(command.Profile);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteCapProfileCommand>))]
public sealed class DeleteCapProfileHandler : ICommandHandler<DeleteCapProfileCommand>
{
    private readonly IKnowledgeCapProfileRepository _repository;

    public DeleteCapProfileHandler(IKnowledgeCapProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteCapProfileCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.IsInUse(command.Tag))
            return Task.FromResult(CommandResult.Fail(
                $"Cap profile '{command.Tag}' is assigned to one or more characters and cannot be deleted"));

        bool deleted = _repository.Delete(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"Cap profile '{command.Tag}' not found"));

        return Task.FromResult(CommandResult.Ok());
    }
}
