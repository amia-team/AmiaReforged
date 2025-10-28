using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Commands;

/// <summary>
/// BDD-style tests for DepositGoldCommand.
/// Following Given-When-Then pattern for clarity and readability.
/// </summary>
[TestFixture]
public class DepositGoldCommandTests
{
    #region Happy Path Tests

    [Test]
    public void Given_ValidInputs_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 500;
        var reason = "Depositing earnings";

        // When
        var command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.PersonaId, Is.EqualTo(personaId));
        Assert.That(command.Coinhouse, Is.EqualTo(coinhouse));
        Assert.That(command.Amount.Value, Is.EqualTo(amount));
        Assert.That(command.Reason.Value, Is.EqualTo(reason));
    }

    [Test]
    public void Given_ZeroAmount_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 0;
        var reason = "Zero deposit test";

        // When
        var command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Amount.Value, Is.EqualTo(0));
    }

    [Test]
    public void Given_LargeAmount_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 1_000_000;
        var reason = "Large deposit";

        // When
        var command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

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
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = -100;
        var reason = "Invalid deposit";

        // When & Then
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("cannot be negative"));
    }

    [Test]
    public void Given_EmptyReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 100;
        var reason = "";

        // When & Then
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("cannot be empty"));
    }

    [Test]
    public void Given_TooShortReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 100;
        var reason = "Ab"; // Less than 3 characters

        // When & Then
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("at least 3 characters"));
    }

    [Test]
    public void Given_TooLongReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 100;
        var reason = new string('A', 201); // More than 200 characters

        // When & Then
        var ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("cannot exceed 200 characters"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Given_ReasonWithLeadingAndTrailingSpaces_When_CreatingCommand_Then_ReasonIsTrimmed()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 100;
        var reason = "  Deposit with spaces  ";

        // When
        var command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value, Is.EqualTo("Deposit with spaces"));
    }

    [Test]
    public void Given_MinimumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 100;
        var reason = "ABC"; // Exactly 3 characters

        // When
        var command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Reason.Value, Is.EqualTo("ABC"));
    }

    [Test]
    public void Given_MaximumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        var personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        var coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        var amount = 100;
        var reason = new string('A', 200); // Exactly 200 characters

        // When
        var command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Reason.Value.Length, Is.EqualTo(200));
    }

    #endregion
}

