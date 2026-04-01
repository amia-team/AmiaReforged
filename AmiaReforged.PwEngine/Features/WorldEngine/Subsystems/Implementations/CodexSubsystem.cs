using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Wired implementation of the Codex subsystem.
/// Maps the ICodexSubsystem "knowledge entry" API onto the underlying lore domain.
/// </summary>
[ServiceBinding(typeof(ICodexSubsystem))]
public sealed class CodexSubsystem : ICodexSubsystem
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IPlayerCodexRepository _codexRepository;
    private readonly PwContextFactory _contextFactory;
    private readonly ICommandHandler<OpenCodexCommand> _openHandler;
    private readonly ICommandHandler<CloseCodexCommand> _closeHandler;
    private readonly WindowDirector _windowDirector;

    public CodexSubsystem(
        IPlayerCodexRepository codexRepository,
        PwContextFactory contextFactory,
        ICommandHandler<OpenCodexCommand> openHandler,
        ICommandHandler<CloseCodexCommand> closeHandler,
        WindowDirector windowDirector)
    {
        _codexRepository = codexRepository;
        _contextFactory = contextFactory;
        _openHandler = openHandler;
        _closeHandler = closeHandler;
        _windowDirector = windowDirector;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Codex Window Lifecycle
    // ═══════════════════════════════════════════════════════════════════

    public Task<CommandResult> OpenCodexAsync(NwPlayer player)
    {
        return _openHandler.HandleAsync(new OpenCodexCommand { Player = player });
    }

    public Task<CommandResult> CloseCodexAsync(NwPlayer player)
    {
        return _closeHandler.HandleAsync(new CloseCodexCommand { Player = player });
    }

    public bool IsCodexOpen(NwPlayer player)
    {
        return _windowDirector.IsWindowOpen(player, typeof(PlayerCodexPresenter));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Knowledge entry queries (backed by PersistedLoreDefinition)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<KnowledgeEntry?> GetKnowledgeEntryAsync(string entryId, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();
            PersistedLoreDefinition? def = await ctx.CodexLoreDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.LoreId == entryId, ct);
            return def is null ? null : ToKnowledgeEntry(def);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load knowledge entry {EntryId}", entryId);
            return null;
        }
    }

    public async Task<List<KnowledgeEntry>> SearchKnowledgeAsync(string searchTerm, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();
            string lower = searchTerm.ToLowerInvariant();
            List<PersistedLoreDefinition> defs = await ctx.CodexLoreDefinitions
                .AsNoTracking()
                .Where(d => EF.Functions.ILike(d.Title, $"%{lower}%")
                         || EF.Functions.ILike(d.Content, $"%{lower}%")
                         || (d.Keywords != null && EF.Functions.ILike(d.Keywords, $"%{lower}%")))
                .ToListAsync(ct);
            return defs.Select(ToKnowledgeEntry).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to search knowledge for '{SearchTerm}'", searchTerm);
            return [];
        }
    }

    public async Task<List<KnowledgeEntry>> GetKnowledgeByCategoryAsync(KnowledgeCategory category, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();
            LoreCategory loreCategory = MapToLoreCategory(category);
            List<PersistedLoreDefinition> defs = await ctx.CodexLoreDefinitions
                .AsNoTracking()
                .Where(d => d.Category == loreCategory)
                .ToListAsync(ct);
            return defs.Select(ToKnowledgeEntry).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load knowledge by category {Category}", category);
            return [];
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Character-specific knowledge operations
    // ═══════════════════════════════════════════════════════════════════

    public async Task<CommandResult> GrantKnowledgeAsync(CharacterId characterId, string entryId, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();

            // Verify definition exists
            bool exists = await ctx.CodexLoreDefinitions.AnyAsync(d => d.LoreId == entryId, ct);
            if (!exists)
                return CommandResult.Fail($"Knowledge entry '{entryId}' does not exist");

            // Check if already unlocked
            bool alreadyUnlocked = await ctx.CodexLoreUnlocks
                .AnyAsync(u => u.CharacterId == characterId.Value && u.LoreId == entryId, ct);
            if (alreadyUnlocked)
                return CommandResult.Fail($"Character already has knowledge '{entryId}'");

            ctx.CodexLoreUnlocks.Add(new PersistedLoreUnlock
            {
                CharacterId = characterId.Value,
                LoreId = entryId,
                DateDiscovered = DateTime.UtcNow,
                DiscoverySource = "Admin Grant"
            });
            await ctx.SaveChangesAsync(ct);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to grant knowledge {EntryId} to character {CharacterId}", entryId, characterId);
            return CommandResult.Fail($"Database error: {ex.Message}");
        }
    }

    public async Task<bool> HasKnowledgeAsync(CharacterId characterId, string entryId, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();

            // Check unlock table
            bool unlocked = await ctx.CodexLoreUnlocks
                .AnyAsync(u => u.CharacterId == characterId.Value && u.LoreId == entryId, ct);
            if (unlocked) return true;

            // Check if always-available
            return await ctx.CodexLoreDefinitions
                .AnyAsync(d => d.LoreId == entryId && d.IsAlwaysAvailable, ct);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check knowledge {EntryId} for character {CharacterId}", entryId, characterId);
            return false;
        }
    }

    public async Task<List<KnowledgeEntry>> GetCharacterKnowledgeAsync(CharacterId characterId, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();

            // Get unlocked lore
            List<PersistedLoreDefinition> unlocked = await ctx.CodexLoreUnlocks
                .Include(u => u.LoreDefinition)
                .Where(u => u.CharacterId == characterId.Value && u.LoreDefinition != null)
                .Select(u => u.LoreDefinition!)
                .ToListAsync(ct);

            // Get always-available entries
            HashSet<string> unlockedIds = unlocked.Select(d => d.LoreId).ToHashSet();
            List<PersistedLoreDefinition> alwaysAvailable = await ctx.CodexLoreDefinitions
                .Where(d => d.IsAlwaysAvailable && !unlockedIds.Contains(d.LoreId))
                .ToListAsync(ct);

            return unlocked.Concat(alwaysAvailable).Select(ToKnowledgeEntry).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load character knowledge for {CharacterId}", characterId);
            return [];
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CRUD operations on knowledge entries (lore definitions)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<CommandResult> CreateKnowledgeEntryAsync(CreateKnowledgeEntryCommand command, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();

            bool exists = await ctx.CodexLoreDefinitions.AnyAsync(d => d.LoreId == command.EntryId, ct);
            if (exists)
                return CommandResult.Fail($"Knowledge entry '{command.EntryId}' already exists");

            ctx.CodexLoreDefinitions.Add(new PersistedLoreDefinition
            {
                LoreId = command.EntryId,
                Title = command.Title,
                Content = command.Content,
                Category = MapToLoreCategory(command.Category),
                Tier = 0, // Default tier
                Keywords = command.Tags.Count > 0 ? string.Join(",", command.Tags) : null,
                IsAlwaysAvailable = false,
                CreatedUtc = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync(ct);
            return CommandResult.OkWith("EntryId", command.EntryId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create knowledge entry {EntryId}", command.EntryId);
            return CommandResult.Fail($"Database error: {ex.Message}");
        }
    }

    public async Task<CommandResult> UpdateKnowledgeEntryAsync(UpdateKnowledgeEntryCommand command, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();

            PersistedLoreDefinition? def = await ctx.CodexLoreDefinitions
                .FirstOrDefaultAsync(d => d.LoreId == command.EntryId, ct);
            if (def is null)
                return CommandResult.Fail($"Knowledge entry '{command.EntryId}' not found");

            if (command.Title != null) def.Title = command.Title;
            if (command.Content != null) def.Content = command.Content;
            if (command.Category.HasValue) def.Category = MapToLoreCategory(command.Category.Value);
            if (command.Tags != null) def.Keywords = command.Tags.Count > 0 ? string.Join(",", command.Tags) : null;

            ctx.CodexLoreDefinitions.Update(def);
            await ctx.SaveChangesAsync(ct);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update knowledge entry {EntryId}", command.EntryId);
            return CommandResult.Fail($"Database error: {ex.Message}");
        }
    }

    public async Task<CommandResult> DeleteKnowledgeEntryAsync(string entryId, CancellationToken ct = default)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();

            PersistedLoreDefinition? def = await ctx.CodexLoreDefinitions
                .FirstOrDefaultAsync(d => d.LoreId == entryId, ct);
            if (def is null)
                return CommandResult.Fail($"Knowledge entry '{entryId}' not found");

            // Remove all unlock records first
            List<PersistedLoreUnlock> unlocks = await ctx.CodexLoreUnlocks
                .Where(u => u.LoreId == entryId)
                .ToListAsync(ct);
            ctx.CodexLoreUnlocks.RemoveRange(unlocks);

            ctx.CodexLoreDefinitions.Remove(def);
            await ctx.SaveChangesAsync(ct);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete knowledge entry {EntryId}", entryId);
            return CommandResult.Fail($"Database error: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Quest Stage Management
    // ═══════════════════════════════════════════════════════════════════

    public async Task<CommandResult> SetQuestStageAsync(
        CharacterId characterId,
        string questId,
        int stageId,
        CancellationToken ct = default)
    {
        try
        {
            QuestId qid = (QuestId)questId;
            DateTime now = DateTime.UtcNow;

            // Load the player's codex (or create one if it doesn't exist)
            PlayerCodex? codex = await _codexRepository.LoadAsync(characterId);
            if (codex == null)
            {
                codex = new PlayerCodex(characterId, now);
            }

            if (codex.HasQuest(qid))
            {
                // Quest already in codex — advance to the requested stage
                codex.AdvanceQuestStage(qid, stageId, now);
            }
            else
            {
                // Quest not in codex — look up the definition and add it
                using PwEngineContext ctx = _contextFactory.CreateDbContext();
                PersistedQuestDefinition? definition = await ctx.CodexQuestDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.QuestId == questId, ct);

                if (definition == null)
                    return CommandResult.Fail($"Quest definition '{questId}' not found");

                // Build a new codex entry from the definition
                CodexQuestEntry entry = new()
                {
                    QuestId = qid,
                    Title = definition.Title,
                    Description = definition.Description,
                    DateStarted = now,
                    QuestGiver = definition.QuestGiver,
                    Location = definition.Location,
                    Keywords = ParseKeywords(definition.Keywords)
                };

                // Add to codex in InProgress state, then advance to the requested stage
                codex.RecordQuestStarted(entry, now);
                codex.AdvanceQuestStage(qid, stageId, now);
            }

            await _codexRepository.SaveAsync(codex);
            Log.Info("SetQuestStage: quest '{QuestId}' → stage {StageId} for character {CharacterId}",
                questId, stageId, characterId);
            return CommandResult.OkWith("questId", questId);
        }
        catch (InvalidOperationException ex)
        {
            Log.Warn(ex, "SetQuestStage domain error for quest '{QuestId}'", questId);
            return CommandResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SetQuestStage failed for quest '{QuestId}' character {CharacterId}", questId, characterId);
            return CommandResult.Fail($"Database error: {ex.Message}");
        }
    }

    private static List<Keyword> ParseKeywords(string? keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords)) return [];
        return keywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(k => new Keyword(k))
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Mapping helpers
    // ═══════════════════════════════════════════════════════════════════

    private static KnowledgeEntry ToKnowledgeEntry(PersistedLoreDefinition def)
    {
        List<string> tags = string.IsNullOrWhiteSpace(def.Keywords)
            ? []
            : def.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        return new KnowledgeEntry(
            EntryId: def.LoreId,
            Title: def.Title,
            Content: def.Content,
            Category: MapToKnowledgeCategory(def.Category),
            Tags: tags,
            CreatedAt: def.CreatedUtc,
            UpdatedAt: null);
    }

    private static LoreCategory MapToLoreCategory(KnowledgeCategory category) => category switch
    {
        KnowledgeCategory.History => LoreCategory.History,
        KnowledgeCategory.Geography => LoreCategory.Geography,
        KnowledgeCategory.Magic => LoreCategory.Arcana,
        KnowledgeCategory.Religion => LoreCategory.Religion,
        KnowledgeCategory.Nature => LoreCategory.Nature,
        KnowledgeCategory.Culture => LoreCategory.Local,
        KnowledgeCategory.Organizations => LoreCategory.NobilityAndRoyalty,
        KnowledgeCategory.Creatures => LoreCategory.Nature,
        KnowledgeCategory.Items => LoreCategory.Arcana,
        KnowledgeCategory.Persons => LoreCategory.Local,
        KnowledgeCategory.Events => LoreCategory.History,
        KnowledgeCategory.Legends => LoreCategory.ThePlanes,
        KnowledgeCategory.Secrets => LoreCategory.Dungeoneering,
        _ => LoreCategory.Local
    };

    private static KnowledgeCategory MapToKnowledgeCategory(LoreCategory category) => category switch
    {
        LoreCategory.History => KnowledgeCategory.History,
        LoreCategory.Geography => KnowledgeCategory.Geography,
        LoreCategory.Arcana => KnowledgeCategory.Magic,
        LoreCategory.Religion => KnowledgeCategory.Religion,
        LoreCategory.Nature => KnowledgeCategory.Nature,
        LoreCategory.Local => KnowledgeCategory.Culture,
        LoreCategory.NobilityAndRoyalty => KnowledgeCategory.Organizations,
        LoreCategory.ThePlanes => KnowledgeCategory.Legends,
        LoreCategory.Dungeoneering => KnowledgeCategory.Secrets,
        LoreCategory.ArchitectureAndEngineering => KnowledgeCategory.Culture,
        LoreCategory.Ooc => KnowledgeCategory.Culture,
        _ => KnowledgeCategory.Culture
    };
}

