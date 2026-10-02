using System.Text.Json;
using AmiaReforged.Shared.Dialogue;
using Npgsql;
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
    public DateTime? DeletedRevisionUtc { get; set; }
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
        string? validation = DialogueTreeWriteValidation.Validate(command.Tree);
        if (validation != null) return CommandResult.Fail(validation);
        using PwEngineContext context = _contextFactory.CreateDbContext();

        bool exists = await context.DialogueTrees.AnyAsync(d => d.DialogueTreeId == command.Tree.DialogueTreeId, cancellationToken);
        if (exists)
            return CommandResult.Fail($"A dialogue tree with ID '{command.Tree.DialogueTreeId}' already exists");

        if (command.Tree.SpeakerTag != null && await context.DialogueTrees.AnyAsync(
                d => d.SpeakerTag == command.Tree.SpeakerTag, cancellationToken))
            return CommandResult.Fail($"Speaker tag '{command.Tree.SpeakerTag}' is already assigned to another dialogue");

        command.Tree.CreatedUtc = DateTime.UtcNow;

        context.DialogueTrees.Add(command.Tree);
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "ix_dialogue_trees_speaker_tag" })
        {
            return CommandResult.Fail($"Speaker tag '{command.Tree.SpeakerTag}' is already assigned to another dialogue");
        }

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
        string? validation = DialogueTreeWriteValidation.Validate(command.Tree);
        if (validation != null) return CommandResult.Fail(validation);
        using PwEngineContext context = _contextFactory.CreateDbContext();
        PersistedDialogueTree? existing = await context.DialogueTrees.FindAsync([command.DialogueTreeId], cancellationToken);

        if (existing is null)
            return CommandResult.Fail($"No dialogue tree with ID '{command.DialogueTreeId}'");

        if (command.Tree.SpeakerTag != null && await context.DialogueTrees.AnyAsync(
                d => d.DialogueTreeId != command.DialogueTreeId && d.SpeakerTag == command.Tree.SpeakerTag, cancellationToken))
            return CommandResult.Fail($"Speaker tag '{command.Tree.SpeakerTag}' is already assigned to another dialogue");

        // DialogueTreeId is immutable — update mutable fields only.
        existing.Title = command.Tree.Title;
        existing.Description = command.Tree.Description;
        existing.RootNodeId = command.Tree.RootNodeId;
        existing.SpeakerTag = command.Tree.SpeakerTag;
        existing.NodesJson = command.Tree.NodesJson;
        existing.UpdatedUtc = DateTime.UtcNow;
        command.Tree.UpdatedUtc = existing.UpdatedUtc;

        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "ix_dialogue_trees_speaker_tag" })
        {
            return CommandResult.Fail($"Speaker tag '{command.Tree.SpeakerTag}' is already assigned to another dialogue");
        }
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

        command.DeletedRevisionUtc = existing.UpdatedUtc ?? existing.CreatedUtc;
        context.DialogueTrees.Remove(existing);
        await context.SaveChangesAsync(cancellationToken);
        return CommandResult.Ok();
    }
}

internal static class DialogueTreeWriteValidation
{
    public static string? Validate(PersistedDialogueTree tree)
    {
        try
        {
            DialogueTreeDto dto = new()
            {
                DialogueTreeId = tree.DialogueTreeId, Title = tree.Title, Description = tree.Description ?? "",
                SpeakerTag = tree.SpeakerTag, RootNodeId = tree.RootNodeId,
                Nodes = JsonSerializer.Deserialize<List<DialogueNodeDto>>(tree.NodesJson ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []
            };
            DialogueDefinitionValidator.Normalize(dto);
            List<string> errors = DialogueDefinitionValidator.Validate(dto);
            if (errors.Count > 0) return string.Join("\n", errors);
            tree.SpeakerTag = string.IsNullOrWhiteSpace(tree.SpeakerTag) ? null : tree.SpeakerTag.Trim();
            tree.RootNodeId = dto.RootNodeId;
            tree.NodesJson = JsonSerializer.Serialize(dto.Nodes);
            return null;
        }
        catch (JsonException ex) { return $"Invalid dialogue definition: {ex.Message}"; }
    }
}
