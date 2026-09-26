using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// Direct coverage for the TraitBudget value-object operations.
/// </summary>
[TestFixture]
public class TraitBudgetTests
{
    [Test]
    public void CanAfford_WhenCostEqualsAvailablePoints_ReturnsTrue()
    {
        // Arrange: EarnedPoints = 1, SpentPoints = 1
        // => TotalPoints = 3 (2 base + 1 earned), AvailablePoints = 2
        TraitBudget budget = new TraitBudget
        {
            EarnedPoints = 1,
            SpentPoints = 1
        };

        Assert.That(budget.TotalPoints, Is.EqualTo(3));
        Assert.That(budget.AvailablePoints, Is.EqualTo(2));

        // Act & Assert
        Assert.That(budget.CanAfford(2), Is.True);
    }

    [Test]
    public void CanAfford_WhenCostExceedsAvailablePoints_ReturnsFalse()
    {
        // Arrange: same budget as CanAfford_WhenCostEqualsAvailablePoints_ReturnsTrue
        TraitBudget budget = new TraitBudget
        {
            EarnedPoints = 1,
            SpentPoints = 1
        };

        // Act & Assert
        Assert.That(budget.CanAfford(3), Is.False);
    }

    [Test]
    public void WithSpentPoints_ReplacesSpentPointsAndPreservesEarnedPoints()
    {
        // Arrange: EarnedPoints = 3, SpentPoints = 1
        TraitBudget budget = new TraitBudget
        {
            EarnedPoints = 3,
            SpentPoints = 1
        };

        // Act
        TraitBudget result = budget.WithSpentPoints(4);

        // Assert - returned budget
        Assert.That(result.EarnedPoints, Is.EqualTo(3));
        Assert.That(result.SpentPoints, Is.EqualTo(4));

        // Assert - original remains unchanged
        Assert.That(budget.EarnedPoints, Is.EqualTo(3));
        Assert.That(budget.SpentPoints, Is.EqualTo(1));
    }

    [Test]
    public void AfterSpending_AddsToExistingSpentPointsAndPreservesEarnedPoints()
    {
        // Arrange: EarnedPoints = 2, SpentPoints = 1
        TraitBudget budget = new TraitBudget
        {
            EarnedPoints = 2,
            SpentPoints = 1
        };

        // Act
        TraitBudget result = budget.AfterSpending(3);

        // Assert - returned budget
        Assert.That(result.EarnedPoints, Is.EqualTo(2));
        Assert.That(result.SpentPoints, Is.EqualTo(4));

        // Assert - original remains unchanged
        Assert.That(budget.EarnedPoints, Is.EqualTo(2));
        Assert.That(budget.SpentPoints, Is.EqualTo(1));
    }

    [Test]
    public void WithEarnedPoints_AddsToExistingEarnedPointsAndPreservesSpentPoints()
    {
        // Arrange: EarnedPoints = 2, SpentPoints = 1
        TraitBudget budget = new TraitBudget
        {
            EarnedPoints = 2,
            SpentPoints = 1
        };

        // Act
        TraitBudget result = budget.WithEarnedPoints(3);

        // Assert - returned budget
        Assert.That(result.EarnedPoints, Is.EqualTo(5));
        Assert.That(result.SpentPoints, Is.EqualTo(1));

        // Assert - original remains unchanged
        Assert.That(budget.EarnedPoints, Is.EqualTo(2));
        Assert.That(budget.SpentPoints, Is.EqualTo(1));
    }
}
