using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Commands;

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
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 500;
        string reason = "Depositing earnings";

        // When
        DepositGoldCommand command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

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
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 0;
        string reason = "Zero deposit test";

        // When
        DepositGoldCommand command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Amount.Value, Is.EqualTo(0));
    }

    [Test]
    public void Given_LargeAmount_When_CreatingCommand_Then_CommandIsCreatedSuccessfully()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 1_000_000;
        string reason = "Large deposit";

        // When
        DepositGoldCommand command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

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
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = -100;
        string reason = "Invalid deposit";

        // When & Then
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("cannot be negative"));
    }

    [Test]
    public void Given_EmptyReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 100;
        string reason = "";

        // When & Then
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("cannot be empty"));
    }

    [Test]
    public void Given_TooShortReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 100;
        string reason = "Ab"; // Less than 3 characters

        // When & Then
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("at least 3 characters"));
    }

    [Test]
    public void Given_TooLongReason_When_CreatingCommand_Then_ThrowsArgumentException()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 100;
        string reason = new string('A', 201); // More than 200 characters

        // When & Then
        ArgumentException? ex = Assert.Throws<ArgumentException>(() =>
            DepositGoldCommand.Create(personaId, coinhouse, amount, reason));
        Assert.That(ex!.Message, Does.Contain("cannot exceed 200 characters"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Given_ReasonWithLeadingAndTrailingSpaces_When_CreatingCommand_Then_ReasonIsTrimmed()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 100;
        string reason = "  Deposit with spaces  ";

        // When
        DepositGoldCommand command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command.Reason.Value, Is.EqualTo("Deposit with spaces"));
    }

    [Test]
    public void Given_MinimumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 100;
        string reason = "ABC"; // Exactly 3 characters

        // When
        DepositGoldCommand command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Reason.Value, Is.EqualTo("ABC"));
    }

    [Test]
    public void Given_MaximumValidReason_When_CreatingCommand_Then_CommandIsCreated()
    {
        // Given
        PersonaId personaId = PersonaTestHelpers.CreateCharacterPersona().Id;
        CoinhouseTag coinhouse = EconomyTestHelpers.CreateCoinhouseTag();
        int amount = 100;
        string reason = new string('A', 200); // Exactly 200 characters

        // When
        DepositGoldCommand command = DepositGoldCommand.Create(personaId, coinhouse, amount, reason);

        // Then
        Assert.That(command, Is.Not.Null);
        Assert.That(command.Reason.Value.Length, Is.EqualTo(200));
    }

    #endregion
}

