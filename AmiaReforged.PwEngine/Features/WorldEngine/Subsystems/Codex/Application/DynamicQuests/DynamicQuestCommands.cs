using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.DynamicQuests;

/// <summary>
/// Creates a dynamic quest posting from a template and makes it available.
/// Fails if the template is unknown or inactive. (F-6 audit: CQRS envelope over
/// <see cref="DynamicQuestService.PostQuestAsync"/>.)
/// </summary>
public record PostDynamicQuestCommand : ICommand
{
    public required Guid TemplateId { get; init; }
    public required CharacterId PostedBy { get; init; }
}

/// <summary>
/// Claims a dynamic quest posting for a character (cooldown, max-completion and
/// slot checks apply). Fails when the claim is rejected. (F-6 audit.)
/// </summary>
public record ClaimDynamicQuestCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required Guid PostingId { get; init; }
}

/// <summary>
/// Shares a claimant's dynamic quest with a party member for co-op play.
/// Fails when the share is rejected. (F-6 audit.)
/// </summary>
public record ShareDynamicQuestCommand : ICommand
{
    public required CharacterId ClaimantId { get; init; }
    public required CharacterId InviteeId { get; init; }
    public required Guid PostingId { get; init; }
    public required string QuestId { get; init; }
}

/// <summary>
/// Releases a character's claim on a dynamic quest posting. Fails when the
/// unclaim is rejected. (F-6 audit.)
/// </summary>
public record UnclaimDynamicQuestCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required Guid PostingId { get; init; }
    public required string QuestId { get; init; }
}

/// <summary>
/// Processes deadline expirations for all active dynamic quest sessions and
/// removes expired postings. System-initiated (heartbeat/timer): carries no
/// actor identity. (F-6 audit.)
/// </summary>
public record ExpireDynamicQuestsCommand : ICommand;

[ServiceBinding(typeof(ICommandHandler<PostDynamicQuestCommand>))]
public sealed class PostDynamicQuestHandler : ICommandHandler<PostDynamicQuestCommand>
{
    private readonly DynamicQuestService _quests;

    public PostDynamicQuestHandler(DynamicQuestService quests)
    {
        _quests = quests;
    }

    public async Task<CommandResult> HandleAsync(PostDynamicQuestCommand command, CancellationToken cancellationToken = default)
    {
        DynamicQuestPosting? posting;
        try
        {
            posting = await _quests.PostQuestAsync((TemplateId)command.TemplateId, command.PostedBy, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return CommandResult.Fail(ex.Message);
        }

        if (posting is null)
            return CommandResult.Fail($"Template '{command.TemplateId}' not found or inactive");

        return CommandResult.OkWith("postingId", posting.PostingId.Value.ToString());
    }
}

[ServiceBinding(typeof(ICommandHandler<ClaimDynamicQuestCommand>))]
public sealed class ClaimDynamicQuestHandler : ICommandHandler<ClaimDynamicQuestCommand>
{
    private readonly DynamicQuestService _quests;

    public ClaimDynamicQuestHandler(DynamicQuestService quests)
    {
        _quests = quests;
    }

    public async Task<CommandResult> HandleAsync(ClaimDynamicQuestCommand command, CancellationToken cancellationToken = default)
    {
        QuestId? questId = await _quests.ClaimQuestAsync(
            command.CharacterId, (PostingId)command.PostingId, cancellationToken);

        if (questId is null)
            return CommandResult.Fail(
                $"Claim rejected for posting '{command.PostingId}' (expired, full, cooldown, or already claimed)");

        return CommandResult.OkWith("questId", questId.Value);
    }
}

[ServiceBinding(typeof(ICommandHandler<ShareDynamicQuestCommand>))]
public sealed class ShareDynamicQuestHandler : ICommandHandler<ShareDynamicQuestCommand>
{
    private readonly DynamicQuestService _quests;

    public ShareDynamicQuestHandler(DynamicQuestService quests)
    {
        _quests = quests;
    }

    public async Task<CommandResult> HandleAsync(ShareDynamicQuestCommand command, CancellationToken cancellationToken = default)
    {
        bool shared = await _quests.ShareQuestAsync(
            command.ClaimantId, command.InviteeId,
            (PostingId)command.PostingId, (QuestId)command.QuestId, cancellationToken);

        if (!shared)
            return CommandResult.Fail($"Share rejected for posting '{command.PostingId}'");

        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<UnclaimDynamicQuestCommand>))]
public sealed class UnclaimDynamicQuestHandler : ICommandHandler<UnclaimDynamicQuestCommand>
{
    private readonly DynamicQuestService _quests;

    public UnclaimDynamicQuestHandler(DynamicQuestService quests)
    {
        _quests = quests;
    }

    public async Task<CommandResult> HandleAsync(UnclaimDynamicQuestCommand command, CancellationToken cancellationToken = default)
    {
        bool unclaimed = await _quests.UnclaimQuestAsync(
            command.CharacterId, (PostingId)command.PostingId, (QuestId)command.QuestId, cancellationToken);

        if (!unclaimed)
            return CommandResult.Fail($"Unclaim rejected for posting '{command.PostingId}'");

        return CommandResult.Ok();
    }
}

[ServiceBinding(typeof(ICommandHandler<ExpireDynamicQuestsCommand>))]
public sealed class ExpireDynamicQuestsHandler : ICommandHandler<ExpireDynamicQuestsCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly DynamicQuestService _quests;

    public ExpireDynamicQuestsHandler(DynamicQuestService quests)
    {
        _quests = quests;
    }

    public async Task<CommandResult> HandleAsync(ExpireDynamicQuestsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            await _quests.TickExpirationsAsync(cancellationToken);
            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Dynamic quest expiration tick failed");
            return CommandResult.Fail($"Expiration tick failed: {ex.Message}");
        }
    }
}
