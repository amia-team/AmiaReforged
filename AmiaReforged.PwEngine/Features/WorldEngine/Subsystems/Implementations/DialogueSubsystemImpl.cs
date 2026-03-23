using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Implementation of <see cref="IDialogueSubsystem"/> that delegates to <see cref="DialogueService"/>.
/// </summary>
[ServiceBinding(typeof(IDialogueSubsystem))]
public sealed class DialogueSubsystemImpl : IDialogueSubsystem
{
    [Inject] private Lazy<DialogueService>? DialogueService { get; init; }

    public async Task<bool> StartDialogueAsync(NwPlayer player, NwCreature npc, DialogueTreeId treeId,
        Guid characterId)
    {
        if (DialogueService?.Value == null) return false;
        return await DialogueService.Value.StartDialogueAsync(player, npc, treeId, characterId);
    }

    public void EndDialogue(NwPlayer player, string reason = "goodbye")
    {
        DialogueService?.Value?.EndDialogue(player, reason);
    }

    public async Task<List<DialogueTree>> GetDialoguesForNpcAsync(string npcTag)
    {
        if (DialogueService?.Value == null) return [];
        return await DialogueService.Value.GetDialoguesForNpcAsync(npcTag);
    }

    public bool HasActiveDialogue(NwPlayer player)
    {
        return DialogueService?.Value?.HasActiveSession(player) ?? false;
    }
}
