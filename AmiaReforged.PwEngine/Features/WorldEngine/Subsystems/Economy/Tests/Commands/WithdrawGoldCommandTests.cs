using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Commands;

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
        int amount = 500;
        string reason = "Withdrawing earnings";

        // When
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

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
        int amount = 0;
        string reason = "Zero withdrawal test";

        // When
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Amount.Value, Is.EqualTo(0));
    }

    [Test]
    public void Given_LargeAmount_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        int amount = 999999;
        string reason = "Large withdrawal";

        // When
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

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
        int amount = -100;
        string reason = "Invalid withdrawal";

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    [Test]
    public void Given_EmptyReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        int amount = 100;
        string reason = "";

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    [Test]
    public void Given_TooShortReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        int amount = 100;
        string reason = "ab"; // Too short (< 3 chars)

        // When/Then
        Assert.Throws<ArgumentException>(() =>
            WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason));
    }

    [Test]
    public void Given_TooLongReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        int amount = 100;
        string reason = new string('x', 201); // Too long (> 200 chars)

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
        int amount = 100;
        string reason = "  Valid reason  ";

        // When
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value, Is.EqualTo("Valid reason"));
    }

    [Test]
    public void Given_MinimumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        int amount = 100;
        string reason = "abc"; // Exactly 3 characters

        // When
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value, Is.EqualTo(reason));
    }

    [Test]
    public void Given_MaximumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        int amount = 100;
        string reason = new string('x', 200); // Exactly 200 characters

        // When
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_persona, _coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value.Length, Is.EqualTo(200));
    }

    #endregion
}

