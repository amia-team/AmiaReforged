using System.Runtime.CompilerServices;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

/// <summary>
/// Keeps an editor draft across forced window closure for the current player session.
/// Weak player keys allow disconnected sessions to be collected.
/// </summary>
[ServiceBinding(typeof(CodexNoteDraftStore))]
public sealed class CodexNoteDraftStore
{
    private readonly ConditionalWeakTable<NwPlayer, Dictionary<CharacterId, CodexNoteDraft>> _drafts = new();

    internal CodexNoteDraft? Get(NwPlayer player, CharacterId characterId) =>
        _drafts.TryGetValue(player, out var drafts) ? drafts.GetValueOrDefault(characterId) : null;

    internal void Set(NwPlayer player, CharacterId characterId, CodexNoteDraft draft) =>
        _drafts.GetOrCreateValue(player)[characterId] = draft;

    internal void Remove(NwPlayer player, CharacterId characterId)
    {
        if (_drafts.TryGetValue(player, out var drafts)) drafts.Remove(characterId);
    }
}
