using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IPlayerCodexRepository"/>.
/// Persists notes to <c>codex_notes</c> and lore to <c>codex_lore_definitions</c> /
/// <c>codex_lore_unlocks</c>. Quest and reputation data remains in-memory until
/// their own tables are created.
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

    // ═══════════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads a player's codex, hydrating notes and lore from the database.
    /// </summary>
    public async Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        try
        {
            using PwEngineContext context = _factory.CreateDbContext();

            // ── Notes ──
            List<PersistedCodexNote> noteRows = await context.CodexNotes
                .Where(n => n.CharacterId == characterId.Value)
                .ToListAsync(cancellationToken);

            // ── Lore unlocks (with definition eagerly loaded) ──
            List<PersistedLoreUnlock> loreRows = await context.CodexLoreUnlocks
                .Include(u => u.LoreDefinition)
                .Where(u => u.CharacterId == characterId.Value)
                .ToListAsync(cancellationToken);

            // ── Always-available definitions (no unlock record needed) ──
            HashSet<string> unlockedIds = loreRows
                .Where(r => r.LoreDefinition is not null)
                .Select(r => r.LoreId)
                .ToHashSet();

            List<PersistedLoreDefinition> alwaysAvailable = await context.CodexLoreDefinitions
                .Where(d => d.IsAlwaysAvailable && !unlockedIds.Contains(d.LoreId))
                .ToListAsync(cancellationToken);

            if (noteRows.Count == 0 && loreRows.Count == 0 && alwaysAvailable.Count == 0)
                return null;

            // Determine creation date from the earliest persisted record
            DateTime earliest = DateTime.UtcNow;
            if (noteRows.Count > 0)
                earliest = noteRows.Min(r => r.CreatedUtc);
            if (loreRows.Count > 0)
            {
                DateTime loreEarliest = loreRows.Min(r => r.DateDiscovered);
                if (loreEarliest < earliest) earliest = loreEarliest;
            }

            PlayerCodex codex = new(characterId, earliest);

            foreach (PersistedCodexNote row in noteRows)
            {
                CodexNoteEntry note = NoteToDomain(row);
                codex.AddNote(note, row.CreatedUtc);
            }

            foreach (PersistedLoreUnlock row in loreRows)
            {
                if (row.LoreDefinition is null) continue;
                CodexLoreEntry lore = LoreToDomain(row.LoreDefinition, row);
                codex.RecordLoreDiscovered(lore, row.DateDiscovered);
            }

            // Add always-available entries with a synthetic discovery date
            foreach (PersistedLoreDefinition def in alwaysAvailable)
            {
                CodexLoreEntry lore = AlwaysAvailableToDomain(def);
                codex.RecordLoreDiscovered(lore, def.CreatedUtc);
            }

            return codex;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load codex for character {CharacterId}", characterId);
            return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Save
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Saves a player's codex, upserting notes and lore to the database.
    /// </summary>
    public async Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default)
    {
        try
        {
            using PwEngineContext context = _factory.CreateDbContext();

            await SaveNotesAsync(context, codex, cancellationToken);
            await SaveLoreAsync(context, codex, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save codex for character {CharacterId}", codex.OwnerId);
        }
    }

    // ── Notes persistence ──

    private static async Task SaveNotesAsync(
        PwEngineContext context, PlayerCodex codex, CancellationToken ct)
    {
        HashSet<Guid> existingIds = (await context.CodexNotes
                .Where(n => n.CharacterId == codex.OwnerId.Value)
                .Select(n => n.Id)
                .ToListAsync(ct))
            .ToHashSet();

        HashSet<Guid> domainIds = codex.Notes.Select(n => n.Id).ToHashSet();

        // Delete notes removed from the aggregate
        List<Guid> toDelete = existingIds.Except(domainIds).ToList();
        if (toDelete.Count > 0)
        {
            await context.CodexNotes
                .Where(n => toDelete.Contains(n.Id))
                .ExecuteDeleteAsync(ct);
        }

        // Upsert each note
        foreach (CodexNoteEntry note in codex.Notes)
        {
            PersistedCodexNote entity = NoteToEntity(note, codex.OwnerId);

            if (existingIds.Contains(note.Id))
                context.CodexNotes.Update(entity);
            else
                context.CodexNotes.Add(entity);
        }
    }

    // ── Lore persistence ──

    private static async Task SaveLoreAsync(
        PwEngineContext context, PlayerCodex codex, CancellationToken ct)
    {
        // Existing unlock lore IDs for this character
        HashSet<string> existingUnlockIds = (await context.CodexLoreUnlocks
                .Where(u => u.CharacterId == codex.OwnerId.Value)
                .Select(u => u.LoreId)
                .ToListAsync(ct))
            .ToHashSet();

        HashSet<string> domainLoreIds = codex.Lore.Select(l => l.LoreId.Value).ToHashSet();

        // Remove unlocks that are no longer in the aggregate
        List<string> toRemove = existingUnlockIds.Except(domainLoreIds).ToList();
        if (toRemove.Count > 0)
        {
            await context.CodexLoreUnlocks
                .Where(u => u.CharacterId == codex.OwnerId.Value && toRemove.Contains(u.LoreId))
                .ExecuteDeleteAsync(ct);
        }

        // Upsert definitions and unlocks
        // Load existing definitions so we can preserve admin-managed fields (IsAlwaysAvailable)
        Dictionary<string, PersistedLoreDefinition> existingDefs = await context.CodexLoreDefinitions
            .Where(d => codex.Lore.Select(l => l.LoreId.Value).Contains(d.LoreId))
            .ToDictionaryAsync(d => d.LoreId, ct);

        HashSet<string> existingDefIds = (await context.CodexLoreDefinitions
                .Select(d => d.LoreId)
                .ToListAsync(ct))
            .ToHashSet();

        foreach (CodexLoreEntry lore in codex.Lore)
        {
            (PersistedLoreDefinition def, PersistedLoreUnlock unlock) = LoreToEntities(lore, codex.OwnerId);

            // Upsert global definition (may already exist from another player or admin)
            if (existingDefIds.Contains(def.LoreId))
            {
                // Preserve admin-managed IsAlwaysAvailable flag
                if (existingDefs.TryGetValue(def.LoreId, out PersistedLoreDefinition? existing))
                    def.IsAlwaysAvailable = existing.IsAlwaysAvailable;
                context.CodexLoreDefinitions.Update(def);
            }
            else
            {
                context.CodexLoreDefinitions.Add(def);
                existingDefIds.Add(def.LoreId);
            }

            // Upsert unlock record
            if (existingUnlockIds.Contains(lore.LoreId.Value))
                context.CodexLoreUnlocks.Update(unlock);
            else
                context.CodexLoreUnlocks.Add(unlock);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Mapping helpers — Notes
    // ═══════════════════════════════════════════════════════════════════

    private static CodexNoteEntry NoteToDomain(PersistedCodexNote row)
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

    private static PersistedCodexNote NoteToEntity(CodexNoteEntry note, CharacterId ownerId)
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

    // ═══════════════════════════════════════════════════════════════════
    //  Mapping helpers — Lore
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Merges a global lore definition with a per-player unlock to produce a
    /// <see cref="CodexLoreEntry"/> domain object.
    /// </summary>
    private static CodexLoreEntry LoreToDomain(PersistedLoreDefinition def, PersistedLoreUnlock unlock)
    {
        List<Keyword> keywords = ParseKeywords(def.Keywords);

        return new CodexLoreEntry
        {
            LoreId = (LoreId)def.LoreId,
            Title = def.Title,
            Content = def.Content,
            Category = def.Category,
            Tier = (LoreTier)def.Tier,
            DateDiscovered = unlock.DateDiscovered,
            DiscoveryLocation = unlock.DiscoveryLocation,
            DiscoverySource = unlock.DiscoverySource,
            Keywords = keywords
        };
    }

    /// <summary>
    /// Converts an always-available definition into a <see cref="CodexLoreEntry"/>
    /// domain object without requiring an unlock record.
    /// </summary>
    private static CodexLoreEntry AlwaysAvailableToDomain(PersistedLoreDefinition def)
    {
        List<Keyword> keywords = ParseKeywords(def.Keywords);

        return new CodexLoreEntry
        {
            LoreId = (LoreId)def.LoreId,
            Title = def.Title,
            Content = def.Content,
            Category = def.Category,
            Tier = (LoreTier)def.Tier,
            DateDiscovered = def.CreatedUtc,
            DiscoveryLocation = null,
            DiscoverySource = "Always Available",
            Keywords = keywords
        };
    }

    private static List<Keyword> ParseKeywords(string? keywordsString)
    {
        return string.IsNullOrWhiteSpace(keywordsString)
            ? []
            : keywordsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(k => (Keyword)k)
                .ToList();
    }

    /// <summary>
    /// Splits a <see cref="CodexLoreEntry"/> domain object back into a global definition
    /// entity and a per-player unlock entity.
    /// </summary>
    private static (PersistedLoreDefinition Def, PersistedLoreUnlock Unlock) LoreToEntities(
        CodexLoreEntry lore, CharacterId ownerId)
    {
        PersistedLoreDefinition def = new()
        {
            LoreId = lore.LoreId.Value,
            Title = lore.Title,
            Content = lore.Content,
            Category = lore.Category,
            Tier = (int)lore.Tier,
            Keywords = lore.Keywords.Count > 0
                ? string.Join(",", lore.Keywords.Select(k => (string)k))
                : null,
            CreatedUtc = DateTime.UtcNow
        };

        PersistedLoreUnlock unlock = new()
        {
            CharacterId = ownerId.Value,
            LoreId = lore.LoreId.Value,
            DateDiscovered = lore.DateDiscovered,
            DiscoveryLocation = lore.DiscoveryLocation,
            DiscoverySource = lore.DiscoverySource
        };

        return (def, unlock);
    }
}
