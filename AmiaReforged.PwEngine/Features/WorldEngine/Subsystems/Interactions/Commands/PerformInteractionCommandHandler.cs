using AmiaReforged.PwEngine.Features.Glyph.Integration;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Handlers;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;

/// <summary>
/// Central dispatcher for the Interaction Framework.
/// Manages session lifecycle (create → tick → complete/cancel) and delegates
/// domain-specific behavior to the matching <see cref="IInteractionHandler"/>.
/// Falls back to <see cref="DataDrivenInteractionAdapter"/> when no compiled handler
/// claims the requested tag but a matching <see cref="InteractionDefinition"/> exists.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<PerformInteractionCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class PerformInteractionCommandHandler(
    IInteractionSessionManager sessionManager,
    ICharacterRepository characterRepository,
    IInteractionHandlerRegistry handlerRegistry,
    IInteractionDefinitionRepository definitionRepository,
    IEventBus eventBus,
    GlyphInteractionHookService? glyphHook = null) : ICommandHandler<PerformInteractionCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task<CommandResult> HandleAsync(
        PerformInteractionCommand command,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve handler — compiled handlers first, then data-driven definitions
        IInteractionHandler? handler = handlerRegistry.GetHandler(command.InteractionTag);
        if (handler is null)
        {
            // Fallback: check for a data-driven definition in the repository
            InteractionDefinition? definition = definitionRepository.Get(command.InteractionTag);
            if (definition is not null)
            {
                handler = new DataDrivenInteractionAdapter(definition, eventBus);
            }
            else
            {
                return CommandResult.Fail($"Unknown interaction type: {command.InteractionTag}");
            }
        }

        // 2. Resolve character
        ICharacter? character = characterRepository.GetById(command.CharacterId);
        if (character is null)
        {
            return CommandResult.Fail("Character not found");
        }

        InteractionContext context = new(
            command.CharacterId,
            command.TargetId,
            handler.TargetMode,
            command.AreaResRef,
            command.Metadata);

        // 3. Session management — get-or-create with exclusive cancellation
        InteractionSession? session = sessionManager.GetActiveSession(command.CharacterId);

        if (session is not null && IsDifferentInteraction(session, command))
        {
            CancelExistingSession(session, character);
            session = null;
        }

        if (session is null)
        {
            CommandResult? startResult = await TryStartNewSessionAsync(
                handler, character, context, command, cancellationToken);

            if (startResult is not null)
            {
                return startResult; // precondition failure
            }

            session = sessionManager.GetActiveSession(command.CharacterId)!;
        }

        // 4. Tick
        TickResult tick = handler.OnTick(session, character);

        if (tick.Status == InteractionStatus.Completed)
        {
            return await CompleteInteractionAsync(handler, session, character, cancellationToken);
        }

        // Glyph OnTick hook — scripts can cancel the interaction mid-progress
        if (glyphHook is not null)
        {
            (bool shouldCancel, string? cancelMessage) = glyphHook.RunOnInteractionTick(
                session.InteractionTag,
                session.CharacterId.Value.ToString(),
                session.TargetId,
                session.AreaResRef,
                session.Id,
                session.Progress,
                session.RequiredRounds,
                proficiency: null,
                session.Metadata);

            if (shouldCancel)
            {
                Log.Info("Glyph script cancelled '{Tag}' session {SessionId}: {Message}",
                    session.InteractionTag, session.Id, cancelMessage);
                handler.OnCancel(session, character);
                sessionManager.EndSession(session.CharacterId);
                return CommandResult.Fail(cancelMessage ?? "Interaction cancelled by script");
            }
        }

        return CommandResult.OkWith("status", "InProgress");
    }

    private static bool IsDifferentInteraction(InteractionSession session, PerformInteractionCommand command)
        => session.InteractionTag != command.InteractionTag || session.TargetId != command.TargetId;

    private void CancelExistingSession(InteractionSession session, ICharacter character)
    {
        IInteractionHandler? oldHandler = handlerRegistry.GetHandler(session.InteractionTag);
        if (oldHandler is not null)
        {
            Log.Debug("Cancelling active '{Tag}' session {SessionId} for character {CharacterId}",
                session.InteractionTag, session.Id, session.CharacterId);
            oldHandler.OnCancel(session, character);
        }

        sessionManager.EndSession(session.CharacterId);
    }

    private async Task<CommandResult?> TryStartNewSessionAsync(
        IInteractionHandler handler,
        ICharacter character,
        InteractionContext context,
        PerformInteractionCommand command,
        CancellationToken ct)
    {
        // Glyph OnAttempted hook — scripts can block the interaction before preconditions
        if (glyphHook is not null)
        {
            (bool shouldBlock, string? blockMessage, Dictionary<string, object>? glyphCapturedMetadata) =
                glyphHook.RunOnInteractionAttempted(
                    command.InteractionTag,
                    command.CharacterId.Value.ToString(),
                    command.TargetId,
                    handler.TargetMode.ToString(),
                    command.AreaResRef,
                    proficiency: null,
                    command.Metadata);

            if (shouldBlock)
            {
                return CommandResult.Fail(blockMessage ?? "Interaction blocked by script");
            }

            // Merge any metadata that Glyph scripts stored during the Attempted stage
            // (e.g., via Store Session Object) into the command metadata so it flows
            // into the session when StartSession is called below.
            if (glyphCapturedMetadata is { Count: > 0 })
            {
                command = command with
                {
                    Metadata = MergeMetadata(command.Metadata, glyphCapturedMetadata)
                };
            }
        }

        PreconditionResult canStart = handler.CanStart(character, context);
        if (!canStart.Passed)
        {
            return CommandResult.Fail(canStart.FailureMessage ?? "Cannot start interaction");
        }

        int requiredRounds = handler.CalculateRequiredRounds(character, context);
        InteractionSession session = sessionManager.StartSession(
            command.CharacterId,
            command.InteractionTag,
            command.TargetId,
            handler.TargetMode,
            requiredRounds,
            command.AreaResRef,
            command.Metadata);

        Log.Info("Started '{Tag}' interaction session {SessionId} for character {CharacterId} ({Rounds} rounds)",
            command.InteractionTag, session.Id, command.CharacterId, requiredRounds);

        await eventBus.PublishAsync(new InteractionStartedEvent(
            session.Id,
            command.CharacterId,
            command.InteractionTag,
            command.TargetId,
            requiredRounds,
            DateTime.UtcNow), ct);

        return null; // success — session is now active
    }

    private async Task<CommandResult> CompleteInteractionAsync(
        IInteractionHandler handler,
        InteractionSession session,
        ICharacter character,
        CancellationToken ct)
    {
        InteractionOutcome outcome = await handler.OnCompleteAsync(session, character, ct);

        // Run Glyph OnCompleted stage synchronously BEFORE ending the session,
        // so that pipeline scripts can access session data and traces are captured
        // before the command returns (the async event bus would be too late).
        if (glyphHook is not null)
        {
            glyphHook.RunOnInteractionCompleted(
                session.InteractionTag,
                session.CharacterId.Value.ToString(),
                session.TargetId,
                session.Id,
                outcome.Success,
                proficiency: null,
                session.Metadata);
        }

        sessionManager.EndSession(session.CharacterId);

        Log.Info("Completed '{Tag}' interaction session {SessionId}: {Success}",
            session.InteractionTag, session.Id, outcome.Success ? "success" : "failed");

        await eventBus.PublishAsync(new InteractionCompletedEvent(
            session.Id,
            session.CharacterId,
            session.InteractionTag,
            session.TargetId,
            outcome.Success,
            outcome.Message,
            DateTime.UtcNow), ct);

        Dictionary<string, object> data = outcome.Data != null
            ? new Dictionary<string, object>(outcome.Data)
            : new Dictionary<string, object>();

        data["status"] = outcome.Success ? "Completed" : "Failed";

        return new CommandResult
        {
            Success = outcome.Success,
            ErrorMessage = outcome.Success ? null : outcome.Message,
            Data = data
        };
    }

    /// <summary>
    /// Merges <paramref name="source"/> entries into <paramref name="target"/>, creating a
    /// new dictionary when <paramref name="target"/> is <c>null</c>.
    /// Source values overwrite existing keys in target (Glyph scripts win).
    /// </summary>
    private static Dictionary<string, object> MergeMetadata(
        Dictionary<string, object>? target,
        Dictionary<string, object> source)
    {
        Dictionary<string, object> result = target != null
            ? new Dictionary<string, object>(target)
            : new Dictionary<string, object>();

        foreach (KeyValuePair<string, object> kvp in source)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }
}
