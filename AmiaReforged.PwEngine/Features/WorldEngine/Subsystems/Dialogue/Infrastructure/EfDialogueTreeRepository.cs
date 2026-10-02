using AmiaReforged.Shared.Dialogue;
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
        List<string> errors = tree.Validate();
        if (errors.Count > 0) throw new FormatException(string.Join("; ", errors));
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

    internal static DialogueTree ToDomain(PersistedDialogueTree entity) => DialogueTreeMapper.FromDto(new DialogueTreeDto
    {
        DialogueTreeId = entity.DialogueTreeId,
        Title = entity.Title,
        Description = entity.Description ?? string.Empty,
        RootNodeId = entity.RootNodeId,
        SpeakerTag = entity.SpeakerTag,
        Nodes = JsonSerializer.Deserialize<List<DialogueNodeDto>>(entity.NodesJson ?? "[]", JsonOpts) ?? [],
        CreatedUtc = entity.CreatedUtc,
        UpdatedUtc = entity.UpdatedUtc
    });

    private static PersistedDialogueTree FromDomain(DialogueTree tree) => new()
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

    private static string SerializeNodes(List<DialogueNode> nodes) => JsonSerializer.Serialize(
        nodes.Select(DialogueTreeMapper.ToDto).ToList(), JsonOpts);

    private static PwEngineContext CreateContext()
    {
        PwContextFactory factory = AnvilCore.GetService<PwContextFactory>()
                                   ?? throw new InvalidOperationException("PwContextFactory not available");
        return factory.CreateDbContext();
    }

}
