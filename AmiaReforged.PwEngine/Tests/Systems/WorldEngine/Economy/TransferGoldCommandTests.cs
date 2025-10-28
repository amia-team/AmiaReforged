using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy;

/// <summary>
/// Tests for TransferGoldCommand behavior and validation.
/// Validates the command model used for transferring gold between personas.
/// </summary>
[TestFixture]
public class TransferGoldCommandTests
{
    private PersonaId _fromPersona;
    private PersonaId _toPersona;
    private Quantity _validAmount;

    [SetUp]
    public void Setup()
    {
        _fromPersona = PersonaId.FromCharacter(CharacterId.New());
        _toPersona = PersonaId.FromOrganization(OrganizationId.New());
        _validAmount = Quantity.Parse(100);
    }

    #region Construction Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesCommand()
    {
        // Act
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount);

        // Assert
        Assert.That(command.From, Is.EqualTo(_fromPersona));
        Assert.That(command.To, Is.EqualTo(_toPersona));
        Assert.That(command.Amount, Is.EqualTo(_validAmount));
        Assert.That(command.Memo, Is.Null);
    }

    [Test]
    public void Constructor_WithMemo_StoresMemo()
    {
        // Arrange
        const string memo = "Guild dues for January";

        // Act
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, memo);

        // Assert
        Assert.That(command.Memo, Is.EqualTo(memo));
    }

    [Test]
    public void Command_IsRecord_SupportsValueEquality()
    {
        // Arrange
        TransferGoldCommand cmd1 = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, "memo");
        TransferGoldCommand cmd2 = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, "memo");

        // Assert
        Assert.That(cmd1, Is.EqualTo(cmd2));
    }

    [Test]
    public void Command_IsImmutable()
    {
        // Arrange
        TransferGoldCommand original = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount);

        // Act
        TransferGoldCommand modified = original with { Amount = Quantity.Parse(200) };

        // Assert
        Assert.That(original.Amount.Value, Is.EqualTo(100));
        Assert.That(modified.Amount.Value, Is.EqualTo(200));
    }

    #endregion

    #region Validation Tests

    [Test]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount);

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(errorMessage, Is.Null);
    }

    [Test]
    public void Validate_WithZeroAmount_ReturnsInvalid()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(0));

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.That(isValid, Is.False);
        Assert.That(errorMessage, Does.Contain("Amount must be greater than zero"));
    }

    [Test]
    public void Validate_WithNegativeAmount_ThrowsAtValueObjectLevel()
    {
        // Assert - Quantity.Parse enforces non-negative constraint
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => Quantity.Parse(-100));
        Assert.That(ex!.Message, Does.Contain("Quantity cannot be negative"));
        Assert.That(ex.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Validate_WithSameFromAndTo_ReturnsInvalid()
    {
        // Arrange
        PersonaId samePersona = PersonaId.FromCharacter(CharacterId.New());
        TransferGoldCommand command = new TransferGoldCommand(samePersona, samePersona, _validAmount);

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.That(isValid, Is.False);
        Assert.That(errorMessage, Does.Contain("Cannot transfer to self"));
    }

    [Test]
    public void Validate_WithMemoExceeding500Characters_ReturnsInvalid()
    {
        // Arrange
        string longMemo = new string('x', 501);
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, longMemo);

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.That(isValid, Is.False);
        Assert.That(errorMessage, Does.Contain("Memo cannot exceed 500 characters"));
    }

    [Test]
    public void Validate_WithMemoExactly500Characters_ReturnsValid()
    {
        // Arrange
        string exactMemo = new string('x', 500);
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, exactMemo);

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(errorMessage, Is.Null);
    }

    [Test]
    public void Validate_WithEmptyMemo_ReturnsValid()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, "");

        // Act
        (bool isValid, string? errorMessage) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
    }

    #endregion

    #region Cross-Persona Transfer Tests

    [Test]
    public void Command_SupportsCharacterToOrganization()
    {
        // Arrange
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        PersonaId organization = PersonaId.FromOrganization(OrganizationId.New());

        // Act
        TransferGoldCommand command = new TransferGoldCommand(character, organization, _validAmount, "Membership fee");
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(command.From.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(command.To.Type, Is.EqualTo(PersonaType.Organization));
    }

    [Test]
    public void Command_SupportsOrganizationToCharacter()
    {
        // Arrange
        PersonaId organization = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());

        // Act
        TransferGoldCommand command = new TransferGoldCommand(organization, character, _validAmount, "Salary payment");
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(command.From.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(command.To.Type, Is.EqualTo(PersonaType.Character));
    }

    [Test]
    public void Command_SupportsCharacterToCoinhouse()
    {
        // Arrange
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        PersonaId coinhouse = PersonaId.FromCoinhouse(new CoinhouseTag("CordorBank"));

        // Act
        TransferGoldCommand command = new TransferGoldCommand(character, coinhouse, _validAmount, "Deposit");
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(command.To.Type, Is.EqualTo(PersonaType.Coinhouse));
    }

    [Test]
    public void Command_SupportsOrganizationToOrganization()
    {
        // Arrange
        PersonaId org1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId org2 = PersonaId.FromOrganization(OrganizationId.New());

        // Act
        TransferGoldCommand command = new TransferGoldCommand(org1, org2, _validAmount, "Trade payment");
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void Command_SupportsSystemToCharacter()
    {
        // Arrange
        PersonaId system = PersonaId.FromSystem("QuestRewards");
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());

        // Act
        TransferGoldCommand command = new TransferGoldCommand(system, character, _validAmount, "Quest completion");
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
        Assert.That(command.From.Type, Is.EqualTo(PersonaType.SystemProcess));
    }

    #endregion

    #region Amount Boundary Tests

    [Test]
    public void Validate_WithSmallAmount_ReturnsValid()
    {
        // Arrange - 1 gold
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(1));

        // Act
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void Validate_WithLargeAmount_ReturnsValid()
    {
        // Arrange - 1 million gold
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(1_000_000));

        // Act
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void Validate_WithMaxIntAmount_ReturnsValid()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, Quantity.Parse(int.MaxValue));

        // Act
        (bool isValid, _) = command.Validate();

        // Assert
        Assert.That(isValid, Is.True);
    }

    #endregion

    #region Deconstruction Tests

    [Test]
    public void Command_SupportsDeconstruction()
    {
        // Arrange
        TransferGoldCommand command = new TransferGoldCommand(_fromPersona, _toPersona, _validAmount, "test");

        // Act
        (PersonaId from, PersonaId to, Quantity amount, string? memo) = command;

        // Assert
        Assert.That(from, Is.EqualTo(_fromPersona));
        Assert.That(to, Is.EqualTo(_toPersona));
        Assert.That(amount, Is.EqualTo(_validAmount));
        Assert.That(memo, Is.EqualTo("test"));
    }

    #endregion
}

