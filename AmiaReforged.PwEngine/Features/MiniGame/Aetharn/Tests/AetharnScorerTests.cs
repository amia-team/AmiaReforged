using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Tests;

[TestFixture]
public class AetharnScorerTests
{
    #region Bust (No Scoring)

    [Test]
    public void Evaluate_NullDice_ReturnsBust()
    {
        ScoringResult result = AetharnScorer.Evaluate(null!);

        result.IsBust.Should().BeTrue();
        result.Points.Should().Be(0);
        result.Type.Should().Be(ScoringType.None);
    }

    [Test]
    public void Evaluate_EmptyDice_ReturnsBust()
    {
        ScoringResult result = AetharnScorer.Evaluate([]);

        result.IsBust.Should().BeTrue();
        result.Points.Should().Be(0);
    }

    [Test]
    public void Evaluate_NoScoringDice_ReturnsBust()
    {
        // 2, 3, 4, 6 - no 1s, no 5s, no three of a kind
        int[] dice = [2, 3, 4, 6, 2, 3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.IsBust.Should().BeTrue();
        result.Points.Should().Be(0);
        result.Type.Should().Be(ScoringType.None);
    }

    [Test]
    public void Evaluate_AllNonScoringTwosThreesFours_ReturnsBust()
    {
        // 2, 3, 4, 6, 2, 4 - not three pairs, no 1s or 5s, no three of a kind
        int[] dice = [2, 3, 4, 6, 2, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.IsBust.Should().BeTrue();
    }

    #endregion

    #region Singles (1s and 5s)

    [Test]
    public void Evaluate_SingleOne_Returns100Points()
    {
        int[] dice = [1, 2, 3, 4, 6, 2];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(100);
        result.Type.Should().Be(ScoringType.Singles);
        result.ScoringDiceIndices.Should().Contain(0);
        result.NonScoringDiceIndices.Should().HaveCount(5);
    }

    [Test]
    public void Evaluate_SingleFive_Returns50Points()
    {
        int[] dice = [5, 2, 3, 4, 6, 2];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(50);
        result.Type.Should().Be(ScoringType.Singles);
        result.ScoringDiceIndices.Should().Contain(0);
    }

    [Test]
    public void Evaluate_TwoOnes_Returns200Points()
    {
        int[] dice = [1, 1, 3, 4, 6, 2];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(200);
        result.Type.Should().Be(ScoringType.Singles);
        result.ScoringDiceIndices.Should().HaveCount(2);
    }

    [Test]
    public void Evaluate_TwoFives_Returns100Points()
    {
        int[] dice = [5, 5, 3, 4, 6, 2];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(100);
        result.Type.Should().Be(ScoringType.Singles);
    }

    [Test]
    public void Evaluate_OneAndFive_Returns150Points()
    {
        // Use 4 dice to avoid straight detection
        int[] dice = [1, 5, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(150);
        result.Type.Should().Be(ScoringType.Singles);
        result.ScoringDiceIndices.Should().HaveCount(2);
    }

    [Test]
    public void Evaluate_TwoOnesAndTwoFives_Returns300Points()
    {
        int[] dice = [1, 1, 5, 5, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(300); // 200 + 100
        result.ScoringDiceIndices.Should().HaveCount(4);
    }

    #endregion

    #region Three of a Kind

    [Test]
    public void Evaluate_ThreeOnes_Returns1000Points()
    {
        int[] dice = [1, 1, 1, 2, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1000);
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
        result.ScoringDiceIndices.Should().HaveCount(3);
    }

    [Test]
    public void Evaluate_ThreeTwos_Returns200Points()
    {
        int[] dice = [2, 2, 2, 3, 4, 6];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(200); // 2 × 100
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void Evaluate_ThreeThrees_Returns300Points()
    {
        int[] dice = [3, 3, 3, 2, 4, 6];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(300);
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void Evaluate_ThreeFours_Returns400Points()
    {
        int[] dice = [4, 4, 4, 2, 3, 6];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(400);
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void Evaluate_ThreeFives_Returns500Points()
    {
        int[] dice = [5, 5, 5, 2, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(500);
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void Evaluate_ThreeSixes_Returns600Points()
    {
        int[] dice = [6, 6, 6, 2, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(600);
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void Evaluate_ThreeTwosWithSingleOne_Returns300Points()
    {
        int[] dice = [2, 2, 2, 1, 4, 6];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(300); // 200 + 100
        result.ScoringDiceIndices.Should().HaveCount(4);
    }

    #endregion

    #region Four of a Kind

    [Test]
    public void Evaluate_FourOnes_Returns2000Points()
    {
        int[] dice = [1, 1, 1, 1, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(2000); // 1000 × 2
        result.Type.Should().Be(ScoringType.FourOfAKind);
        result.ScoringDiceIndices.Should().HaveCount(4);
    }

    [Test]
    public void Evaluate_FourTwos_Returns400Points()
    {
        int[] dice = [2, 2, 2, 2, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(400); // 200 × 2
        result.Type.Should().Be(ScoringType.FourOfAKind);
    }

    [Test]
    public void Evaluate_FourSixes_Returns1200Points()
    {
        int[] dice = [6, 6, 6, 6, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1200); // 600 × 2
        result.Type.Should().Be(ScoringType.FourOfAKind);
    }

    #endregion

    #region Five of a Kind

    [Test]
    public void Evaluate_FiveOnes_Returns4000Points()
    {
        int[] dice = [1, 1, 1, 1, 1, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(4000); // 1000 × 4
        result.Type.Should().Be(ScoringType.FiveOfAKind);
        result.ScoringDiceIndices.Should().HaveCount(5);
    }

    [Test]
    public void Evaluate_FiveThrees_Returns1200Points()
    {
        int[] dice = [3, 3, 3, 3, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1200); // 300 × 4
        result.Type.Should().Be(ScoringType.FiveOfAKind);
    }

    #endregion

    #region Six of a Kind

    [Test]
    public void Evaluate_SixOnes_Returns8000Points()
    {
        int[] dice = [1, 1, 1, 1, 1, 1];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(8000); // 1000 × 8
        result.Type.Should().Be(ScoringType.SixOfAKind);
        result.ScoringDiceIndices.Should().HaveCount(6);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_SixFives_Returns4000Points()
    {
        int[] dice = [5, 5, 5, 5, 5, 5];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(4000); // 500 × 8
        result.Type.Should().Be(ScoringType.SixOfAKind);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_SixSixes_Returns4800Points()
    {
        int[] dice = [6, 6, 6, 6, 6, 6];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(4800); // 600 × 8
        result.Type.Should().Be(ScoringType.SixOfAKind);
        result.IsHotDice.Should().BeTrue();
    }

    #endregion

    #region Straight

    [Test]
    public void Evaluate_Straight_Returns1500Points()
    {
        int[] dice = [1, 2, 3, 4, 5, 6];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1500);
        result.Type.Should().Be(ScoringType.Straight);
        result.ScoringDiceIndices.Should().HaveCount(6);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_StraightScrambled_Returns1500Points()
    {
        int[] dice = [6, 1, 4, 2, 5, 3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1500);
        result.Type.Should().Be(ScoringType.Straight);
    }

    [Test]
    public void Evaluate_NotStraight_DoesNotReturnStraightType()
    {
        int[] dice = [1, 2, 3, 4, 5, 5]; // Missing 6, has two 5s

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Type.Should().NotBe(ScoringType.Straight);
    }

    #endregion

    #region Three Pairs

    [Test]
    public void Evaluate_ThreePairs_Returns1500Points()
    {
        int[] dice = [2, 2, 3, 3, 4, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1500);
        result.Type.Should().Be(ScoringType.ThreePairs);
        result.ScoringDiceIndices.Should().HaveCount(6);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_ThreePairsScrambled_Returns1500Points()
    {
        int[] dice = [4, 2, 3, 2, 4, 3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1500);
        result.Type.Should().Be(ScoringType.ThreePairs);
    }

    [Test]
    public void Evaluate_ThreePairsWithOnesAndFives_Returns1500Points()
    {
        // Even though 1s and 5s could score individually, three pairs is better
        int[] dice = [1, 1, 5, 5, 3, 3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(1500);
        result.Type.Should().Be(ScoringType.ThreePairs);
    }

    [Test]
    public void Evaluate_FourOfKindPlusPair_IsThreePairs()
    {
        // 4 of a kind (2 pairs) + 1 pair = 3 pairs total
        int[] dice = [2, 2, 2, 2, 3, 3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        // This should be treated as three pairs (1500 pts) not four-of-a-kind (400 pts)
        result.Points.Should().Be(1500);
        result.Type.Should().Be(ScoringType.ThreePairs);
    }

    [Test]
    public void Evaluate_SixOfKind_IsAlsoThreePairs_ButSixOfKindScoresHigher()
    {
        // Six of a kind can be seen as 3 pairs, but 6-of-a-kind scoring is higher for most faces
        int[] dice = [2, 2, 2, 2, 2, 2];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        // Six 2s = 200 × 8 = 1600, which is > 1500 (three pairs)
        result.Points.Should().Be(1600);
        result.Type.Should().Be(ScoringType.SixOfAKind);
    }

    #endregion

    #region Hot Dice Detection

    [Test]
    public void Evaluate_AllDiceScore_IsHotDice()
    {
        int[] dice = [1, 1, 1, 5, 5, 5];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.IsHotDice.Should().BeTrue();
        result.NonScoringDiceIndices.Should().BeEmpty();
    }

    [Test]
    public void Evaluate_SomeDiceDoNotScore_IsNotHotDice()
    {
        int[] dice = [1, 1, 1, 2, 3, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.IsHotDice.Should().BeFalse();
        result.NonScoringDiceIndices.Should().HaveCount(3);
    }

    #endregion

    #region Helper Method Tests

    [Test]
    public void IsStraight_ValidStraight_ReturnsTrue()
    {
        AetharnScorer.IsStraight([1, 2, 3, 4, 5, 6]).Should().BeTrue();
    }

    [Test]
    public void IsStraight_ScrambledStraight_ReturnsTrue()
    {
        AetharnScorer.IsStraight([6, 5, 4, 3, 2, 1]).Should().BeTrue();
    }

    [Test]
    public void IsStraight_WrongCount_ReturnsFalse()
    {
        AetharnScorer.IsStraight([1, 2, 3, 4, 5]).Should().BeFalse();
    }

    [Test]
    public void IsStraight_Duplicates_ReturnsFalse()
    {
        AetharnScorer.IsStraight([1, 2, 3, 4, 5, 5]).Should().BeFalse();
    }

    [Test]
    public void IsThreePairs_ValidThreePairs_ReturnsTrue()
    {
        AetharnScorer.IsThreePairs([1, 1, 2, 2, 3, 3]).Should().BeTrue();
    }

    [Test]
    public void IsThreePairs_FourPlusTwo_ReturnsTrue()
    {
        // 4+2 = 2 pairs + 1 pair = 3 pairs
        AetharnScorer.IsThreePairs([1, 1, 1, 1, 2, 2]).Should().BeTrue();
    }

    [Test]
    public void IsThreePairs_SixOfKind_ReturnsTrue()
    {
        // 6 of a kind = 3 pairs
        AetharnScorer.IsThreePairs([3, 3, 3, 3, 3, 3]).Should().BeTrue();
    }

    [Test]
    public void IsThreePairs_WrongCount_ReturnsFalse()
    {
        AetharnScorer.IsThreePairs([1, 1, 2, 2, 3]).Should().BeFalse();
    }

    [Test]
    public void IsThreePairs_NotEnoughPairs_ReturnsFalse()
    {
        AetharnScorer.IsThreePairs([1, 1, 2, 3, 4, 5]).Should().BeFalse();
    }

    [Test]
    public void CalculateThreeOfAKindValue_Ones_Returns1000()
    {
        AetharnScorer.CalculateThreeOfAKindValue(1).Should().Be(1000);
    }

    [TestCase(2, 200)]
    [TestCase(3, 300)]
    [TestCase(4, 400)]
    [TestCase(5, 500)]
    [TestCase(6, 600)]
    public void CalculateThreeOfAKindValue_NonOnes_ReturnsFaceTimesFundred(int face, int expected)
    {
        AetharnScorer.CalculateThreeOfAKindValue(face).Should().Be(expected);
    }

    #endregion

    #region Fewer Than 6 Dice (Re-rolls)

    [Test]
    public void Evaluate_ThreeDiceWithOne_Returns100Points()
    {
        int[] dice = [1, 2, 3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(100);
        result.ScoringDiceIndices.Should().ContainSingle().Which.Should().Be(0);
    }

    [Test]
    public void Evaluate_ThreeDiceAllSame_ReturnsThreeOfAKind()
    {
        int[] dice = [4, 4, 4];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(400);
        result.Type.Should().Be(ScoringType.ThreeOfAKind);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_OneDie_One_Returns100()
    {
        int[] dice = [1];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(100);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_OneDie_Five_Returns50()
    {
        int[] dice = [5];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.Points.Should().Be(50);
        result.IsHotDice.Should().BeTrue();
    }

    [Test]
    public void Evaluate_OneDie_NonScoring_ReturnsBust()
    {
        int[] dice = [3];

        ScoringResult result = AetharnScorer.Evaluate(dice);

        result.IsBust.Should().BeTrue();
    }

    #endregion

    #region ScoringResult Properties

    [Test]
    public void ScoringResult_Bust_HasCorrectProperties()
    {
        ScoringResult bust = ScoringResult.Bust;

        bust.Points.Should().Be(0);
        bust.IsBust.Should().BeTrue();
        bust.IsHotDice.Should().BeFalse();
        bust.Type.Should().Be(ScoringType.None);
        bust.ScoringDiceIndices.Should().BeEmpty();
        bust.NonScoringDiceIndices.Should().BeEmpty();
    }

    #endregion
}
