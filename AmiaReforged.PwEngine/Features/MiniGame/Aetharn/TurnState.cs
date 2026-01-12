namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn;

/// <summary>
/// Represents the state of a single die during a turn.
/// Tracks both the face value and whether it has been held for scoring.
/// </summary>
/// <param name="Value">The face value of the die (1-6).</param>
/// <param name="IsHeld">Whether this die has been held for scoring.</param>
public readonly record struct Die(int Value, bool IsHeld = false)
{
    /// <summary>
    /// Creates a new Die with the held state set to true.
    /// </summary>
    public Die Hold() => this with { IsHeld = true };
}

/// <summary>
/// Immutable snapshot of a player's turn progress in Aetharn.
/// Uses record semantics for value equality and easy state transitions via with-expressions.
/// </summary>
public sealed record TurnState
{
    /// <summary>
    /// The player key identifier (ds_pckey) for the player whose turn this is.
    /// </summary>
    public string PlayerKey { get; init; } = string.Empty;

    /// <summary>
    /// The current state of all dice in play.
    /// </summary>
    public IReadOnlyList<Die> Dice { get; init; } = [];

    /// <summary>
    /// Points accumulated during this turn (not yet banked).
    /// </summary>
    public int AccumulatedPoints { get; init; }

    /// <summary>
    /// Whether the player has already used their Hot Dice bonus this turn.
    /// Only one Hot Dice roll is allowed per turn to prevent runaway wins.
    /// </summary>
    public bool HasUsedHotDice { get; init; }

    /// <summary>
    /// The number of rolls made this turn.
    /// </summary>
    public int RollCount { get; init; }

    /// <summary>
    /// Gets the dice that have been held for scoring.
    /// </summary>
    public IReadOnlyList<Die> HeldDice => Dice.Where(d => d.IsHeld).ToList();

    /// <summary>
    /// Gets the dice that are still available to roll.
    /// </summary>
    public IReadOnlyList<Die> RemainingDice => Dice.Where(d => !d.IsHeld).ToList();

    /// <summary>
    /// Gets the number of dice available to roll.
    /// </summary>
    public int RemainingDiceCount => Dice.Count(d => !d.IsHeld);

    /// <summary>
    /// Returns true if all dice have been held (potential Hot Dice situation).
    /// </summary>
    public bool AllDiceHeld => Dice.Count > 0 && Dice.All(d => d.IsHeld);

    /// <summary>
    /// Creates a new turn state for a player with freshly rolled dice.
    /// </summary>
    /// <param name="playerKey">The player key identifier.</param>
    /// <param name="diceValues">The values of the rolled dice.</param>
    /// <returns>A new TurnState representing the start of a turn.</returns>
    public static TurnState StartTurn(string playerKey, int[] diceValues)
    {
        ArgumentNullException.ThrowIfNull(playerKey);
        ArgumentNullException.ThrowIfNull(diceValues);

        return new TurnState
        {
            PlayerKey = playerKey,
            Dice = diceValues.Select(v => new Die(v)).ToList(),
            AccumulatedPoints = 0,
            HasUsedHotDice = false,
            RollCount = 1
        };
    }

    /// <summary>
    /// Creates a new turn state with the specified dice held.
    /// Validates that the held dice are valid scoring dice using AetharnScorer.
    /// </summary>
    /// <param name="indicesToHold">Indices of dice to hold (must be scoring dice).</param>
    /// <returns>A result containing either the new state or an error.</returns>
    public HoldResult HoldDice(int[] indicesToHold)
    {
        ArgumentNullException.ThrowIfNull(indicesToHold);

        if (indicesToHold.Length == 0)
        {
            return HoldResult.Failure("Must hold at least one die.");
        }

        // Validate indices are in range and not already held
        foreach (int index in indicesToHold)
        {
            if (index < 0 || index >= Dice.Count)
            {
                return HoldResult.Failure($"Invalid die index: {index}");
            }

            if (Dice[index].IsHeld)
            {
                return HoldResult.Failure($"Die at index {index} is already held.");
            }
        }

        // Get the values of dice being held
        int[] heldValues = indicesToHold.Select(i => Dice[i].Value).ToArray();

        // Validate that the held dice are scoring dice
        ScoringResult scoringResult = AetharnScorer.Evaluate(heldValues);
        if (scoringResult.IsBust)
        {
            return HoldResult.Failure("Selected dice do not form a valid scoring combination.");
        }

        // Create new dice list with specified indices held
        List<Die> newDice = [];
        for (int i = 0; i < Dice.Count; i++)
        {
            newDice.Add(indicesToHold.Contains(i) ? Dice[i].Hold() : Dice[i]);
        }

        TurnState newState = this with
        {
            Dice = newDice,
            AccumulatedPoints = AccumulatedPoints + scoringResult.Points
        };

        return HoldResult.Success(newState, scoringResult);
    }

    /// <summary>
    /// Creates a new turn state after rolling the remaining (non-held) dice.
    /// </summary>
    /// <param name="newValues">The new values for the remaining dice.</param>
    /// <returns>A result containing either the new state or an error.</returns>
    public RollResult Roll(int[] newValues)
    {
        ArgumentNullException.ThrowIfNull(newValues);

        int remainingCount = RemainingDiceCount;

        if (remainingCount == 0)
        {
            return RollResult.Failure("No dice remaining to roll.");
        }

        if (newValues.Length != remainingCount)
        {
            return RollResult.Failure(
                $"Expected {remainingCount} dice values, got {newValues.Length}.");
        }

        // Build new dice list: keep held dice, replace remaining with new values
        List<Die> newDice = [];
        int newValueIndex = 0;

        foreach (Die die in Dice)
        {
            if (die.IsHeld)
            {
                newDice.Add(die);
            }
            else
            {
                newDice.Add(new Die(newValues[newValueIndex++]));
            }
        }

        // Evaluate the new remaining dice for scoring
        ScoringResult scoringResult = AetharnScorer.Evaluate(newValues);

        TurnState newState = this with
        {
            Dice = newDice,
            RollCount = RollCount + 1
        };

        return RollResult.Success(newState, scoringResult);
    }

    /// <summary>
    /// Creates a new turn state after using Hot Dice (all 6 dice reset to rollable).
    /// Can only be used once per turn.
    /// </summary>
    /// <param name="newValues">The values for all 6 freshly rolled dice.</param>
    /// <returns>A result containing either the new state or an error.</returns>
    public HotDiceResult UseHotDice(int[] newValues)
    {
        ArgumentNullException.ThrowIfNull(newValues);

        if (HasUsedHotDice)
        {
            return HotDiceResult.Failure("Hot Dice has already been used this turn.");
        }

        if (!AllDiceHeld)
        {
            return HotDiceResult.Failure("Hot Dice can only be used when all dice are held.");
        }

        if (newValues.Length != AetharnConstants.DiceCount)
        {
            return HotDiceResult.Failure(
                $"Hot Dice requires exactly {AetharnConstants.DiceCount} dice values.");
        }

        ScoringResult scoringResult = AetharnScorer.Evaluate(newValues);

        TurnState newState = this with
        {
            Dice = newValues.Select(v => new Die(v)).ToList(),
            HasUsedHotDice = true,
            RollCount = RollCount + 1
        };

        return HotDiceResult.Success(newState, scoringResult);
    }

    /// <summary>
    /// Creates a fresh turn state for a new turn (resets everything except player key).
    /// </summary>
    /// <param name="diceValues">The values of the freshly rolled dice.</param>
    /// <returns>A new TurnState for the new turn.</returns>
    public TurnState Reset(int[] diceValues)
    {
        return StartTurn(PlayerKey, diceValues);
    }
}

/// <summary>
/// Result of attempting to hold dice during a turn.
/// </summary>
public sealed record HoldResult
{
    public bool IsSuccess { get; init; }
    public TurnState? NewState { get; init; }
    public ScoringResult? ScoringResult { get; init; }
    public string? ErrorMessage { get; init; }

    public static HoldResult Success(TurnState newState, ScoringResult scoringResult) =>
        new()
        {
            IsSuccess = true,
            NewState = newState,
            ScoringResult = scoringResult
        };

    public static HoldResult Failure(string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Result of attempting to roll dice during a turn.
/// </summary>
public sealed record RollResult
{
    public bool IsSuccess { get; init; }
    public TurnState? NewState { get; init; }
    public ScoringResult? ScoringResult { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// True if the roll resulted in a bust (no scoring dice).
    /// </summary>
    public bool IsBust => IsSuccess && ScoringResult?.IsBust == true;

    public static RollResult Success(TurnState newState, ScoringResult scoringResult) =>
        new()
        {
            IsSuccess = true,
            NewState = newState,
            ScoringResult = scoringResult
        };

    public static RollResult Failure(string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}

/// <summary>
/// Result of attempting to use Hot Dice during a turn.
/// </summary>
public sealed record HotDiceResult
{
    public bool IsSuccess { get; init; }
    public TurnState? NewState { get; init; }
    public ScoringResult? ScoringResult { get; init; }
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// True if the Hot Dice roll resulted in a bust.
    /// </summary>
    public bool IsBust => IsSuccess && ScoringResult?.IsBust == true;

    public static HotDiceResult Success(TurnState newState, ScoringResult scoringResult) =>
        new()
        {
            IsSuccess = true,
            NewState = newState,
            ScoringResult = scoringResult
        };

    public static HotDiceResult Failure(string errorMessage) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}
