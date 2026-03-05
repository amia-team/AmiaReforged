using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IPlayerCodexRepository"/>.
/// Persists notes to the <c>codex_notes</c> table. Quest, lore, and reputation
/// data remains in-memory until their own tables are created.
/// </summary>
[ServiceBinding(typeof(IPlayerCodexRepository))]
public class EfPlayerCodexRepository : IPlayerCodexRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwContextFactory _factory;

    public EfPlayerCodexRepository(PwContextFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Loads a player's codex, hydrating notes from the database.
    /// </summary>
    public async Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            using PwEngineContext context = _factory.CreateDbContext();

            List<PersistedCodexNote> rows = await context.CodexNotes
                .Where(n => n.CharacterId == characterId.Value)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
                return null;

            // Determine creation date from the earliest note
            DateTime earliest = rows.Min(r => r.CreatedUtc);
            PlayerCodex codex = new(characterId, earliest);

            foreach (PersistedCodexNote row in rows)
            {
                CodexNoteEntry note = ToDomain(row);
                codex.AddNote(note, row.CreatedUtc);
            }

            return codex;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load codex for character {CharacterId}", characterId);
            return null;
        }
    }

    /// <summary>
    /// Saves a player's codex, upserting all notes to the database.
    /// </summary>
    public async Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default)
    {
        try
        {
            using PwEngineContext context = _factory.CreateDbContext();

            // Load existing note IDs for this character
            HashSet<Guid> existingIds = (await context.CodexNotes
                    .Where(n => n.CharacterId == codex.OwnerId.Value)
                    .Select(n => n.Id)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            HashSet<Guid> domainIds = codex.Notes.Select(n => n.Id).ToHashSet();

            // Delete notes removed from the aggregate
            List<Guid> toDelete = existingIds.Except(domainIds).ToList();
            if (toDelete.Count > 0)
            {
                await context.CodexNotes
                    .Where(n => toDelete.Contains(n.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // Upsert each note
            foreach (CodexNoteEntry note in codex.Notes)
            {
                PersistedCodexNote entity = ToEntity(note, codex.OwnerId);

                if (existingIds.Contains(note.Id))
                {
                    context.CodexNotes.Update(entity);
                }
                else
                {
                    context.CodexNotes.Add(entity);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save codex for character {CharacterId}", codex.OwnerId);
        }
    }

    // ── Mapping helpers ──

    private static CodexNoteEntry ToDomain(PersistedCodexNote row)
    {
        return new CodexNoteEntry(
            id: row.Id,
            content: row.Content,
            category: (NoteCategory)row.Category,
            dateCreated: row.CreatedUtc,
            isDmNote: row.IsDmNote,
            isPrivate: row.IsPrivate,
            title: row.Title
        );
    }

    private static PersistedCodexNote ToEntity(CodexNoteEntry note, CharacterId ownerId)
    {
        return new PersistedCodexNote
        {
            Id = note.Id,
            CharacterId = ownerId.Value,
            Title = note.Title,
            Content = note.Content,
            Category = (int)note.Category,
            IsDmNote = note.IsDmNote,
            IsPrivate = note.IsPrivate,
            CreatedUtc = note.DateCreated,
            ModifiedUtc = note.LastModified
        };
    }
}
