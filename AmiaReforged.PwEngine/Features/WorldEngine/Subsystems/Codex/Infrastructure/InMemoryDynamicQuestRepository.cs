using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;

/// <summary>
/// In-memory implementation of <see cref="IDynamicQuestRepository"/> for unit testing.
/// </summary>
public class InMemoryDynamicQuestRepository : IDynamicQuestRepository
{
    private readonly ConcurrentDictionary<TemplateId, DynamicQuestTemplate> _templates = new();
    private readonly ConcurrentDictionary<PostingId, DynamicQuestPosting> _postings = new();
    private readonly ConcurrentDictionary<(CharacterId, TemplateId), List<DateTime>> _completions = new();

    #region Template Operations

    public Task<DynamicQuestTemplate?> GetTemplateAsync(TemplateId templateId, CancellationToken cancellationToken = default)
        => Task.FromResult(_templates.GetValueOrDefault(templateId));

    public Task<IReadOnlyList<DynamicQuestTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DynamicQuestTemplate> result = _templates.Values
            .Where(t => t.IsActive)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DynamicQuestTemplate>> GetActiveTemplatesBySourceAsync(
        DynamicQuestSource source, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DynamicQuestTemplate> result = _templates.Values
            .Where(t => t.IsActive && t.Source == source)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveTemplateAsync(DynamicQuestTemplate template, CancellationToken cancellationToken = default)
    {
        _templates[template.TemplateId] = template;
        return Task.CompletedTask;
    }

    #endregion

    #region Posting Operations

    public Task<DynamicQuestPosting?> GetPostingAsync(PostingId postingId, CancellationToken cancellationToken = default)
        => Task.FromResult(_postings.GetValueOrDefault(postingId));

    public Task<IReadOnlyList<DynamicQuestPosting>> GetActivePostingsAsync(
        DateTime now, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DynamicQuestPosting> result = _postings.Values
            .Where(p => !p.IsPostingExpired(now))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DynamicQuestPosting>> GetActivePostingsForSourceAsync(
        DynamicQuestSource source, DateTime now, CancellationToken cancellationToken = default)
    {
        HashSet<TemplateId> sourceTemplateIds = _templates.Values
            .Where(t => t.Source == source)
            .Select(t => t.TemplateId)
            .ToHashSet();

        IReadOnlyList<DynamicQuestPosting> result = _postings.Values
            .Where(p => !p.IsPostingExpired(now) && sourceTemplateIds.Contains(p.SourceTemplateId))
            .ToList();
        return Task.FromResult(result);
    }

    public Task SavePostingAsync(DynamicQuestPosting posting, CancellationToken cancellationToken = default)
    {
        _postings[posting.PostingId] = posting;
        return Task.CompletedTask;
    }

    public Task RemoveExpiredPostingsAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        List<PostingId> expired = _postings.Values
            .Where(p => p.IsPostingExpired(now))
            .Select(p => p.PostingId)
            .ToList();

        foreach (PostingId id in expired)
            _postings.TryRemove(id, out _);

        return Task.CompletedTask;
    }

    #endregion

    #region Character Completion Tracking

    public Task<int> GetCompletionCountAsync(
        CharacterId characterId, TemplateId templateId, CancellationToken cancellationToken = default)
    {
        int count = _completions.TryGetValue((characterId, templateId), out List<DateTime>? times)
            ? times.Count
            : 0;
        return Task.FromResult(count);
    }

    public Task<DateTime?> GetLastCompletionTimeAsync(
        CharacterId characterId, TemplateId templateId, CancellationToken cancellationToken = default)
    {
        DateTime? last = _completions.TryGetValue((characterId, templateId), out List<DateTime>? times) && times.Count > 0
            ? times.Max()
            : null;
        return Task.FromResult(last);
    }

    public Task RecordCompletionAsync(
        CharacterId characterId, TemplateId templateId, DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        List<DateTime> times = _completions.GetOrAdd((characterId, templateId), _ => []);
        times.Add(completedAt);
        return Task.CompletedTask;
    }

    #endregion

    #region Test Helpers

    /// <summary>Removes all data. For testing only.</summary>
    public void Clear()
    {
        _templates.Clear();
        _postings.Clear();
        _completions.Clear();
    }

    /// <summary>Total template count.</summary>
    public int TemplateCount => _templates.Count;

    /// <summary>Total posting count.</summary>
    public int PostingCount => _postings.Count;

    #endregion
}
