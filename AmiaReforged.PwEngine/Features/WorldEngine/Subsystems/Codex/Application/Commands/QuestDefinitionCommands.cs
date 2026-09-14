using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;

/// <summary>
/// Creates a new quest definition. Fails if the ID already exists.
/// </summary>
public record CreateQuestDefinitionCommand : ICommand
{
    public required PersistedQuestDefinition Definition { get; init; }
}

/// <summary>
/// Updates an existing quest definition (ID is immutable). Fails if unknown.
/// </summary>
public record UpdateQuestDefinitionCommand : ICommand
{
    public required string QuestId { get; init; }
    public required PersistedQuestDefinition Definition { get; init; }
}

/// <summary>
/// Deletes a quest definition. Fails if unknown.
/// </summary>
public record DeleteQuestDefinitionCommand : ICommand
{
    public required string QuestId { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateQuestDefinitionCommand>))]
public sealed class CreateQuestDefinitionHandler : ICommandHandler<CreateQuestDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;

    public CreateQuestDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(CreateQuestDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();

        bool exists = await context.CodexQuestDefinitions.AnyAsync(d => d.QuestId == command.Definition.QuestId, cancellationToken);
        if (exists)
            return CommandResult.Fail($"A quest definition with ID '{command.Definition.QuestId}' already exists");

        command.Definition.CreatedUtc = DateTime.UtcNow;

        context.CodexQuestDefinitions.Add(command.Definition);
        await context.SaveChangesAsync(cancellationToken);

        return CommandResult.OkWith("QuestId", command.Definition.QuestId);
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateQuestDefinitionCommand>))]
public sealed class UpdateQuestDefinitionHandler : ICommandHandler<UpdateQuestDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;

    public UpdateQuestDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(UpdateQuestDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedQuestDefinition? existing = await context.CodexQuestDefinitions.FindAsync([command.QuestId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No quest definition with ID '{command.QuestId}'");

        // QuestId is immutable — update mutable fields only.
        existing.Title = command.Definition.Title;
        existing.Description = command.Definition.Description;
        existing.StagesJson = command.Definition.StagesJson;
        existing.CompletionRewardJson = command.Definition.CompletionRewardJson;
        existing.QuestGiver = command.Definition.QuestGiver;
        existing.Location = command.Definition.Location;
        existing.Keywords = command.Definition.Keywords;
        existing.IsAlwaysAvailable = command.Definition.IsAlwaysAvailable;

        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteQuestDefinitionCommand>))]
public sealed class DeleteQuestDefinitionHandler : ICommandHandler<DeleteQuestDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;

    public DeleteQuestDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(DeleteQuestDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedQuestDefinition? existing = await context.CodexQuestDefinitions.FindAsync([command.QuestId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No quest definition with ID '{command.QuestId}'");

        context.CodexQuestDefinitions.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}
