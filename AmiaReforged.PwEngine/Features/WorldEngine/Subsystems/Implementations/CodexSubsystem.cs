using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.PlayerKnowledge;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Quests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Thin dispatch wrapper over the Codex command/query handlers.
/// All supported operations route through the central dispatchers so
/// writes get logging, the exception-to-Fail contract, and
/// CommandExecutedEvent publishing. (F-6 audit.)
/// </summary>
[ServiceBinding(typeof(ICodexSubsystem))]
public sealed class CodexSubsystem : ICodexSubsystem
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly WindowDirector _windowDirector;

    public CodexSubsystem(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        WindowDirector windowDirector)
    {
        _commands = commands;
        _queries = queries;
        _windowDirector = windowDirector;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Codex Window Lifecycle
    // ═══════════════════════════════════════════════════════════════════

    public Task<CommandResult> OpenCodexAsync(NwPlayer player)
    {
        return _commands.DispatchAsync(new OpenCodexCommand { Player = player });
    }

    public Task<CommandResult> CloseCodexAsync(NwPlayer player)
    {
        return _commands.DispatchAsync(new CloseCodexCommand { Player = player });
    }

    public bool IsCodexOpen(NwPlayer player)
    {
        return _windowDirector.IsWindowOpen(player, typeof(PlayerCodexPresenter));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Knowledge entry queries (backed by PersistedLoreDefinition)
    // ═══════════════════════════════════════════════════════════════════

    public Task<KnowledgeEntry?> GetKnowledgeEntryAsync(string entryId, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetKnowledgeEntryQuery, KnowledgeEntry?>(
            new GetKnowledgeEntryQuery { EntryId = entryId }, ct);
    }

    public Task<List<KnowledgeEntry>> SearchKnowledgeAsync(string searchTerm, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<SearchKnowledgeQuery, List<KnowledgeEntry>>(
            new SearchKnowledgeQuery { SearchTerm = searchTerm }, ct);
    }

    public Task<List<KnowledgeEntry>> GetKnowledgeByCategoryAsync(KnowledgeCategory category, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetKnowledgeByCategoryQuery, List<KnowledgeEntry>>(
            new GetKnowledgeByCategoryQuery { Category = category }, ct);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Character-specific knowledge operations
    // ═══════════════════════════════════════════════════════════════════

    public Task<CommandResult> GrantKnowledgeAsync(CharacterId characterId, string entryId, CancellationToken ct = default)
    {
        return _commands.DispatchAsync(new UnlockLoreCommand
        {
            CharacterId = characterId,
            LoreId = entryId
        }, ct);
    }

    public Task<bool> HasKnowledgeAsync(CharacterId characterId, string entryId, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<HasKnowledgeQuery, bool>(
            new HasKnowledgeQuery { CharacterId = characterId, EntryId = entryId }, ct);
    }

    public Task<List<KnowledgeEntry>> GetCharacterKnowledgeAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetCharacterKnowledgeQuery, List<KnowledgeEntry>>(
            new GetCharacterKnowledgeQuery { CharacterId = characterId }, ct);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CRUD operations on knowledge entries (lore definitions)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<CommandResult> CreateKnowledgeEntryAsync(CreateKnowledgeEntryCommand command, CancellationToken ct = default)
    {
        CommandResult result = await _commands.DispatchAsync(new CreateLoreDefinitionCommand
        {
            Definition = new PersistedLoreDefinition
            {
                LoreId = command.EntryId,
                Title = command.Title,
                Content = command.Content,
                Category = MapToLoreCategory(command.Category),
                Tier = 0, // Default tier
                Keywords = command.Tags.Count > 0 ? string.Join(",", command.Tags) : null,
                IsAlwaysAvailable = false,
                CreatedUtc = DateTime.UtcNow
            }
        }, ct);

        // Preserve the historical response shape (EntryId key).
        if (result.Success)
            return CommandResult.OkWith("EntryId", command.EntryId);
        return result;
    }

    public async Task<CommandResult> UpdateKnowledgeEntryAsync(UpdateKnowledgeEntryCommand command, CancellationToken ct = default)
    {
        // The definition command is a full replace; fetch first so partial
        // updates preserve fields the DTO leaves unset (Tier, availability).
        PersistedLoreDefinition? existing = await _queries
            .DispatchAsync<GetLoreDefinitionQuery, PersistedLoreDefinition?>(
                new GetLoreDefinitionQuery { LoreId = command.EntryId }, ct);
        if (existing is null)
            return CommandResult.Fail($"Knowledge entry '{command.EntryId}' not found");

        if (command.Title != null) existing.Title = command.Title;
        if (command.Content != null) existing.Content = command.Content;
        if (command.Category.HasValue) existing.Category = MapToLoreCategory(command.Category.Value);
        if (command.Tags != null)
            existing.Keywords = command.Tags.Count > 0 ? string.Join(",", command.Tags) : null;

        return await _commands.DispatchAsync(new UpdateLoreDefinitionCommand
        {
            LoreId = command.EntryId,
            Definition = existing
        }, ct);
    }

    public Task<CommandResult> DeleteKnowledgeEntryAsync(string entryId, CancellationToken ct = default)
    {
        return _commands.DispatchAsync(new DeleteLoreDefinitionCommand { LoreId = entryId }, ct);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Quest Stage Management
    // ═══════════════════════════════════════════════════════════════════

    public Task<CommandResult> SetQuestStageAsync(
        CharacterId characterId,
        string questId,
        int stageId,
        CancellationToken ct = default)
    {
        return _commands.DispatchAsync(new SetQuestStageCommand
        {
            CharacterId = characterId,
            QuestId = questId,
            StageId = stageId
        }, ct);
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
}
