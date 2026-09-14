using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;

/// <summary>
/// Creates a new dialogue tree. Fails if the ID already exists.
/// </summary>
public record CreateDialogueTreeCommand : ICommand
{
    public required PersistedDialogueTree Tree { get; init; }
}

/// <summary>
/// Updates an existing dialogue tree (ID is immutable). Fails if unknown.
/// </summary>
public record UpdateDialogueTreeCommand : ICommand
{
    public required string DialogueTreeId { get; init; }
    public required PersistedDialogueTree Tree { get; init; }
}

/// <summary>
/// Deletes a dialogue tree. Fails if unknown.
/// </summary>
public record DeleteDialogueTreeCommand : ICommand
{
    public required string DialogueTreeId { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<CreateDialogueTreeCommand>))]
public sealed class CreateDialogueTreeHandler : ICommandHandler<CreateDialogueTreeCommand>
{
    private readonly PwContextFactory _contextFactory;

    public CreateDialogueTreeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(CreateDialogueTreeCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();

        bool exists = await context.DialogueTrees.AnyAsync(d => d.DialogueTreeId == command.Tree.DialogueTreeId, cancellationToken);
        if (exists)
            return CommandResult.Fail($"A dialogue tree with ID '{command.Tree.DialogueTreeId}' already exists");

        command.Tree.CreatedUtc = DateTime.UtcNow;

        context.DialogueTrees.Add(command.Tree);
        await context.SaveChangesAsync(cancellationToken);

        return CommandResult.OkWith("DialogueTreeId", command.Tree.DialogueTreeId);
    }
}

[ServiceBinding(typeof(ICommandHandler<UpdateDialogueTreeCommand>))]
public sealed class UpdateDialogueTreeHandler : ICommandHandler<UpdateDialogueTreeCommand>
{
    private readonly PwContextFactory _contextFactory;

    public UpdateDialogueTreeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(UpdateDialogueTreeCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedDialogueTree? existing = await context.DialogueTrees.FindAsync([command.DialogueTreeId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No dialogue tree with ID '{command.DialogueTreeId}'");

        // DialogueTreeId is immutable — update mutable fields only.
        existing.Title = command.Tree.Title;
        existing.Description = command.Tree.Description;
        existing.RootNodeId = command.Tree.RootNodeId;
        existing.SpeakerTag = command.Tree.SpeakerTag;
        existing.NodesJson = command.Tree.NodesJson;
        existing.UpdatedUtc = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteDialogueTreeCommand>))]
public sealed class DeleteDialogueTreeHandler : ICommandHandler<DeleteDialogueTreeCommand>
{
    private readonly PwContextFactory _contextFactory;

    public DeleteDialogueTreeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CommandResult> HandleAsync(DeleteDialogueTreeCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedDialogueTree? existing = await context.DialogueTrees.FindAsync([command.DialogueTreeId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No dialogue tree with ID '{command.DialogueTreeId}'");

        context.DialogueTrees.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}
