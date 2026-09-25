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

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;

[ServiceBinding(typeof(IDynamicQuestRepository))]
public sealed class EfDynamicQuestRepository : IDynamicQuestRepository
{
    private readonly PwContextFactory _contextFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new ObjectiveIdJsonConverter()
        }
    };

    public EfDynamicQuestRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DynamicQuestTemplate?> GetTemplateAsync(
        TemplateId templateId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        PersistedDynamicQuestTemplate? row = await context.DynamicQuestTemplates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.TemplateId == templateId.Value,
                cancellationToken);

        return row == null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<DynamicQuestTemplate>> GetActiveTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        List<PersistedDynamicQuestTemplate> rows = await context.DynamicQuestTemplates
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<DynamicQuestTemplate>> GetActiveTemplatesBySourceAsync(
        DynamicQuestSource source,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        int sourceValue = (int)source;

        List<PersistedDynamicQuestTemplate> rows = await context.DynamicQuestTemplates
            .AsNoTracking()
            .Where(x => x.IsActive && x.Source == sourceValue)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task SaveTemplateAsync(
        DynamicQuestTemplate template,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        PersistedDynamicQuestTemplate? row = await context.DynamicQuestTemplates
            .SingleOrDefaultAsync(
                x => x.TemplateId == template.TemplateId.Value,
                cancellationToken);

        string payloadJson = SerializeTemplate(template);

        if (row == null)
        {
            row = new PersistedDynamicQuestTemplate
            {
                TemplateId = template.TemplateId.Value,
                Source = (int)template.Source,
                IsActive = template.IsActive,
                CreatedAt = template.CreatedAt,
                PayloadJson = payloadJson
            };

            context.DynamicQuestTemplates.Add(row);
        }
        else
        {
            row.Source = (int)template.Source;
            row.IsActive = template.IsActive;
            row.CreatedAt = template.CreatedAt;
            row.PayloadJson = payloadJson;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DynamicQuestPosting?> GetPostingAsync(
        PostingId postingId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        PersistedDynamicQuestPosting? row = await context.DynamicQuestPostings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.PostingId == postingId.Value,
                cancellationToken);

        return row == null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<DynamicQuestPosting>> GetActivePostingsAsync(
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        List<PersistedDynamicQuestPosting> rows = await context.DynamicQuestPostings
            .AsNoTracking()
            .Where(x => x.ExpiresAt == null || x.ExpiresAt > now)
            .OrderBy(x => x.PostedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<DynamicQuestPosting>> GetActivePostingsForSourceAsync(
        DynamicQuestSource source,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        int sourceValue = (int)source;

        List<PersistedDynamicQuestPosting> rows =
            await (
                from posting in context.DynamicQuestPostings.AsNoTracking()
                join template in context.DynamicQuestTemplates.AsNoTracking()
                    on posting.SourceTemplateId equals template.TemplateId
                where template.Source == sourceValue
                      && (posting.ExpiresAt == null || posting.ExpiresAt > now)
                orderby posting.PostedAt
                select posting
            ).ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task SavePostingAsync(
        DynamicQuestPosting posting,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(posting);

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        PersistedDynamicQuestPosting? row = await context.DynamicQuestPostings
            .SingleOrDefaultAsync(
                x => x.PostingId == posting.PostingId.Value,
                cancellationToken);

        string payloadJson = SerializePosting(posting);

        if (row == null)
        {
            row = new PersistedDynamicQuestPosting
            {
                PostingId = posting.PostingId.Value,
                SourceTemplateId = posting.SourceTemplateId.Value,
                PostedAt = posting.PostedAt,
                ExpiresAt = posting.ExpiresAt,
                PayloadJson = payloadJson
            };

            context.DynamicQuestPostings.Add(row);
        }
        else
        {
            row.SourceTemplateId = posting.SourceTemplateId.Value;
            row.PostedAt = posting.PostedAt;
            row.ExpiresAt = posting.ExpiresAt;
            row.PayloadJson = payloadJson;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveExpiredPostingsAsync(
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        await context.DynamicQuestPostings
            .Where(x => x.ExpiresAt != null && x.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> GetCompletionCountAsync(
        CharacterId characterId,
        TemplateId templateId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        return await context.DynamicQuestCompletions
            .AsNoTracking()
            .Where(x =>
                x.CharacterId == characterId.Value &&
                x.TemplateId == templateId.Value)
            .Select(x => x.CompletionCount)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLastCompletionTimeAsync(
        CharacterId characterId,
        TemplateId templateId,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        return await context.DynamicQuestCompletions
            .AsNoTracking()
            .Where(x =>
                x.CharacterId == characterId.Value &&
                x.TemplateId == templateId.Value)
            .Select(x => (DateTime?)x.LastCompletedAt)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RecordCompletionAsync(
        CharacterId characterId,
        TemplateId templateId,
        DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext context = _contextFactory.CreateDbContext();

        PersistedDynamicQuestCompletion? row =
            await context.DynamicQuestCompletions
                .SingleOrDefaultAsync(
                    x =>
                        x.CharacterId == characterId.Value &&
                        x.TemplateId == templateId.Value,
                    cancellationToken);

        if (row == null)
        {
            context.DynamicQuestCompletions.Add(
                new PersistedDynamicQuestCompletion
                {
                    CharacterId = characterId.Value,
                    TemplateId = templateId.Value,
                    CompletionCount = 1,
                    LastCompletedAt = completedAt
                });
        }
        else
        {
            row.CompletionCount++;

            if (completedAt > row.LastCompletedAt)
                row.LastCompletedAt = completedAt;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string SerializeTemplate(DynamicQuestTemplate template)
    {
        TemplatePayload payload = new()
        {
            Title = template.Title,
            Description = template.Description,
            ClaimMode = template.ClaimMode,
            MaxClaimants = template.MaxClaimants,
            TimeLimit = template.TimeLimit,
            ExpiryBehavior = template.ExpiryBehavior,
            MaxCompletionsPerCharacter = template.MaxCompletionsPerCharacter,
            CooldownAfterCompletion = template.CooldownAfterCompletion,
            PostingDuration = template.PostingDuration,
            BaseReward = template.BaseReward,
            StageTemplates = template.StageTemplates,
            Keywords = template.Keywords.Select(x => x.Value).ToList()
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static DynamicQuestTemplate ToDomain(
        PersistedDynamicQuestTemplate row)
    {
        TemplatePayload payload =
            JsonSerializer.Deserialize<TemplatePayload>(
                row.PayloadJson,
                JsonOptions)
            ?? throw new InvalidOperationException(
                $"Could not deserialize dynamic quest template {row.TemplateId}");

        DynamicQuestTemplate template = new()
        {
            TemplateId = new TemplateId(row.TemplateId),
            Title = payload.Title,
            Description = payload.Description,
            Source = (DynamicQuestSource)row.Source,
            ClaimMode = payload.ClaimMode,
            MaxClaimants = payload.MaxClaimants,
            TimeLimit = payload.TimeLimit,
            ExpiryBehavior = payload.ExpiryBehavior,
            MaxCompletionsPerCharacter = payload.MaxCompletionsPerCharacter,
            CooldownAfterCompletion = payload.CooldownAfterCompletion,
            PostingDuration = payload.PostingDuration,
            BaseReward = payload.BaseReward,
            StageTemplates = payload.StageTemplates,
            Keywords = payload.Keywords
                .Select(x => new Keyword(x))
                .ToList(),
            CreatedAt = row.CreatedAt
        };

        if (!row.IsActive)
            template.Deactivate();

        return template;
    }

    private static string SerializePosting(DynamicQuestPosting posting)
    {
        PostingPayload payload = new()
        {
            Title = posting.Title,
            Description = posting.Description,
            ClaimMode = posting.ClaimMode,
            MaxClaimants = posting.MaxClaimants,
            TimeLimit = posting.TimeLimit,
            ExpiryBehavior = posting.ExpiryBehavior,
            BaseReward = posting.BaseReward,
            StageTemplates = posting.StageTemplates,
            Keywords = posting.Keywords.Select(x => x.Value).ToList(),
            Claims = posting.Claims
                .Select(claim => new ClaimPayload
                {
                    ClaimantId = claim.ClaimantId.Value,
                    ClaimedAt = claim.ClaimedAt,
                    SharedWith = claim.SharedWith
                        .Select(x => x.Value)
                        .ToList()
                })
                .ToList()
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static DynamicQuestPosting ToDomain(
        PersistedDynamicQuestPosting row)
    {
        PostingPayload payload =
            JsonSerializer.Deserialize<PostingPayload>(
                row.PayloadJson,
                JsonOptions)
            ?? throw new InvalidOperationException(
                $"Could not deserialize dynamic quest posting {row.PostingId}");

        DynamicQuestPosting posting = new()
        {
            PostingId = new PostingId(row.PostingId),
            SourceTemplateId = new TemplateId(row.SourceTemplateId),
            Title = payload.Title,
            Description = payload.Description,
            PostedAt = row.PostedAt,
            ExpiresAt = row.ExpiresAt,
            ClaimMode = payload.ClaimMode,
            MaxClaimants = payload.MaxClaimants,
            TimeLimit = payload.TimeLimit,
            ExpiryBehavior = payload.ExpiryBehavior,
            BaseReward = payload.BaseReward,
            StageTemplates = payload.StageTemplates,
            Keywords = payload.Keywords
                .Select(x => new Keyword(x))
                .ToList()
        };

        foreach (ClaimPayload claim in payload.Claims)
        {
            posting.RestoreClaim(new ClaimSlot
            {
                ClaimantId = new CharacterId(claim.ClaimantId),
                ClaimedAt = claim.ClaimedAt,
                SharedWith = claim.SharedWith
                    .Select(x => new CharacterId(x))
                    .ToList()
            });
        }

        return posting;
    }

    private sealed class TemplatePayload
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public ClaimMode ClaimMode { get; init; }
        public int MaxClaimants { get; init; }
        public TimeSpan? TimeLimit { get; init; }
        public ExpiryBehavior ExpiryBehavior { get; init; }
        public int MaxCompletionsPerCharacter { get; init; }
        public TimeSpan? CooldownAfterCompletion { get; init; }
        public TimeSpan? PostingDuration { get; init; }
        public RewardMix BaseReward { get; init; } = RewardMix.Empty;
        public List<QuestStage> StageTemplates { get; init; } = [];
        public List<string> Keywords { get; init; } = [];
    }

    private sealed class PostingPayload
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public ClaimMode ClaimMode { get; init; }
        public int MaxClaimants { get; init; }
        public TimeSpan? TimeLimit { get; init; }
        public ExpiryBehavior ExpiryBehavior { get; init; }
        public RewardMix BaseReward { get; init; } = RewardMix.Empty;
        public List<QuestStage> StageTemplates { get; init; } = [];
        public List<string> Keywords { get; init; } = [];
        public List<ClaimPayload> Claims { get; init; } = [];
    }

    private sealed class ClaimPayload
    {
        public Guid ClaimantId { get; init; }
        public DateTime ClaimedAt { get; init; }
        public List<Guid> SharedWith { get; init; } = [];
    }
}
