using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Creates a new industry definition. Fails if the tag already exists.
/// </summary>
public record CreateIndustryCommand : ICommand
{
    public required Industry Industry { get; init; }
}

/// <summary>
/// Updates an existing industry definition. Fails if the tag is unknown.
/// </summary>
public record UpdateIndustryCommand : ICommand
{
    public required string Tag { get; init; }
    public required Industry Industry { get; init; }
}

/// <summary>
/// Creates or replaces an industry definition (bulk-import semantics).
/// </summary>
public record UpsertIndustryCommand : ICommand
{
    public required Industry Industry { get; init; }
}

/// <summary>
/// Deletes an industry definition. Fails if the tag is unknown.
/// </summary>
public record DeleteIndustryCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateIndustryCommand>))]
public sealed class CreateIndustryHandler : ICommandHandler<CreateIndustryCommand>
{
    private readonly IIndustryRepository _repository;

    public CreateIndustryHandler(IIndustryRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(CreateIndustryCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.IndustryExists(command.Industry.Tag))
            return Task.FromResult(CommandResult.Fail(
                $"Industry with tag '{command.Industry.Tag}' already exists"));

        _repository.Add(command.Industry);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Industry.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateIndustryCommand>))]
public sealed class UpdateIndustryHandler : ICommandHandler<UpdateIndustryCommand>
{
    private readonly IIndustryRepository _repository;

    public UpdateIndustryHandler(IIndustryRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateIndustryCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.Get(command.Tag) == null)
            return Task.FromResult(CommandResult.Fail($"No industry with tag '{command.Tag}'"));

        _repository.Update(command.Industry);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<UpsertIndustryCommand>))]
public sealed class UpsertIndustryHandler : ICommandHandler<UpsertIndustryCommand>
{
    private readonly IIndustryRepository _repository;

    public UpsertIndustryHandler(IIndustryRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpsertIndustryCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.IndustryExists(command.Industry.Tag))
            _repository.Update(command.Industry);
        else
            _repository.Add(command.Industry);

        return Task.FromResult(CommandResult.OkWith("Tag", command.Industry.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteIndustryCommand>))]
public sealed class DeleteIndustryHandler : ICommandHandler<DeleteIndustryCommand>
{
    private readonly IIndustryRepository _repository;

    public DeleteIndustryHandler(IIndustryRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteIndustryCommand command, CancellationToken cancellationToken = default)
    {
        bool deleted = _repository.Delete(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No industry with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
