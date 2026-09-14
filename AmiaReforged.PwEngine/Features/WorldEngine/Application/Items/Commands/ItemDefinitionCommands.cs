using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Persistence;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Commands;

/// <summary>
/// Creates or replaces an item blueprint definition (the repository Add is an upsert).
/// </summary>
public record UpsertItemDefinitionCommand : ICommand
{
    public required ItemBlueprint Blueprint { get; init; }
}

/// <summary>
/// Deletes an item blueprint definition. Only supported by database-backed repositories.
/// </summary>
public record DeleteItemDefinitionCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<UpsertItemDefinitionCommand>))]
public sealed class UpsertItemDefinitionHandler : ICommandHandler<UpsertItemDefinitionCommand>
{
    private readonly IItemDefinitionRepository _repository;

    public UpsertItemDefinitionHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpsertItemDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        _repository.AddItemDefinition(command.Blueprint);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Blueprint.ItemTag));
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteItemDefinitionCommand>))]
public sealed class DeleteItemDefinitionHandler : ICommandHandler<DeleteItemDefinitionCommand>
{
    private readonly IItemDefinitionRepository _repository;

    public DeleteItemDefinitionHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteItemDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository is not DbItemDefinitionRepository dbRepo)
            return Task.FromResult(CommandResult.Fail("Delete is only supported with database-backed repositories"));

        bool deleted = dbRepo.DeleteByTag(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No item with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
