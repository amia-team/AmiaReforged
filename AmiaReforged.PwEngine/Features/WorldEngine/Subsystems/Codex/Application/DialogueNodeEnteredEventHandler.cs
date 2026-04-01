using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Handles <see cref="DialogueNodeEnteredEvent"/> by routing it as a quest signal
/// through the objective resolution service. This is how "speak to NPC" objectives
/// complete — when the conversation enters a node whose truncated ID matches the
/// objective definition's TargetTag.
/// </summary>
[ServiceBinding(typeof(IEventHandler<DialogueNodeEnteredEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class DialogueNodeEnteredEventHandler
    : IEventHandler<DialogueNodeEnteredEvent>, IEventHandlerMarker
{
    private readonly QuestObjectiveResolutionService _resolutionService;

    public DialogueNodeEnteredEventHandler(QuestObjectiveResolutionService resolutionService)
    {
        _resolutionService = resolutionService;
    }

    public Task HandleAsync(DialogueNodeEnteredEvent @event, CancellationToken cancellationToken = default)
    {
        CharacterId characterId = CharacterId.From(@event.CharacterId);
        _resolutionService.ProcessDialogueNodeEntered(characterId, @event.NodeId);
        return Task.CompletedTask;
    }
}
