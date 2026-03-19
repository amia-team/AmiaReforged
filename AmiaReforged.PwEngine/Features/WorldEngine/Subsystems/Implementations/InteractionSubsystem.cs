using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Concrete implementation of <see cref="IInteractionSubsystem"/>.
/// Delegates to the command dispatcher and session manager.
/// </summary>
[ServiceBinding(typeof(IInteractionSubsystem))]
public sealed class InteractionSubsystem(
    ICommandDispatcher commandDispatcher,
    IInteractionSessionManager sessionManager,
    IInteractionHandlerRegistry handlerRegistry,
    IInteractionDefinitionRepository definitionRepository) : IInteractionSubsystem
{
    /// <inheritdoc />
    public Task<CommandResult> PerformInteractionAsync(
        CharacterId characterId,
        string interactionTag,
        Guid targetId,
        string? areaResRef = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        PerformInteractionCommand command = new(
            characterId, interactionTag, targetId, areaResRef, metadata);

        return commandDispatcher.DispatchAsync(command, ct);
    }

    /// <inheritdoc />
    public bool HasActiveInteraction(CharacterId characterId)
        => sessionManager.HasActiveSession(characterId);

    /// <inheritdoc />
    public InteractionInfo? GetActiveInteraction(CharacterId characterId)
    {
        InteractionSession? session = sessionManager.GetActiveSession(characterId);
        if (session is null) return null;

        return new InteractionInfo(
            session.InteractionTag,
            session.TargetId,
            session.Progress,
            session.RequiredRounds,
            session.StartedAt);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetRegisteredInteractionTypes()
    {
        HashSet<string> tags = new(StringComparer.OrdinalIgnoreCase);

        // Compiled handlers
        foreach (IInteractionHandler handler in handlerRegistry.GetAll())
        {
            tags.Add(handler.InteractionTag);
        }

        // Data-driven definitions
        foreach (InteractionDefinition definition in definitionRepository.All())
        {
            tags.Add(definition.Tag);
        }

        return tags.ToList();
    }

    /// <inheritdoc />
    public string GetInteractionDisplayName(string interactionTag)
    {
        InteractionDefinition? definition = definitionRepository.Get(interactionTag);
        if (definition is not null && !string.IsNullOrWhiteSpace(definition.Name))
        {
            return definition.Name;
        }

        // Fallback: humanize the tag
        string display = interactionTag;
        if (display.Length > 0)
        {
            display = char.ToUpper(display[0]) + display[1..];
        }

        return display.Replace('_', ' ');
    }
}
