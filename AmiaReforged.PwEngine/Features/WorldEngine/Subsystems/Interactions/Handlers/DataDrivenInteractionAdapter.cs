using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Handlers;

/// <summary>
/// Generic interaction handler that reads its behavior from an <see cref="InteractionDefinition"/>
/// stored in the database. Enables fully data-driven interactions configurable from the admin panel.
/// <para>
/// Created on-demand by <see cref="Commands.PerformInteractionCommandHandler"/> when no compiled
/// <see cref="IInteractionHandler"/> claims the requested tag but a matching definition exists.
/// Lightweight and stateless — the definition is immutable for the handler's lifetime.
/// </para>
/// </summary>
internal sealed class DataDrivenInteractionAdapter : IInteractionHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly InteractionDefinition _definition;
    private readonly IEventBus _eventBus;

    public DataDrivenInteractionAdapter(InteractionDefinition definition, IEventBus eventBus)
    {
        _definition = definition;
        _eventBus = eventBus;
    }

    /// <inheritdoc />
    public string InteractionTag => _definition.Tag;

    /// <inheritdoc />
    public InteractionTargetMode TargetMode => _definition.TargetMode;

    /// <inheritdoc />
    public PreconditionResult CanStart(ICharacter character, InteractionContext context)
    {
        // Knowledge unlock gate — character must have learned knowledge
        // with KnowledgeEffectType.UnlockInteraction targeting this tag
        if (!character.HasUnlockedInteraction(_definition.Tag))
        {
            return PreconditionResult.Fail(
                $"You haven't learned the knowledge required for {_definition.Name}");
        }

        // Optional industry membership check
        if (_definition.RequiresIndustryMembership)
        {
            List<IndustryMembership> memberships = character.AllIndustryMemberships();
            if (memberships.Count == 0)
            {
                return PreconditionResult.Fail("You must be a member of an industry");
            }
        }

        return PreconditionResult.Success();
    }

    /// <inheritdoc />
    public int CalculateRequiredRounds(ICharacter character, InteractionContext context)
    {
        if (!_definition.ProficiencyReducesRounds)
        {
            return _definition.BaseRounds;
        }

        ProficiencyLevel bestLevel = GetBestProficiency(character);
        int roundReduction = (int)bestLevel;
        return Math.Max(_definition.MinRounds, _definition.BaseRounds - roundReduction);
    }

    /// <inheritdoc />
    public TickResult OnTick(InteractionSession session, ICharacter character)
    {
        int newProgress = session.IncrementProgress(1);
        InteractionStatus status = session.IsComplete
            ? InteractionStatus.Completed
            : InteractionStatus.Active;

        string? message = status == InteractionStatus.Active
            ? $"{_definition.Name}... ({newProgress}/{session.RequiredRounds})"
            : null;

        return new TickResult(status, newProgress, session.RequiredRounds, message);
    }

    /// <inheritdoc />
    public async Task<InteractionOutcome> OnCompleteAsync(
        InteractionSession session,
        ICharacter character,
        CancellationToken ct = default)
    {
        ProficiencyLevel proficiency = GetBestProficiency(character);

        InteractionResponse? response = _definition.SelectResponse(proficiency);
        if (response is null)
        {
            Log.Warn("No eligible responses for interaction '{Tag}' at proficiency {Level}",
                _definition.Tag, proficiency);
            return InteractionOutcome.Failed("No valid outcome for your skill level");
        }

        Log.Info("Interaction '{Tag}' completed for {CharacterId} — selected response '{ResponseTag}'",
            _definition.Tag, session.CharacterId, response.ResponseTag);

        // Publish event so runtime subscribers can apply VFX, text, spawns, etc.
        await _eventBus.PublishAsync(new InteractionResponseSelectedEvent(
            session.Id,
            session.CharacterId,
            _definition.Tag,
            response.ResponseTag,
            response.Effects,
            DateTime.UtcNow), ct);

        Dictionary<string, object> data = new Dictionary<string, object>
        {
            ["responseTag"] = response.ResponseTag,
            ["effectCount"] = response.Effects.Count
        };

        return InteractionOutcome.Succeeded(response.Message, data);
    }

    /// <inheritdoc />
    public void OnCancel(InteractionSession session, ICharacter character)
    {
        Log.Debug("{InteractionTag} cancelled for character {CharacterId}",
            _definition.Tag, session.CharacterId);
    }

    private static ProficiencyLevel GetBestProficiency(ICharacter character)
    {
        List<IndustryMembership> memberships = character.AllIndustryMemberships();
        if (memberships.Count == 0) return ProficiencyLevel.Layman;
        return memberships.Max(m => m.Level);
    }
}
