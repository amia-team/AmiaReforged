namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn;

/// <summary>
/// Pure static scoring logic for Aetharn dice game.
/// All methods are pure functions with no side effects or external dependencies.
/// </summary>
public static class AetharnScorer
{
    /// <summary>
    /// Evaluates a set of dice and returns the optimal scoring result.
    /// </summary>
    /// <param name="diceValues">Array of dice face values (1-6).</param>
    /// <returns>A ScoringResult containing points, scoring dice indices, and combination type.</returns>
    public static ScoringResult Evaluate(int[] diceValues)
    {
        if (diceValues == null || diceValues.Length == 0)
        {
            return ScoringResult.Bust;
        }

        // Check for special combinations first (these use all 6 dice)
        if (diceValues.Length == AetharnConstants.DiceCount)
        {
            // Check for straight (1-2-3-4-5-6)
            if (IsStraight(diceValues))
            {
                return ScoringResult.Create(
                    points: AetharnConstants.StraightPoints,
                    scoringDiceIndices: Enumerable.Range(0, diceValues.Length).ToArray(),
                    nonScoringDiceIndices: [],
                    type: ScoringType.Straight
                );
            }

            // Check for three pairs - but compare with standard scoring to pick best
            if (IsThreePairs(diceValues))
            {
                ScoringResult standardResult = EvaluateStandardScoring(diceValues);
                
                // Three pairs is 1500 - only use if it beats standard scoring
                if (AetharnConstants.ThreePairsPoints > standardResult.Points)
                {
                    return ScoringResult.Create(
                        points: AetharnConstants.ThreePairsPoints,
                        scoringDiceIndices: Enumerable.Range(0, diceValues.Length).ToArray(),
                        nonScoringDiceIndices: [],
                        type: ScoringType.ThreePairs
                    );
                }
                
                // Standard scoring is better (e.g., six-of-a-kind)
                return standardResult;
            }
        }

        // Check for N-of-a-kind and singles
        return EvaluateStandardScoring(diceValues);
    }

    /// <summary>
    /// Checks if the dice form a straight (1-2-3-4-5-6).
    /// </summary>
    public static bool IsStraight(int[] diceValues)
    {
        if (diceValues.Length != AetharnConstants.DiceCount)
        {
            return false;
        }

        int[] sorted = diceValues.OrderBy(d => d).ToArray();
        return sorted.SequenceEqual([1, 2, 3, 4, 5, 6]);
    }

    /// <summary>
    /// Checks if the dice form three pairs.
    /// </summary>
    public static bool IsThreePairs(int[] diceValues)
    {
        if (diceValues.Length != AetharnConstants.DiceCount)
        {
            return false;
        }

        Dictionary<int, int> counts = GetFaceCounts(diceValues);
        
        // Three pairs means exactly 3 distinct values, each appearing exactly twice
        // OR one value appearing 4 times and another appearing 2 times (4+2 = 6)
        // OR one value appearing 6 times (counts as 3 pairs of same)
        // Actually per standard rules: three pairs means 3 distinct pairs
        
        // Count how many pairs we have (a count of 2 = 1 pair, count of 4 = 2 pairs, count of 6 = 3 pairs)
        int pairCount = 0;
        foreach (int count in counts.Values)
        {
            pairCount += count / 2;
        }

        return pairCount == 3;
    }

    /// <summary>
    /// Calculates the base points for three of a kind.
    /// </summary>
    public static int CalculateThreeOfAKindValue(int faceValue)
    {
        if (faceValue == 1)
        {
            return AetharnConstants.ThreeOnesPoints;
        }
        return faceValue * AetharnConstants.ThreeOfAKindMultiplier;
    }

    /// <summary>
    /// Evaluates standard scoring (N-of-a-kind and singles).
    /// </summary>
    private static ScoringResult EvaluateStandardScoring(int[] diceValues)
    {
        Dictionary<int, int> counts = GetFaceCounts(diceValues);
        Dictionary<int, List<int>> indexesByFace = GetIndexesByFace(diceValues);

        int totalPoints = 0;
        List<int> scoringIndices = [];
        ScoringType bestType = ScoringType.None;

        // Process each face value
        foreach (KeyValuePair<int, int> kvp in counts)
        {
            int face = kvp.Key;
            int count = kvp.Value;
            List<int> indices = indexesByFace[face];

            if (count >= 3)
            {
                // N-of-a-kind scoring
                int basePoints = CalculateThreeOfAKindValue(face);
                int multiplier = 1;

                // Double for each die beyond 3
                for (int i = 3; i < count; i++)
                {
                    multiplier *= AetharnConstants.AdditionalDieMultiplier;
                }

                totalPoints += basePoints * multiplier;
                scoringIndices.AddRange(indices);

                // Track the best scoring type
                bestType = count switch
                {
                    6 => ScoringType.SixOfAKind,
                    5 => ScoringType.FiveOfAKind,
                    4 => ScoringType.FourOfAKind,
                    _ => bestType == ScoringType.None ? ScoringType.ThreeOfAKind : bestType
                };
            }
            else
            {
                // Check for individual 1s and 5s
                if (face == 1)
                {
                    totalPoints += count * AetharnConstants.SingleOnePoints;
                    scoringIndices.AddRange(indices);
                    if (bestType == ScoringType.None)
                    {
                        bestType = ScoringType.Singles;
                    }
                }
                else if (face == 5)
                {
                    totalPoints += count * AetharnConstants.SingleFivePoints;
                    scoringIndices.AddRange(indices);
                    if (bestType == ScoringType.None)
                    {
                        bestType = ScoringType.Singles;
                    }
                }
            }
        }

        // Calculate non-scoring indices
        int[] nonScoringIndices = Enumerable.Range(0, diceValues.Length)
            .Except(scoringIndices)
            .ToArray();

        if (totalPoints == 0)
        {
            return ScoringResult.Bust;
        }

        return ScoringResult.Create(
            points: totalPoints,
            scoringDiceIndices: scoringIndices.OrderBy(i => i).ToArray(),
            nonScoringDiceIndices: nonScoringIndices,
            type: bestType
        );
    }

    /// <summary>
    /// Gets the count of each face value in the dice array.
    /// </summary>
    private static Dictionary<int, int> GetFaceCounts(int[] diceValues)
    {
        Dictionary<int, int> counts = new();
        foreach (int value in diceValues)
        {
            counts.TryAdd(value, 0);
            counts[value]++;
        }
        return counts;
    }

    /// <summary>
    /// Gets the indices of each face value in the dice array.
    /// </summary>
    private static Dictionary<int, List<int>> GetIndexesByFace(int[] diceValues)
    {
        Dictionary<int, List<int>> indexesByFace = new();
        for (int i = 0; i < diceValues.Length; i++)
        {
            int face = diceValues[i];
            if (!indexesByFace.ContainsKey(face))
            {
                indexesByFace[face] = [];
            }
            indexesByFace[face].Add(i);
        }
        return indexesByFace;
    }
}
