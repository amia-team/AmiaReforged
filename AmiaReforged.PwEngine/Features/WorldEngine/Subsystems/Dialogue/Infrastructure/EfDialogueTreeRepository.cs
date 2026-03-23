using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Infrastructure;

/// <summary>
/// EF Core implementation of <see cref="IDialogueTreeRepository"/>.
/// Handles mapping between the domain <see cref="DialogueTree"/> and persisted <see cref="PersistedDialogueTree"/>.
/// </summary>
[ServiceBinding(typeof(IDialogueTreeRepository))]
public sealed class EfDialogueTreeRepository : IDialogueTreeRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<DialogueTree?> GetByIdAsync(DialogueTreeId id, CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        PersistedDialogueTree? entity = await context.DialogueTrees.FindAsync([id.Value], ct);
        return entity == null ? null : ToDomain(entity);
    }

    public async Task<List<DialogueTree>> GetAllAsync(CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        List<PersistedDialogueTree> entities = await context.DialogueTrees
            .OrderBy(d => d.Title)
            .ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<List<DialogueTree>> GetBySpeakerTagAsync(string speakerTag, CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        List<PersistedDialogueTree> entities = await context.DialogueTrees
            .Where(d => d.SpeakerTag == speakerTag)
            .OrderBy(d => d.Title)
            .ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public async Task SaveAsync(DialogueTree tree, CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        PersistedDialogueTree? existing = await context.DialogueTrees.FindAsync([tree.Id.Value], ct);

        if (existing == null)
        {
            PersistedDialogueTree entity = FromDomain(tree);
            entity.CreatedUtc = DateTime.UtcNow;
            context.DialogueTrees.Add(entity);
        }
        else
        {
            existing.Title = tree.Title;
            existing.Description = tree.Description;
            existing.RootNodeId = tree.RootNodeId.Value.ToString();
            existing.SpeakerTag = tree.SpeakerTag;
            existing.NodesJson = SerializeNodes(tree.Nodes);
            existing.UpdatedUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(DialogueTreeId id, CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        PersistedDialogueTree? entity = await context.DialogueTrees.FindAsync([id.Value], ct);
        if (entity == null) return false;

        context.DialogueTrees.Remove(entity);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<DialogueTree>> SearchAsync(string searchTerm, int page = 1, int pageSize = 50,
        CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        string term = searchTerm.Trim().ToLower();

        List<PersistedDialogueTree> entities = await context.DialogueTrees
            .Where(d => d.DialogueTreeId.ToLower().Contains(term) ||
                        d.Title.ToLower().Contains(term))
            .OrderBy(d => d.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<int> CountAsync(string? searchTerm = null, CancellationToken ct = default)
    {
        using PwEngineContext context = CreateContext();
        IQueryable<PersistedDialogueTree> query = context.DialogueTrees;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(d => d.DialogueTreeId.ToLower().Contains(term) ||
                                     d.Title.ToLower().Contains(term));
        }

        return await query.CountAsync(ct);
    }

    // ══════════════════════════════════════════════════
    //  Mapping
    // ══════════════════════════════════════════════════

    private static DialogueTree ToDomain(PersistedDialogueTree entity)
    {
        List<DialogueNode> nodes = DeserializeNodes(entity.NodesJson);
        Guid rootGuid = Guid.TryParse(entity.RootNodeId, out Guid g) ? g : Guid.Empty;

        return new DialogueTree
        {
            Id = new DialogueTreeId(entity.DialogueTreeId),
            Title = entity.Title,
            Description = entity.Description,
            RootNodeId = rootGuid != Guid.Empty ? new DialogueNodeId(rootGuid) : default,
            SpeakerTag = entity.SpeakerTag,
            Nodes = nodes,
            CreatedUtc = entity.CreatedUtc,
            UpdatedUtc = entity.UpdatedUtc
        };
    }

    private static PersistedDialogueTree FromDomain(DialogueTree tree)
    {
        return new PersistedDialogueTree
        {
            DialogueTreeId = tree.Id.Value,
            Title = tree.Title,
            Description = tree.Description,
            RootNodeId = tree.RootNodeId.Value.ToString(),
            SpeakerTag = tree.SpeakerTag,
            NodesJson = SerializeNodes(tree.Nodes),
            CreatedUtc = tree.CreatedUtc,
            UpdatedUtc = tree.UpdatedUtc
        };
    }

    private static string SerializeNodes(List<DialogueNode> nodes)
    {
        List<NodeJsonModel> models = nodes.Select(n => new NodeJsonModel
        {
            Id = n.Id.Value,
            Type = n.Type,
            SpeakerTag = n.SpeakerTag,
            Text = n.Text,
            SortOrder = n.SortOrder,
            ParentNodeId = n.ParentNodeId?.Value,
            Choices = n.Choices.Select(c => new ChoiceJsonModel
            {
                TargetNodeId = c.TargetNodeId.Value,
                ResponseText = c.ResponseText,
                SortOrder = c.SortOrder,
                Conditions = c.Conditions.Select(cond => new ConditionJsonModel
                {
                    Type = cond.Type,
                    Parameters = cond.Parameters
                }).ToList()
            }).ToList(),
            Actions = n.Actions.Select(a => new ActionJsonModel
            {
                ActionType = a.ActionType,
                Parameters = a.Parameters,
                ExecutionOrder = a.ExecutionOrder
            }).ToList()
        }).ToList();

        return JsonSerializer.Serialize(models, JsonOpts);
    }

    private static List<DialogueNode> DeserializeNodes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json is "[]" or "null")
            return [];

        try
        {
            List<NodeJsonModel>? models = JsonSerializer.Deserialize<List<NodeJsonModel>>(json, JsonOpts);
            if (models == null) return [];

            return models.Select(m => new DialogueNode
            {
                Id = new DialogueNodeId(m.Id),
                Type = m.Type,
                SpeakerTag = m.SpeakerTag,
                Text = m.Text ?? string.Empty,
                SortOrder = m.SortOrder,
                ParentNodeId = m.ParentNodeId.HasValue ? new DialogueNodeId(m.ParentNodeId.Value) : null,
                Choices = m.Choices.Select(c => new DialogueChoice
                {
                    TargetNodeId = new DialogueNodeId(c.TargetNodeId),
                    ResponseText = c.ResponseText ?? string.Empty,
                    SortOrder = c.SortOrder,
                    Conditions = c.Conditions.Select(cond => new DialogueCondition
                    {
                        Type = cond.Type,
                        Parameters = cond.Parameters ?? new Dictionary<string, string>()
                    }).ToList()
                }).ToList(),
                Actions = m.Actions.Select(a => new DialogueAction
                {
                    ActionType = a.ActionType,
                    Parameters = a.Parameters ?? new Dictionary<string, string>(),
                    ExecutionOrder = a.ExecutionOrder
                }).ToList()
            }).ToList();
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to deserialize dialogue nodes JSON");
            return [];
        }
    }

    private static PwEngineContext CreateContext()
    {
        PwContextFactory factory = AnvilCore.GetService<PwContextFactory>()
                                   ?? throw new InvalidOperationException("PwContextFactory not available");
        return factory.CreateDbContext();
    }

    // ══════════════════════════════════════════════════
    //  JSON Models
    // ══════════════════════════════════════════════════

    private sealed record NodeJsonModel
    {
        public Guid Id { get; init; }
        public DialogueNodeType Type { get; init; }
        public string? SpeakerTag { get; init; }
        public string? Text { get; init; }
        public int SortOrder { get; init; }
        public Guid? ParentNodeId { get; init; }
        public List<ChoiceJsonModel> Choices { get; init; } = [];
        public List<ActionJsonModel> Actions { get; init; } = [];
    }

    private sealed record ChoiceJsonModel
    {
        public Guid TargetNodeId { get; init; }
        public string? ResponseText { get; init; }
        public int SortOrder { get; init; }
        public List<ConditionJsonModel> Conditions { get; init; } = [];
    }

    private sealed record ConditionJsonModel
    {
        public DialogueConditionType Type { get; init; }
        public Dictionary<string, string>? Parameters { get; init; }
    }

    private sealed record ActionJsonModel
    {
        public DialogueActionType ActionType { get; init; }
        public Dictionary<string, string>? Parameters { get; init; }
        public int ExecutionOrder { get; init; }
    }
}
