using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;

/// <summary>
/// Creates a new lore definition. Fails if the ID already exists.
/// </summary>
public record CreateLoreDefinitionCommand : ICommand
{
    public required PersistedLoreDefinition Definition { get; init; }
}

/// <summary>
/// Updates an existing lore definition (ID is immutable). Fails if unknown.
/// </summary>
public record UpdateLoreDefinitionCommand : ICommand
{
    public required string LoreId { get; init; }
    public required PersistedLoreDefinition Definition { get; init; }
}

/// <summary>
/// Deletes a lore definition and its player unlock records. Fails if unknown.
/// </summary>
public record DeleteLoreDefinitionCommand : ICommand
{
    public required string LoreId { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateLoreDefinitionCommand>))]
public sealed class CreateLoreDefinitionHandler : ICommandHandler<CreateLoreDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;

    public CreateLoreDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(CreateLoreDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();

        bool exists = await context.CodexLoreDefinitions.AnyAsync(d => d.LoreId == command.Definition.LoreId, cancellationToken);
        if (exists)
            return CommandResult.Fail($"A lore definition with ID '{command.Definition.LoreId}' already exists");

        command.Definition.CreatedUtc = DateTime.UtcNow;

        context.CodexLoreDefinitions.Add(command.Definition);
        await context.SaveChangesAsync(cancellationToken);

        return CommandResult.OkWith("LoreId", command.Definition.LoreId);
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateLoreDefinitionCommand>))]
public sealed class UpdateLoreDefinitionHandler : ICommandHandler<UpdateLoreDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;

    public UpdateLoreDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(UpdateLoreDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedLoreDefinition? existing = await context.CodexLoreDefinitions.FindAsync([command.LoreId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No lore definition with ID '{command.LoreId}'");

        // LoreId is immutable — update mutable fields only.
        existing.Title = command.Definition.Title;
        existing.Content = command.Definition.Content;
        existing.Category = command.Definition.Category;
        existing.Tier = command.Definition.Tier;
        existing.Keywords = command.Definition.Keywords;
        existing.IsAlwaysAvailable = command.Definition.IsAlwaysAvailable;

        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteLoreDefinitionCommand>))]
public sealed class DeleteLoreDefinitionHandler : ICommandHandler<DeleteLoreDefinitionCommand>
{
    private readonly PwContextFactory _contextFactory;

    public DeleteLoreDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(DeleteLoreDefinitionCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedLoreDefinition? existing = await context.CodexLoreDefinitions.FindAsync([command.LoreId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No lore definition with ID '{command.LoreId}'");

        // Remove all player unlock records for this lore entry.
        await context.CodexLoreUnlocks
            .Where(u => u.LoreId == command.LoreId)
            .ExecuteDeleteAsync(cancellationToken);

        context.CodexLoreDefinitions.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}
