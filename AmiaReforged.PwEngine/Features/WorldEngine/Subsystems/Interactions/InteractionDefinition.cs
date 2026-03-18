using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Data-driven definition of an interaction type. Stored in the database and editable
/// from the admin panel, allowing designers to create new interaction types without code changes.
/// <para>
/// At runtime, the <see cref="Commands.PerformInteractionCommandHandler"/> falls back to
/// loading an <see cref="InteractionDefinition"/> from the repository when no compiled
/// <see cref="IInteractionHandler"/> claims the requested tag, then wraps it in a
/// <see cref="Handlers.DataDrivenInteractionAdapter"/>.
/// </para>
/// </summary>
public class InteractionDefinition
{
    /// <summary>
    /// Unique tag identifying this interaction type (e.g. <c>"prospecting"</c>, <c>"surveying"</c>).
    /// Acts as the primary key.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>Display name shown in the admin panel and optionally to players.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description for admin reference.</summary>
    public string? Description { get; init; }

    /// <summary>What kind of game entity this interaction targets.</summary>
    public InteractionTargetMode TargetMode { get; init; } = InteractionTargetMode.Trigger;

    /// <summary>Default number of rounds before completion, prior to proficiency adjustments.</summary>
    public int BaseRounds { get; init; } = 4;

    /// <summary>Minimum number of rounds even at maximum proficiency.</summary>
    public int MinRounds { get; init; } = 2;

    /// <summary>
    /// When <c>true</c>, the character's best industry proficiency level
    /// reduces the required rounds (higher proficiency → fewer rounds, down to <see cref="MinRounds"/>).
    /// </summary>
    public bool ProficiencyReducesRounds { get; init; } = true;

    /// <summary>
    /// When <c>true</c>, the character must be a member of at least one industry
    /// to start this interaction. Checked in addition to the knowledge unlock gate.
    /// </summary>
    public bool RequiresIndustryMembership { get; init; } = true;

    /// <summary>
    /// When non-empty, the character must belong to at least one of these industries.
    /// If empty and <see cref="RequiresIndustryMembership"/> is <c>true</c>, any industry counts.
    /// </summary>
    public List<string> RequiredIndustryTags { get; init; } = [];

    /// <summary>
    /// When non-empty, this interaction is only valid in the listed area resrefs.
    /// Empty means valid in any area.
    /// </summary>
    public List<string> AllowedAreaResRefs { get; init; } = [];

    /// <summary>
    /// Knowledge tags the character must possess before starting this interaction.
    /// When non-empty, the character must have every listed tag or the precondition fails.
    /// When empty (default), there is no knowledge prerequisite.
    /// </summary>
    public List<string> RequiredKnowledgeTags { get; init; } = [];

    /// <summary>
    /// Possible outcomes when this interaction completes. One response is selected
    /// via weighted random from the eligible pool (filtered by proficiency).
    /// Must have at least one response for the interaction to succeed.
    /// </summary>
    public List<InteractionResponse> Responses { get; init; } = [];

    /// <summary>
    /// Selects eligible responses for <paramref name="proficiency"/> and picks one
    /// via weighted random selection.
    /// </summary>
    /// <returns>The selected response, or <c>null</c> if no responses are eligible.</returns>
    public InteractionResponse? SelectResponse(ProficiencyLevel proficiency)
    {
        List<InteractionResponse> eligible = Responses
            .Where(r => r.MinProficiency is null || proficiency >= r.MinProficiency.Value)
            .ToList();

        if (eligible.Count == 0) return null;
        if (eligible.Count == 1) return eligible[0];

        int totalWeight = eligible.Sum(r => r.Weight);
        int roll = Random.Shared.Next(totalWeight);

        int cumulative = 0;
        foreach (InteractionResponse response in eligible)
        {
            cumulative += response.Weight;
            if (roll < cumulative) return response;
        }

        return eligible[^1]; // Shouldn't reach here, but safety fallback
    }
}
