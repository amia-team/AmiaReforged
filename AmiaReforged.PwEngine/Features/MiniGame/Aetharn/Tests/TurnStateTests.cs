using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Tests;

[TestFixture]
public class TurnStateTests
{
    private const string TestPlayerKey = "test_player_key";

    #region StartTurn

    [Test]
    public void StartTurn_WithValidInputs_CreatesCorrectInitialState()
    {
        int[] diceValues = [1, 2, 3, 4, 5, 6];

        TurnState state = TurnState.StartTurn(TestPlayerKey, diceValues);

        state.PlayerKey.Should().Be(TestPlayerKey);
        state.Dice.Should().HaveCount(6);
        state.AccumulatedPoints.Should().Be(0);
        state.HasUsedHotDice.Should().BeFalse();
        state.RollCount.Should().Be(1);
    }

    [Test]
    public void StartTurn_AllDiceAreNotHeld()
    {
        int[] diceValues = [1, 2, 3, 4, 5, 6];

        TurnState state = TurnState.StartTurn(TestPlayerKey, diceValues);

        state.Dice.Should().AllSatisfy(d => d.IsHeld.Should().BeFalse());
        state.HeldDice.Should().BeEmpty();
        state.RemainingDice.Should().HaveCount(6);
    }

    [Test]
    public void StartTurn_PreservesDiceValues()
    {
        int[] diceValues = [1, 5, 3, 3, 3, 2];

        TurnState state = TurnState.StartTurn(TestPlayerKey, diceValues);

        state.Dice.Select(d => d.Value).Should().BeEquivalentTo(diceValues);
    }

    [Test]
    public void StartTurn_NullPlayerKey_ThrowsArgumentNullException()
    {
        Action act = () => TurnState.StartTurn(null!, [1, 2, 3, 4, 5, 6]);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void StartTurn_NullDiceValues_ThrowsArgumentNullException()
    {
        Action act = () => TurnState.StartTurn(TestPlayerKey, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region HoldDice - Basic Functionality

    [Test]
    public void HoldDice_SingleOne_AddsPointsAndMarksHeld()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 6, 2]);

        HoldResult result = state.HoldDice([0]); // Hold the 1

        result.IsSuccess.Should().BeTrue();
        result.NewState!.AccumulatedPoints.Should().Be(100);
        result.NewState.Dice[0].IsHeld.Should().BeTrue();
        result.NewState.HeldDice.Should().HaveCount(1);
        result.NewState.RemainingDice.Should().HaveCount(5);
    }

    [Test]
    public void HoldDice_SingleFive_AddsPointsAndMarksHeld()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [5, 2, 3, 4, 6, 2]);

        HoldResult result = state.HoldDice([0]); // Hold the 5

        result.IsSuccess.Should().BeTrue();
        result.NewState!.AccumulatedPoints.Should().Be(50);
        result.NewState.Dice[0].IsHeld.Should().BeTrue();
    }

    [Test]
    public void HoldDice_ThreeOfAKind_AddsBonusPoints()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [3, 3, 3, 2, 4, 6]);

        HoldResult result = state.HoldDice([0, 1, 2]); // Hold the three 3s

        result.IsSuccess.Should().BeTrue();
        result.NewState!.AccumulatedPoints.Should().Be(300); // 3 * 100
        result.ScoringResult!.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void HoldDice_ThreeOnes_Adds1000Points()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 2, 4, 6]);

        HoldResult result = state.HoldDice([0, 1, 2]); // Hold the three 1s

        result.IsSuccess.Should().BeTrue();
        result.NewState!.AccumulatedPoints.Should().Be(1000);
    }

    [Test]
    public void HoldDice_MultipleScoringDice_AccumulatesPoints()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 5, 2, 3, 4, 6]);

        HoldResult result = state.HoldDice([0, 1]); // Hold 1 and 5

        result.IsSuccess.Should().BeTrue();
        result.NewState!.AccumulatedPoints.Should().Be(150); // 100 + 50
    }

    [Test]
    public void HoldDice_PreservesOtherDiceState()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        HoldResult result = state.HoldDice([0]); // Hold the 1

        result.NewState!.Dice[1].Value.Should().Be(2);
        result.NewState.Dice[1].IsHeld.Should().BeFalse();
        result.NewState.Dice[4].Value.Should().Be(5);
        result.NewState.Dice[4].IsHeld.Should().BeFalse();
    }

    [Test]
    public void HoldDice_IsImmutable_OriginalStateUnchanged()
    {
        TurnState originalState = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        HoldResult result = originalState.HoldDice([0]);

        originalState.AccumulatedPoints.Should().Be(0);
        originalState.Dice[0].IsHeld.Should().BeFalse();
        result.NewState!.AccumulatedPoints.Should().Be(100);
    }

    #endregion

    #region HoldDice - Sequential Holds

    [Test]
    public void HoldDice_SequentialHolds_AccumulatesPointsCorrectly()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 5, 3, 3, 3, 2]);

        // First hold: the 1
        HoldResult firstHold = state.HoldDice([0]);
        firstHold.NewState!.AccumulatedPoints.Should().Be(100);

        // Simulate rolling remaining dice and getting a 5
        RollResult rollResult = firstHold.NewState.Roll([5, 2, 4, 6, 3]);
        
        // Second hold: the new 5 (now at index 1 in the dice array)
        HoldResult secondHold = rollResult.NewState!.HoldDice([1]);
        secondHold.NewState!.AccumulatedPoints.Should().Be(150); // 100 + 50
    }

    [Test]
    public void HoldDice_CannotHoldAlreadyHeldDie()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 5, 3, 4, 6, 2]);
        HoldResult firstHold = state.HoldDice([0]); // Hold the 1

        HoldResult secondHold = firstHold.NewState!.HoldDice([0]); // Try to hold again

        secondHold.IsSuccess.Should().BeFalse();
        secondHold.ErrorMessage.Should().Contain("already held");
    }

    #endregion

    #region HoldDice - Validation Errors

    [Test]
    public void HoldDice_EmptyArray_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        HoldResult result = state.HoldDice([]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("at least one die");
    }

    [Test]
    public void HoldDice_NullArray_ThrowsArgumentNullException()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        Action act = () => state.HoldDice(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void HoldDice_IndexOutOfRange_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        HoldResult result = state.HoldDice([10]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid die index");
    }

    [Test]
    public void HoldDice_NegativeIndex_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        HoldResult result = state.HoldDice([-1]);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid die index");
    }

    [Test]
    public void HoldDice_NonScoringDice_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [2, 3, 4, 6, 2, 4]);

        HoldResult result = state.HoldDice([0]); // Try to hold a 2

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("valid scoring combination");
    }

    [Test]
    public void HoldDice_ScoringWithNonScoring_StillScoresTheValidDice()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 6, 2]);

        // Holding [1, 2] together - the scorer evaluates and finds the 1 scores 100
        // The 2 doesn't contribute but the overall result is still valid
        HoldResult result = state.HoldDice([0, 1]);

        // This is actually valid - the scorer finds the 1 and scores 100
        result.IsSuccess.Should().BeTrue();
        result.NewState!.AccumulatedPoints.Should().Be(100);
    }

    #endregion

    #region Roll - Basic Functionality

    [Test]
    public void Roll_WithRemainingDice_UpdatesDiceValues()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]); // Hold the 1

        RollResult rollResult = holdResult.NewState!.Roll([3, 3, 3, 2, 4]);

        rollResult.IsSuccess.Should().BeTrue();
        rollResult.NewState!.Dice[0].Value.Should().Be(1); // Held die preserved
        rollResult.NewState.Dice[0].IsHeld.Should().BeTrue();
        rollResult.NewState.Dice[1].Value.Should().Be(3); // New value
        rollResult.NewState.Dice[1].IsHeld.Should().BeFalse();
    }

    [Test]
    public void Roll_IncrementsRollCount()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]);

        RollResult rollResult = holdResult.NewState!.Roll([2, 3, 4, 5, 6]);

        rollResult.NewState!.RollCount.Should().Be(2);
    }

    [Test]
    public void Roll_PreservesAccumulatedPoints()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]); // 100 points

        RollResult rollResult = holdResult.NewState!.Roll([2, 3, 4, 6, 2]);

        rollResult.NewState!.AccumulatedPoints.Should().Be(100);
    }

    [Test]
    public void Roll_PreservesHeldDice()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 5, 2, 3, 4, 6]);
        HoldResult holdResult = state.HoldDice([0, 1]); // Hold 1 and 5

        RollResult rollResult = holdResult.NewState!.Roll([3, 3, 3, 2]);

        rollResult.NewState!.HeldDice.Should().HaveCount(2);
        rollResult.NewState.HeldDice[0].Value.Should().Be(1);
        rollResult.NewState.HeldDice[1].Value.Should().Be(5);
    }

    [Test]
    public void Roll_ReturnsEvaluatedScoringResult()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]);

        RollResult rollResult = holdResult.NewState!.Roll([3, 3, 3, 2, 4]); // Three 3s

        rollResult.ScoringResult!.Points.Should().Be(300);
        rollResult.ScoringResult.Type.Should().Be(ScoringType.ThreeOfAKind);
    }

    [Test]
    public void Roll_Bust_SetsBustFlag()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]);

        RollResult rollResult = holdResult.NewState!.Roll([2, 3, 4, 6, 2]); // No scoring dice

        rollResult.IsBust.Should().BeTrue();
        rollResult.ScoringResult!.IsBust.Should().BeTrue();
    }

    #endregion

    #region Roll - Validation Errors

    [Test]
    public void Roll_NoDiceRemaining_ReturnsFailure()
    {
        // Create a state where all dice are held
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]); // Hold all

        RollResult rollResult = holdResult.NewState!.Roll([]);

        rollResult.IsSuccess.Should().BeFalse();
        rollResult.ErrorMessage.Should().Contain("No dice remaining");
    }

    [Test]
    public void Roll_WrongNumberOfDice_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]); // 5 dice remaining

        RollResult rollResult = holdResult.NewState!.Roll([1, 2, 3]); // Only 3 values

        rollResult.IsSuccess.Should().BeFalse();
        rollResult.ErrorMessage.Should().Contain("Expected 5 dice values");
    }

    [Test]
    public void Roll_NullValues_ThrowsArgumentNullException()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]);

        Action act = () => holdResult.NewState!.Roll(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Hot Dice

    [Test]
    public void UseHotDice_AllDiceHeld_ResetsAllDice()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]); // Hold all (Hot Dice!)

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        hotDiceResult.IsSuccess.Should().BeTrue();
        hotDiceResult.NewState!.Dice.Should().HaveCount(6);
        hotDiceResult.NewState.Dice.Should().AllSatisfy(d => d.IsHeld.Should().BeFalse());
    }

    [Test]
    public void UseHotDice_PreservesAccumulatedPoints()
    {
        // Use [1,1,1,5,5,5] which scores as three pairs = 1500 points
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]); // Three pairs = 1500

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([2, 3, 4, 6, 2, 4]); // Bust roll

        hotDiceResult.NewState!.AccumulatedPoints.Should().Be(1500);
    }

    [Test]
    public void UseHotDice_SetsHasUsedHotDiceFlag()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        hotDiceResult.NewState!.HasUsedHotDice.Should().BeTrue();
    }

    [Test]
    public void UseHotDice_IncrementsRollCount()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        hotDiceResult.NewState!.RollCount.Should().Be(2);
    }

    [Test]
    public void UseHotDice_ReturnsNewScoringResult()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);

        // Roll a straight after Hot Dice
        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        hotDiceResult.ScoringResult!.Type.Should().Be(ScoringType.Straight);
        hotDiceResult.ScoringResult.Points.Should().Be(1500);
    }

    [Test]
    public void UseHotDice_Bust_SetsBustFlag()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([2, 2, 3, 3, 4, 6]);

        hotDiceResult.IsBust.Should().BeTrue();
    }

    [Test]
    public void UseHotDice_AlreadyUsed_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);
        HotDiceResult firstHotDice = holdResult.NewState!.UseHotDice([1, 1, 1, 5, 5, 5]);

        // Hold all again and try to use Hot Dice a second time
        HoldResult secondHold = firstHotDice.NewState!.HoldDice([0, 1, 2, 3, 4, 5]);
        HotDiceResult secondHotDice = secondHold.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        secondHotDice.IsSuccess.Should().BeFalse();
        secondHotDice.ErrorMessage.Should().Contain("already been used");
    }

    [Test]
    public void UseHotDice_NotAllDiceHeld_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]); // Only hold the 1

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        hotDiceResult.IsSuccess.Should().BeFalse();
        hotDiceResult.ErrorMessage.Should().Contain("all dice are held");
    }

    [Test]
    public void UseHotDice_WrongNumberOfDice_ReturnsFailure()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);

        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3]); // Only 3 dice

        hotDiceResult.IsSuccess.Should().BeFalse();
        hotDiceResult.ErrorMessage.Should().Contain("exactly 6 dice values");
    }

    #endregion

    #region AllDiceHeld Property

    [Test]
    public void AllDiceHeld_NoDiceHeld_ReturnsFalse()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);

        state.AllDiceHeld.Should().BeFalse();
    }

    [Test]
    public void AllDiceHeld_SomeDiceHeld_ReturnsFalse()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        HoldResult holdResult = state.HoldDice([0]);

        holdResult.NewState!.AllDiceHeld.Should().BeFalse();
    }

    [Test]
    public void AllDiceHeld_AllSixDiceHeld_ReturnsTrue()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);

        holdResult.NewState!.AllDiceHeld.Should().BeTrue();
    }

    #endregion

    #region Reset

    [Test]
    public void Reset_ClearsAllStateExceptPlayerKey()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 1, 1, 5, 5, 5]);
        HoldResult holdResult = state.HoldDice([0, 1, 2, 3, 4, 5]);
        HotDiceResult hotDiceResult = holdResult.NewState!.UseHotDice([1, 2, 3, 4, 5, 6]);

        TurnState resetState = hotDiceResult.NewState!.Reset([2, 3, 4, 6, 2, 4]);

        resetState.PlayerKey.Should().Be(TestPlayerKey);
        resetState.AccumulatedPoints.Should().Be(0);
        resetState.HasUsedHotDice.Should().BeFalse();
        resetState.RollCount.Should().Be(1);
        resetState.Dice.Should().AllSatisfy(d => d.IsHeld.Should().BeFalse());
    }

    [Test]
    public void Reset_SetsNewDiceValues()
    {
        TurnState state = TurnState.StartTurn(TestPlayerKey, [1, 2, 3, 4, 5, 6]);
        int[] newValues = [6, 5, 4, 3, 2, 1];

        TurnState resetState = state.Reset(newValues);

        resetState.Dice.Select(d => d.Value).Should().BeEquivalentTo(newValues);
    }

    #endregion

    #region Die Record

    [Test]
    public void Die_Hold_ReturnsNewDieWithIsHeldTrue()
    {
        Die die = new(5, false);

        Die heldDie = die.Hold();

        heldDie.Value.Should().Be(5);
        heldDie.IsHeld.Should().BeTrue();
        die.IsHeld.Should().BeFalse(); // Original unchanged
    }

    [Test]
    public void Die_DefaultIsHeld_IsFalse()
    {
        Die die = new(3);

        die.IsHeld.Should().BeFalse();
    }

    [Test]
    public void Die_Equality_WorksCorrectly()
    {
        Die die1 = new(5, true);
        Die die2 = new(5, true);
        Die die3 = new(5, false);

        die1.Should().Be(die2);
        die1.Should().NotBe(die3);
    }

    #endregion
}
