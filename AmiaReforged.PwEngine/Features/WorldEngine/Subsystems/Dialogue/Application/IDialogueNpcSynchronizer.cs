using System.Threading;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

/// <summary>
/// Application-facing boundary for synchronizing dialogue definitions to NPCs.
///
/// Exposes only the create / update / delete synchronization operations that event
/// subscribers need. It deliberately does <em>not</em> expose NWN runtime concerns such as
/// conversation-event handling, registry inspection, startup discovery, module-load
/// initialization, or direct <c>NwCreature</c> manipulation. This lets the event layer depend
/// on stable identifiers (tree ID, speaker tag) instead of the concrete NWN hook.
/// </summary>
public interface IDialogueNpcSynchronizer
{
    /// <summary>
    /// Finds all creatures with the given <paramref name="speakerTag"/>, stamps the dialogue
    /// tree local variable, and registers the conversation hook.
    /// </summary>
    /// <param name="speakerTag">The NPC creature tag to match.</param>
    /// <param name="dialogueTreeId">The dialogue tree ID to assign.</param>
    /// <returns>Number of NPCs registered.</returns>
    Task<int> RegisterAsync(string speakerTag, string dialogueTreeId, CancellationToken ct = default);

    /// <summary>
    /// Reassigns the speaker tag for a dialogue tree. Unregisters the old tag (tree-aware,
    /// only affecting NPCs still owned by this tree) and registers the new tag.
    /// </summary>
    /// <param name="dialogueTreeId">The dialogue tree being updated.</param>
    /// <param name="newSpeakerTag">The new speaker tag (may be null to clear).</param>
    /// <returns>Tuple of (NPCs unregistered from old tag, NPCs registered on new tag).</returns>
    Task<(int unregistered, int registered)> UpdateAsync(
        string dialogueTreeId, string? newSpeakerTag, CancellationToken ct = default);

    /// <summary>
    /// Unregisters NPCs for a specific dialogue tree. Only clears the local variable /
    /// unhooks creatures whose <c>we_dialogue_tree</c> value still matches the tree, so NPCs
    /// owned by a different tree are left untouched.
    /// </summary>
    /// <param name="dialogueTreeId">The dialogue tree ID to unregister.</param>
    /// <returns>Number of NPCs unregistered.</returns>
    Task<int> UnregisterAsync(string dialogueTreeId, CancellationToken ct = default);
}
