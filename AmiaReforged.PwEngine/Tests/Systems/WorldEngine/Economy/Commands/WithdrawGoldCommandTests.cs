using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Commands;

/// <summary>
/// BDD-style tests for WithdrawGoldCommand.
/// Tests command creation and validation through the factory method.
/// </summary>
[TestFixture]
public class WithdrawGoldCommandTests
{
    private PersonaId _persona;
    private CoinhouseTag _coinhouse;

    [SetUp]
    public void Setup()
    {
        _persona = PersonaTestHelpers.CreateCharacterPersona().Id;
        _coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
    }

    #region Happy Path Tests

    [Test]
    public void Given_ValidInputs_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        var amount = 500;
        var reason = "Withdrawing earnings";

        // When
        var command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.PersonaId, Is.EqualTo(_persona));
        Assert.That(command.Coinhouse, Is.EqualTo(_coinhouse));
        Assert.That(command.Amount.Value, Is.EqualTo(amount));
        Assert.That(command.Reason.Value, Is.EqualTo(reason));
    }

    [Test]
    public void Given_ZeroAmount_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        var amount = 0;
        var reason = "Zero withdrawal test";

        // When
        var command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Amount.Value, Is.EqualTo(0));
    }

    [Test]
    public void Given_LargeAmount_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        var amount = 999999;
        var reason = "Large withdrawal";

        // When
        var command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Amount.Value, Is.EqualTo(amount));
    }

    #endregion

    #region Validation Tests

    [Test]
    public void Given_NegativeAmount_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var amount = -100;
        var reason = "Invalid withdrawal";

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    [Test]
    public void Given_EmptyReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var amount = 100;
        var reason = "";

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    [Test]
    public void Given_TooShortReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var amount = 100;
        var reason = "ab"; // Too short (< 3 chars)

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    [Test]
    public void Given_TooLongReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var amount = 100;
        var reason = new string('x', 201); // Too long (> 200 chars)

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Given_ReasonWithLeadingAndTrailingSpaces_When_CreatingCommand_Then_ReasonIsTrimmed()
    {
        // Given
        var amount = 100;
        var reason = "  Valid reason  ";

        // When
        var command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value, Is.EqualTo("Valid reason"));
    }

    [Test]
    public void Given_MinimumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        var amount = 100;
        var reason = "abc"; // Exactly 3 characters

        // When
        var command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value, Is.EqualTo(reason));
    }

    [Test]
    public void Given_MaximumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        var amount = 100;
        var reason = new string('x', 200); // Exactly 200 characters

        // When
        var command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value.Length, Is.EqualTo(200));
    }

    #endregion
}

