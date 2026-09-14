using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Creates a new recipe template. Fails if the tag already exists.
/// </summary>
public record CreateRecipeTemplateCommand : ICommand
{
    public required RecipeTemplate Template { get; init; }
}

/// <summary>
/// Updates an existing recipe template. Fails if the tag is unknown.
/// </summary>
public record UpdateRecipeTemplateCommand : ICommand
{
    public required string Tag { get; init; }
    public required RecipeTemplate Template { get; init; }
}

/// <summary>
/// Deletes a recipe template. Fails if the tag is unknown.
/// </summary>
public record DeleteRecipeTemplateCommand : ICommand
{
    public required string Tag { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateRecipeTemplateCommand>))]
public sealed class CreateRecipeTemplateHandler : ICommandHandler<CreateRecipeTemplateCommand>
{
    private readonly IRecipeTemplateRepository _repository;

    public CreateRecipeTemplateHandler(IRecipeTemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(CreateRecipeTemplateCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.GetByTag(command.Template.Tag) != null)
            return Task.FromResult(CommandResult.Fail(
                $"Recipe template with tag '{command.Template.Tag}' already exists"));

        _repository.Add(command.Template);
        return Task.FromResult(CommandResult.OkWith("Tag", command.Template.Tag));
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateRecipeTemplateCommand>))]
public sealed class UpdateRecipeTemplateHandler : ICommandHandler<UpdateRecipeTemplateCommand>
{
    private readonly IRecipeTemplateRepository _repository;

    public UpdateRecipeTemplateHandler(IRecipeTemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(UpdateRecipeTemplateCommand command, CancellationToken cancellationToken = default)
    {
        if (_repository.GetByTag(command.Tag) == null)
            return Task.FromResult(CommandResult.Fail($"No recipe template with tag '{command.Tag}'"));

        _repository.Update(command.Template);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteRecipeTemplateCommand>))]
public sealed class DeleteRecipeTemplateHandler : ICommandHandler<DeleteRecipeTemplateCommand>
{
    private readonly IRecipeTemplateRepository _repository;

    public DeleteRecipeTemplateHandler(IRecipeTemplateRepository repository)
    {
        _repository = repository;
    }

    public Task<CommandResult> HandleAsync(DeleteRecipeTemplateCommand command, CancellationToken cancellationToken = default)
    {
        bool deleted = _repository.Delete(command.Tag);
        if (!deleted)
            return Task.FromResult(CommandResult.Fail($"No recipe template with tag '{command.Tag}'"));

        return Task.FromResult(CommandResult.Ok());
    }
}
