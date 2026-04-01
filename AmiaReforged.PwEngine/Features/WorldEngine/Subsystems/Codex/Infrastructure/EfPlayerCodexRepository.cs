using System.Text.Json;
using System.Text.Json.Serialization;
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
/// Persists notes to <c>codex_notes</c>, lore to <c>codex_lore_definitions</c> /
/// <c>codex_lore_unlocks</c>, and quests to <c>codex_quests</c>.
/// Reputation data remains in-memory until its own table is created.
/// </summary>
[ServiceBinding(typeof(IPlayerCodexRepository))]
public class EfPlayerCodexRepository : IPlayerCodexRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwContextFactory _factory;

    private static readonly JsonSerializerOptions StageJsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public EfPlayerCodexRepository(PwContextFactory factory)
    {
        _factory = factory;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Load
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads a player's codex, hydrating notes, lore, and quests from the database.
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

            // ── Quests ──
            List<PersistedCodexQuest> questRows = await context.CodexQuests
                .Where(q => q.CharacterId == characterId.Value)
                .ToListAsync(cancellationToken);

            if (noteRows.Count == 0 && loreRows.Count == 0 && alwaysAvailable.Count == 0 && questRows.Count == 0)
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
            if (questRows.Count > 0)
            {
                DateTime questEarliest = questRows.Min(r => r.DateStarted);
                if (questEarliest < earliest) earliest = questEarliest;
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

            // Hydrate quests
            foreach (PersistedCodexQuest row in questRows)
            {
                CodexQuestEntry quest = QuestToDomain(row);
                QuestState state = (QuestState)row.State;

                if (state == QuestState.Discovered)
                    codex.RecordQuestDiscovered(quest, row.DateStarted);
                else
                    codex.RecordQuestStarted(quest, row.DateStarted);

                // Restore the persisted state, stage, and completion date after insertion
                quest.State = state;
                quest.CurrentStageId = row.CurrentStageId;
                quest.DateCompleted = row.DateCompleted;
                quest.CompletionCount = row.CompletionCount;
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
    /// Saves a player's codex, upserting notes, lore, and quests to the database.
    /// </summary>
    public async Task SaveAsync(PlayerCodex codex, CancellationToken cancellationToken = default)
    {
        try
        {
            using PwEngineContext context = _factory.CreateDbContext();

            await SaveNotesAsync(context, codex, cancellationToken);
            await SaveLoreAsync(context, codex, cancellationToken);
            await SaveQuestsAsync(context, codex, cancellationToken);

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

    // ═══════════════════════════════════════════════════════════════════
    //  Quest persistence
    // ═══════════════════════════════════════════════════════════════════

    private static async Task SaveQuestsAsync(
        PwEngineContext context, PlayerCodex codex, CancellationToken ct)
    {
        HashSet<string> existingQuestIds = (await context.CodexQuests
                .Where(q => q.CharacterId == codex.OwnerId.Value)
                .Select(q => q.QuestId)
                .ToListAsync(ct))
            .ToHashSet();

        HashSet<string> domainQuestIds = codex.Quests.Select(q => q.QuestId.Value).ToHashSet();

        // Delete quests removed from the aggregate
        List<string> toDelete = existingQuestIds.Except(domainQuestIds).ToList();
        if (toDelete.Count > 0)
        {
            await context.CodexQuests
                .Where(q => q.CharacterId == codex.OwnerId.Value && toDelete.Contains(q.QuestId))
                .ExecuteDeleteAsync(ct);
        }

        // Upsert each quest
        foreach (CodexQuestEntry quest in codex.Quests)
        {
            PersistedCodexQuest entity = QuestToEntity(quest, codex.OwnerId);

            if (existingQuestIds.Contains(quest.QuestId.Value))
                context.CodexQuests.Update(entity);
            else
                context.CodexQuests.Add(entity);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Mapping helpers — Quests
    // ═══════════════════════════════════════════════════════════════════

    private static CodexQuestEntry QuestToDomain(PersistedCodexQuest row)
    {
        List<Keyword> keywords = ParseKeywords(row.Keywords);
        List<QuestStage> stages = DeserializeStages(row.StagesJson);

        TemplateId? sourceTemplate = null;
        if (row.SourceTemplateId != null && Guid.TryParse(row.SourceTemplateId, out Guid templateGuid))
            sourceTemplate = (TemplateId)templateGuid;

        return new CodexQuestEntry
        {
            QuestId = (QuestId)row.QuestId,
            Title = row.Title,
            Description = row.Description,
            State = (QuestState)row.State,
            DateStarted = row.DateStarted,
            QuestGiver = row.QuestGiver,
            Location = row.Location,
            Keywords = keywords,
            Stages = stages,
            SourceTemplateId = sourceTemplate,
            Deadline = row.Deadline,
            ExpiryBehavior = row.ExpiryBehavior.HasValue
                ? (ExpiryBehavior)row.ExpiryBehavior.Value
                : null,
            CompletionCount = row.CompletionCount
        };
    }

    private static PersistedCodexQuest QuestToEntity(CodexQuestEntry quest, CharacterId ownerId)
    {
        return new PersistedCodexQuest
        {
            CharacterId = ownerId.Value,
            QuestId = quest.QuestId.Value,
            Title = quest.Title,
            Description = quest.Description,
            State = (int)quest.State,
            CurrentStageId = quest.CurrentStageId,
            DateStarted = quest.DateStarted,
            DateCompleted = quest.DateCompleted,
            QuestGiver = quest.QuestGiver,
            Location = quest.Location,
            Keywords = quest.Keywords.Count > 0
                ? string.Join(",", quest.Keywords.Select(k => (string)k))
                : null,
            StagesJson = SerializeStages(quest.Stages),
            SourceTemplateId = quest.SourceTemplateId?.Value.ToString(),
            Deadline = quest.Deadline,
            ExpiryBehavior = quest.ExpiryBehavior.HasValue ? (int)quest.ExpiryBehavior.Value : null,
            CompletionCount = quest.CompletionCount
        };
    }

    private static string SerializeStages(List<QuestStage> stages)
    {
        try
        {
            return JsonSerializer.Serialize(stages, StageJsonOpts);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to serialize quest stages, defaulting to empty array");
            return "[]";
        }
    }

    private static List<QuestStage> DeserializeStages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json is "[]" or "null")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<QuestStage>>(json, StageJsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to deserialize quest stages JSON");
            return [];
        }
    }
}
