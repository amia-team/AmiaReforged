using FluentAssertions;
using NUnit.Framework;

namespace WorldSimulator.Tests.Domain.ValueObjects;

/// <summary>
/// Tests for domain value objects.
/// Each test demonstrates validation at construction time (Parse, Don't Validate).
/// </summary>
[TestFixture]
public class DomainValueTests
{
    #region TurnDate Tests

    [Test]
    public void TurnDate_Constructor_ValidTimestamp_CreatesValidTurnDate()
    {
        // Arrange
        DateTimeOffset timestamp = new DateTimeOffset(2025, 10, 29, 14, 30, 0, TimeSpan.Zero);

        // Act
        TurnDate turnDate = new TurnDate(timestamp);

        // Assert
        turnDate.Value.Should().Be(timestamp);
        turnDate.Year.Should().Be(2025);
        turnDate.Month.Should().Be(10);
        turnDate.Day.Should().Be(29);
        turnDate.Hour.Should().Be(14);
        turnDate.Minute.Should().Be(30);
    }

    [Test]
    public void TurnDate_Parse_ValidString_CreatesTurnDate()
    {
        // Arrange
        string input = "2025-10-29T14:30:00Z";

        // Act
        TurnDate turnDate = TurnDate.Parse(input);

        // Assert
        turnDate.Value.Year.Should().Be(2025);
        turnDate.Value.Month.Should().Be(10);
        turnDate.Value.Day.Should().Be(29);
    }

    [Test]
    public void TurnDate_Next_ReturnsNextTurnDate()
    {
        // Arrange
        TurnDate turnDate = new TurnDate(new DateTimeOffset(2025, 10, 29, 14, 30, 0, TimeSpan.Zero));

        // Act
        TurnDate next = turnDate.Next();  // Default 10 minutes

        // Assert
        next.Value.Should().Be(new DateTimeOffset(2025, 10, 29, 14, 40, 0, TimeSpan.Zero));
    }

    [Test]
    public void TurnDate_Next_CustomInterval_ReturnsCorrectTurnDate()
    {
        // Arrange
        TurnDate turnDate = new TurnDate(new DateTimeOffset(2025, 10, 29, 14, 30, 0, TimeSpan.Zero));

        // Act
        TurnDate next = turnDate.Next(15);  // 15 minutes ahead

        // Assert
        next.Value.Should().Be(new DateTimeOffset(2025, 10, 29, 14, 45, 0, TimeSpan.Zero));
    }

    [Test]
    public void TurnDate_Previous_ReturnsPreviousTurnDate()
    {
        // Arrange
        TurnDate turnDate = new TurnDate(new DateTimeOffset(2025, 10, 29, 14, 30, 0, TimeSpan.Zero));

        // Act
        TurnDate previous = turnDate.Previous();  // Default 10 minutes

        // Assert
        previous.Value.Should().Be(new DateTimeOffset(2025, 10, 29, 14, 20, 0, TimeSpan.Zero));
    }

    [Test]
    public void TurnDate_Now_ReturnsCurrentTimestamp()
    {
        // Act
        TurnDate turnDate = TurnDate.Now();

        // Assert
        turnDate.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region InfluenceAmount Tests

    [Test]
    public void InfluenceAmount_Constructor_ValidValue_CreatesInstance()
    {
        // Act
        InfluenceAmount influence = new InfluenceAmount(100);

        // Assert
        influence.Value.Should().Be(100);
    }

    [Test]
    public void InfluenceAmount_Constructor_Negative_ThrowsArgumentException()
    {
        // Act & Assert
        Func<InfluenceAmount> act = () => new InfluenceAmount(-10);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Influence cannot be negative*");
    }

    [Test]
    public void InfluenceAmount_Addition_ReturnsSum()
    {
        // Arrange
        InfluenceAmount a = new InfluenceAmount(50);
        InfluenceAmount b = new InfluenceAmount(30);

        // Act
        InfluenceAmount result = a + b;

        // Assert
        result.Value.Should().Be(80);
    }

    [Test]
    public void InfluenceAmount_Subtraction_ReturnsCorrectValue()
    {
        // Arrange
        InfluenceAmount a = new InfluenceAmount(100);
        InfluenceAmount b = new InfluenceAmount(30);

        // Act
        InfluenceAmount result = a - b;

        // Assert
        result.Value.Should().Be(70);
    }

    [Test]
    public void InfluenceAmount_Subtraction_Underflow_ReturnsZero()
    {
        // Arrange
        InfluenceAmount a = new InfluenceAmount(20);
        InfluenceAmount b = new InfluenceAmount(50);

        // Act
        InfluenceAmount result = a - b;

        // Assert
        result.Value.Should().Be(0);  // Underflow protection!
    }

    [Test]
    public void InfluenceAmount_CanAfford_SufficientFunds_ReturnsTrue()
    {
        // Arrange
        InfluenceAmount balance = new InfluenceAmount(100);
        InfluenceAmount cost = new InfluenceAmount(50);

        // Act
        bool canAfford = balance.CanAfford(cost);

        // Assert
        canAfford.Should().BeTrue();
    }

    [Test]
    public void InfluenceAmount_CanAfford_InsufficientFunds_ReturnsFalse()
    {
        // Arrange
        InfluenceAmount balance = new InfluenceAmount(30);
        InfluenceAmount cost = new InfluenceAmount(50);

        // Act
        bool canAfford = balance.CanAfford(cost);

        // Assert
        canAfford.Should().BeFalse();
    }

    #endregion

    #region DemandSignal Tests

    [Test]
    public void DemandSignal_Constructor_ValidMultiplier_CreatesInstance()
    {
        // Act
        DemandSignal demand = new DemandSignal(1.5m);

        // Assert
        demand.Multiplier.Should().Be(1.5m);
    }

    [Test]
    public void DemandSignal_Constructor_TooLow_ThrowsArgumentException()
    {
        // Act & Assert
        Func<DemandSignal> act = () => new DemandSignal(0.05m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be between 0.1 and 10.0*");
    }

    [Test]
    public void DemandSignal_Constructor_TooHigh_ThrowsArgumentException()
    {
        // Act & Assert
        Func<DemandSignal> act = () => new DemandSignal(15.0m);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be between 0.1 and 10.0*");
    }

    [Test]
    public void DemandSignal_IsHigh_HighMultiplier_ReturnsTrue()
    {
        // Arrange
        DemandSignal demand = new DemandSignal(2.0m);

        // Assert
        demand.IsHigh.Should().BeTrue();
        demand.IsLow.Should().BeFalse();
        demand.IsNormal.Should().BeFalse();
    }

    [Test]
    public void DemandSignal_IsNormal_NormalMultiplier_ReturnsTrue()
    {
        // Arrange
        DemandSignal demand = DemandSignal.Normal;

        // Assert
        demand.IsNormal.Should().BeTrue();
        demand.IsHigh.Should().BeFalse();
        demand.IsLow.Should().BeFalse();
    }

    #endregion

    #region CivicScore Tests

    [Test]
    public void CivicScore_Constructor_ValidValue_CreatesInstance()
    {
        // Act
        CivicScore score = new CivicScore(75);

        // Assert
        score.Value.Should().Be(75);
    }

    [Test]
    public void CivicScore_Constructor_Negative_ThrowsArgumentException()
    {
        // Act & Assert
        Func<CivicScore> act = () => new CivicScore(-10);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be between 0 and 100*");
    }

    [Test]
    public void CivicScore_Constructor_Over100_ThrowsArgumentException()
    {
        // Act & Assert
        Func<CivicScore> act = () => new CivicScore(150);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be between 0 and 100*");
    }

    [Test]
    public void CivicScore_Addition_ClampsAt100()
    {
        // Arrange
        CivicScore score = new CivicScore(90);

        // Act
        CivicScore result = score + 20;

        // Assert
        result.Value.Should().Be(100);  // Clamped!
    }

    [Test]
    public void CivicScore_Subtraction_ClampsAtZero()
    {
        // Arrange
        CivicScore score = new CivicScore(10);

        // Act
        CivicScore result = score - 20;

        // Assert
        result.Value.Should().Be(0);  // Clamped!
    }

    [Test]
    public void CivicScore_IsExcellent_HighScore_ReturnsTrue()
    {
        // Arrange
        CivicScore score = new CivicScore(95);

        // Assert
        score.IsExcellent.Should().BeTrue();
        score.IsGood.Should().BeTrue();
        score.IsCritical.Should().BeFalse();
    }

    [Test]
    public void CivicScore_IsCritical_LowScore_ReturnsTrue()
    {
        // Arrange
        CivicScore score = new CivicScore(15);

        // Assert
        score.IsCritical.Should().BeTrue();
        score.IsLow.Should().BeTrue();
        score.IsGood.Should().BeFalse();
    }

    #endregion

    #region Population Tests

    [Test]
    public void Population_Constructor_ValidValue_CreatesInstance()
    {
        // Act
        Population pop = new Population(5000);

        // Assert
        pop.Value.Should().Be(5000);
    }

    [Test]
    public void Population_Constructor_Negative_ThrowsArgumentException()
    {
        // Act & Assert
        Func<Population> act = () => new Population(-100);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Population cannot be negative*");
    }

    [Test]
    public void Population_GrowBy_PositiveRate_IncreasesPopulation()
    {
        // Arrange
        Population pop = new Population(1000);

        // Act
        Population result = pop.GrowBy(0.05m);  // 5% growth

        // Assert
        result.Value.Should().Be(1050);
    }

    [Test]
    public void Population_GrowBy_NegativeRate_DecreasesPopulation()
    {
        // Arrange
        Population pop = new Population(1000);

        // Act
        Population result = pop.GrowBy(-0.10m);  // 10% decline

        // Assert
        result.Value.Should().Be(900);
    }

    #endregion
}

