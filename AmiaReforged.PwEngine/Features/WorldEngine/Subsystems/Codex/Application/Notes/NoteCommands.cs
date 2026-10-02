using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Notes;

/// <summary>
/// Adds a player/DM note to a character's codex.
/// </summary>
public record AddNoteCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required string Content { get; init; }
    public required NoteCategory Category { get; init; }
    public bool IsDmNote { get; init; }
    public bool IsPrivate { get; init; }
    public string? Title { get; init; }
}

/// <summary>
/// Edits a character's codex note. Fails if the codex or note is unknown. (F-6 audit.)
/// </summary>
public record EditNoteCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required Guid NoteId { get; init; }
    public required string NewContent { get; init; }
    public required string? Title { get; init; }
    public required NoteCategory Category { get; init; }
}

/// <summary>
/// Deletes a character's codex note. Fails if the codex or note is unknown. (F-6 audit.)
/// </summary>
public record DeleteNoteCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required Guid NoteId { get; init; }
}

[ServiceBinding(typeof(ICommandHandler<AddNoteCommand>))]
public sealed class AddNoteHandler : ICommandHandler<AddNoteCommand>
{
    private readonly IPlayerCodexRepository _codexRepository;
    private readonly IEventBus _eventBus;

    public AddNoteHandler(IPlayerCodexRepository codexRepository, IEventBus eventBus)
    {
        _codexRepository = codexRepository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(AddNoteCommand command, CancellationToken cancellationToken = default)
    {
        CodexNoteEntry note;
        DateTime now = DateTime.UtcNow;
        try
        {
            if (!command.IsDmNote)
                CodexNoteEntry.ValidatePlayerInput(command.Title, command.Content, command.Category);
            else if (!Enum.IsDefined(command.Category) || command.Title?.Length > CodexNoteEntry.MaxTitleLength)
                return CommandResult.Fail("Invalid note category or title");

            note = new CodexNoteEntry(Guid.NewGuid(), command.Content, command.Category, now,
                command.IsDmNote, command.IsPrivate,
                string.IsNullOrWhiteSpace(command.Title) ? null : command.Title.Trim());
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        using (await CodexMutationLock.AcquireAsync(_codexRepository, command.CharacterId, cancellationToken))
        {
            PlayerCodex codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken)
                                ?? new PlayerCodex(command.CharacterId, now);
            codex.AddNote(note, now);
            await _codexRepository.SaveAsync(codex, cancellationToken);
        }

        await _eventBus.PublishAsync(
            new NoteAddedEvent(command.CharacterId, now, note.Id, note.Content,
                note.Category, note.IsDmNote, note.IsPrivate, note.Title), cancellationToken);

        return CommandResult.OkWith("noteId", note.Id.ToString());
    }
}

[ServiceBinding(typeof(ICommandHandler<EditNoteCommand>))]
public sealed class EditNoteHandler : ICommandHandler<EditNoteCommand>
{
    private readonly IPlayerCodexRepository _codexRepository;
    private readonly IEventBus _eventBus;

    public EditNoteHandler(IPlayerCodexRepository codexRepository, IEventBus eventBus)
    {
        _codexRepository = codexRepository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(EditNoteCommand command, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        using (await CodexMutationLock.AcquireAsync(_codexRepository, command.CharacterId, cancellationToken))
        {
            PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
            CodexNoteEntry? note = codex?.GetNote(command.NoteId);
            if (note == null)
                return CommandResult.Fail("Note not found in your codex");
            if (!note.CanPlayerEdit)
                return CommandResult.Fail("DM notes cannot be edited by players");

            try
            {
                codex!.EditNote(command.NoteId, command.Title, command.NewContent, command.Category, now);
            }
            catch (ArgumentException ex)
            {
                return CommandResult.Fail(ex.Message);
            }

            await _codexRepository.SaveAsync(codex!, cancellationToken);
        }

        await _eventBus.PublishAsync(
            new NoteEditedEvent(command.CharacterId, now, command.NoteId, command.NewContent,
                command.Title, command.Category), cancellationToken);

        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<DeleteNoteCommand>))]
public sealed class DeleteNoteHandler : ICommandHandler<DeleteNoteCommand>
{
    private readonly IPlayerCodexRepository _codexRepository;
    private readonly IEventBus _eventBus;

    public DeleteNoteHandler(IPlayerCodexRepository codexRepository, IEventBus eventBus)
    {
        _codexRepository = codexRepository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(DeleteNoteCommand command, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        using (await CodexMutationLock.AcquireAsync(_codexRepository, command.CharacterId, cancellationToken))
        {
            PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
            CodexNoteEntry? note = codex?.GetNote(command.NoteId);
            if (note == null)
                return CommandResult.Fail("Note not found in your codex");
            if (!note.CanPlayerEdit)
                return CommandResult.Fail("DM notes cannot be deleted by players");

            codex!.DeleteNote(command.NoteId, now);
            await _codexRepository.SaveAsync(codex, cancellationToken);
        }

        await _eventBus.PublishAsync(
            new NoteDeletedEvent(command.CharacterId, now, command.NoteId), cancellationToken);

        return CommandResult.Ok();
    }
}
