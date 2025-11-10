using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Stub implementation of the Codex subsystem.
/// TODO: Wire up to existing Codex Application layer handlers.
/// </summary>
[ServiceBinding(typeof(ICodexSubsystem))]
public sealed class CodexSubsystem : ICodexSubsystem
{
    public Task<KnowledgeEntry?> GetKnowledgeEntryAsync(string entryId, CancellationToken ct = default)
    {
        return Task.FromResult<KnowledgeEntry?>(null);
    }

    public Task<List<KnowledgeEntry>> SearchKnowledgeAsync(string searchTerm, CancellationToken ct = default)
    {
        return Task.FromResult(new List<KnowledgeEntry>());
    }

    public Task<List<KnowledgeEntry>> GetKnowledgeByCategoryAsync(KnowledgeCategory category, CancellationToken ct = default)
    {
        return Task.FromResult(new List<KnowledgeEntry>());
    }

    public Task<CommandResult> GrantKnowledgeAsync(CharacterId characterId, string entryId, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<bool> HasKnowledgeAsync(CharacterId characterId, string entryId, CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    public Task<List<KnowledgeEntry>> GetCharacterKnowledgeAsync(CharacterId characterId, CancellationToken ct = default)
    {
        return Task.FromResult(new List<KnowledgeEntry>());
    }

    public Task<CommandResult> CreateKnowledgeEntryAsync(CreateKnowledgeEntryCommand command, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> UpdateKnowledgeEntryAsync(UpdateKnowledgeEntryCommand command, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<CommandResult> DeleteKnowledgeEntryAsync(string entryId, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }
}

