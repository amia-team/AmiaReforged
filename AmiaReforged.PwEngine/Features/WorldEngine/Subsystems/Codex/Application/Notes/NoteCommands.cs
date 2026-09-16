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
/// Adds a player/DM note to a character's codex. (F-6 audit: CQRS envelope over
/// <see cref="PlayerCodex.AddNote"/>; in-development feature, no game callers yet.)
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
        DateTime now = DateTime.UtcNow;
        PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
        codex ??= new PlayerCodex(command.CharacterId, now);

        CodexNoteEntry note = new(
            Guid.NewGuid(),
            command.Content,
            command.Category,
            now,
            command.IsDmNote,
            command.IsPrivate,
            command.Title);

        try
        {
            codex.AddNote(note, now);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return CommandResult.Fail(ex.Message);
        }

        await _codexRepository.SaveAsync(codex, cancellationToken);

        await _eventBus.PublishAsync(
            new NoteAddedEvent(
                command.CharacterId, now, note.Id, note.Content,
                note.Category, note.IsDmNote, note.IsPrivate),
            cancellationToken);

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
        PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
        if (codex is null)
            return CommandResult.Fail($"Codex not found for character '{command.CharacterId.Value}'");

        try
        {
            codex.EditNote(command.NoteId, command.NewContent, DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return CommandResult.Fail(ex.Message);
        }

        await _codexRepository.SaveAsync(codex, cancellationToken);

        await _eventBus.PublishAsync(
            new NoteEditedEvent(command.CharacterId, DateTime.UtcNow, command.NoteId, command.NewContent),
            cancellationToken);

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
        PlayerCodex? codex = await _codexRepository.LoadAsync(command.CharacterId, cancellationToken);
        if (codex is null)
            return CommandResult.Fail($"Codex not found for character '{command.CharacterId.Value}'");

        try
        {
            codex.DeleteNote(command.NoteId, DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        await _codexRepository.SaveAsync(codex, cancellationToken);

        await _eventBus.PublishAsync(
            new NoteDeletedEvent(command.CharacterId, DateTime.UtcNow, command.NoteId),
            cancellationToken);

        return CommandResult.Ok();
    }
}
