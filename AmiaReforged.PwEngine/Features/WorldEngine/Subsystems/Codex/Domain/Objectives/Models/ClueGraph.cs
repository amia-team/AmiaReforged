namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Models;

/// <summary>
/// A single clue in an investigation. Clues are discovered via signals
/// and can unlock deductions when prerequisite clues are all found.
/// </summary>
public sealed class Clue
{
    /// <summary>Unique identifier for this clue within the investigation.</summary>
    public required string ClueId { get; init; }

    /// <summary>Player-visible name of the clue.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description shown when the clue is discovered.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The signal target tag that discovers this clue.
    /// When a signal with this tag arrives, the clue is marked as discovered.
    /// </summary>
    public required string TriggerTag { get; init; }
}

/// <summary>
/// A deduction that becomes available when all prerequisite clues are discovered.
/// Deductions can themselves act as clues for further deductions, forming a DAG.
/// </summary>
public sealed class Deduction
{
    /// <summary>Unique identifier for this deduction.</summary>
    public required string DeductionId { get; init; }

    /// <summary>Player-visible text describing the deduction.</summary>
    public required string Description { get; init; }

    /// <summary>
    /// Clue IDs that must all be discovered before this deduction is unlocked.
    /// </summary>
    public required List<string> RequiredClueIds { get; init; }

    /// <summary>
    /// Optional clue IDs that this deduction unlocks when it is reached.
    /// Allows chaining: discovering clues → deduction → new clues → deeper deduction.
    /// </summary>
    public List<string> UnlocksClueIds { get; init; } = [];
}

/// <summary>
/// A directed acyclic graph of clues and deductions representing an investigation.
/// The objective is complete when the designated conclusion deduction is reached.
/// </summary>
public sealed class ClueGraph
{
    /// <summary>All clues in this investigation.</summary>
    public required List<Clue> Clues { get; init; }

    /// <summary>All deductions linking clue prerequisites to conclusions.</summary>
    public required List<Deduction> Deductions { get; init; }

    /// <summary>
    /// The deduction ID that represents the final conclusion.
    /// When this deduction is unlocked, the investigation objective is complete.
    /// </summary>
    public required string ConclusionDeductionId { get; init; }

    /// <summary>
    /// Validates that the graph is structurally sound:
    /// - All clue IDs referenced in deductions exist
    /// - The conclusion deduction exists
    /// - No duplicate IDs
    /// </summary>
    public ValidationResult Validate()
    {
        HashSet<string> clueIds = new(Clues.Select(c => c.ClueId));
        HashSet<string> deductionIds = new(Deductions.Select(d => d.DeductionId));
        List<string> errors = [];

        if (Clues.Count != clueIds.Count)
            errors.Add("Duplicate clue IDs found");

        if (Deductions.Count != deductionIds.Count)
            errors.Add("Duplicate deduction IDs found");

        if (!deductionIds.Contains(ConclusionDeductionId))
            errors.Add($"Conclusion deduction '{ConclusionDeductionId}' not found in deductions");

        foreach (Deduction deduction in Deductions)
        {
            foreach (string requiredId in deduction.RequiredClueIds)
            {
                // Required clues can be base clues or clues unlocked by other deductions
                if (!clueIds.Contains(requiredId))
                    errors.Add($"Deduction '{deduction.DeductionId}' requires unknown clue '{requiredId}'");
            }

            foreach (string unlockedId in deduction.UnlocksClueIds)
            {
                if (!clueIds.Contains(unlockedId))
                    errors.Add($"Deduction '{deduction.DeductionId}' unlocks unknown clue '{unlockedId}'");
            }
        }

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors);
    }
}

/// <summary>
/// Result of validating a model (clue graph, state machine, etc.).
/// </summary>
public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Valid() => new(true, Array.Empty<string>());
    public static ValidationResult Invalid(IReadOnlyList<string> errors) => new(false, errors);
}
