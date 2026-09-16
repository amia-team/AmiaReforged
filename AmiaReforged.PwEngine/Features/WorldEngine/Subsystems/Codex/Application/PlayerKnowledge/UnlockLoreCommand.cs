using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.PlayerKnowledge;

/// <summary>
/// Unlocks a lore definition for a character (player knowledge).
/// Fails if the definition is unknown or already unlocked.
/// Replaces the inline-EF <c>CodexSubsystem.GrantKnowledgeAsync</c> path (F-6 audit).
/// </summary>
public record UnlockLoreCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required string LoreId { get; init; }
    public string Source { get; init; } = "Admin Grant";
}

[ServiceBinding(typeof(ICommandHandler<UnlockLoreCommand>))]
public sealed class UnlockLoreHandler : ICommandHandler<UnlockLoreCommand>
{
    private readonly PwContextFactory _contextFactory;
    private readonly IEventBus _eventBus;

    public UnlockLoreHandler(PwContextFactory contextFactory, IEventBus eventBus)
    {
        _contextFactory = contextFactory;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(UnlockLoreCommand command, CancellationToken cancellationToken = default)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedLoreDefinition? def = await ctx.CodexLoreDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.LoreId == command.LoreId, cancellationToken);
        if (def is null)
            return CommandResult.Fail($"Knowledge entry '{command.LoreId}' does not exist");

        bool alreadyUnlocked = await ctx.CodexLoreUnlocks
            .AnyAsync(u => u.CharacterId == command.CharacterId.Value && u.LoreId == command.LoreId, cancellationToken);
        if (alreadyUnlocked)
            return CommandResult.Fail($"Character already has knowledge '{command.LoreId}'");

        DateTime now = DateTime.UtcNow;
        ctx.CodexLoreUnlocks.Add(new PersistedLoreUnlock
        {
            CharacterId = command.CharacterId.Value,
            LoreId = command.LoreId,
            DateDiscovered = now,
            DiscoverySource = command.Source
        });
        await ctx.SaveChangesAsync(cancellationToken);

        LoreDiscoveredEvent evt = new(
            command.CharacterId,
            now,
            (LoreId)command.LoreId,
            def.Title,
            def.Content,
            command.Source,
            def.Category,
            (LoreTier)def.Tier,
            ParseKeywords(def.Keywords));
        await _eventBus.PublishAsync(evt, cancellationToken);

        return CommandResult.Ok();
    }

    private static List<Keyword> ParseKeywords(string? keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords)) return [];
        return keywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(k => new Keyword(k))
            .ToList();
    }
}
