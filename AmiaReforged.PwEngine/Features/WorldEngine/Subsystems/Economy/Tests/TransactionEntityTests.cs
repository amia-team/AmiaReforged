using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests;

/// <summary>
/// Tests for Transaction entity behavior.
/// Validates storage and retrieval of persona-based transactions.
/// </summary>
[TestFixture]
public class TransactionEntityTests
{
    #region Construction Tests

    [Test]
    public void Constructor_WithRequiredProperties_CreatesTransaction()
    {
        // Arrange
        PersonaId fromPersonaId = PersonaId.FromCharacter(CharacterId.New());
        PersonaId toPersonaId = PersonaId.FromOrganization(OrganizationId.New());

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = fromPersonaId.ToString(),
            ToPersonaId = toPersonaId.ToString(),
            Amount = 100
        };

        // Assert
        Assert.That(transaction.FromPersonaId, Is.EqualTo(fromPersonaId.ToString()));
        Assert.That(transaction.ToPersonaId, Is.EqualTo(toPersonaId.ToString()));
        Assert.That(transaction.Amount, Is.EqualTo(100));
        Assert.That(transaction.Timestamp, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void Constructor_SetsTimestamp_Automatically()
    {
        // Arrange
        DateTime before = DateTime.UtcNow;

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100
        };

        DateTime after = DateTime.UtcNow;

        // Assert
        Assert.That(transaction.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(transaction.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void Memo_CanBeNull()
    {
        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100,
            Memo = null
        };

        // Assert
        Assert.That(transaction.Memo, Is.Null);
    }

    [Test]
    public void Memo_CanBeSet()
    {
        // Arrange
        const string memo = "Guild membership dues";

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100,
            Memo = memo
        };

        // Assert
        Assert.That(transaction.Memo, Is.EqualTo(memo));
    }

    #endregion

    #region NotMapped Property Tests

    [Test]
    public void From_ParsesPersonaId_FromString()
    {
        // Arrange
        PersonaId expectedPersonaId = PersonaId.FromCharacter(CharacterId.From(Guid.Parse("12345678-1234-1234-1234-123456789012")));
        Transaction transaction = new Transaction
        {
            FromPersonaId = expectedPersonaId.ToString(),
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100
        };

        // Act
        PersonaId from = transaction.From;

        // Assert
        Assert.That(from, Is.EqualTo(expectedPersonaId));
        Assert.That(from.Type, Is.EqualTo(PersonaType.Character));
    }

    [Test]
    public void To_ParsesPersonaId_FromString()
    {
        // Arrange
        PersonaId expectedPersonaId = PersonaId.FromOrganization(OrganizationId.From(Guid.Parse("87654321-4321-4321-4321-210987654321")));
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = expectedPersonaId.ToString(),
            Amount = 100
        };

        // Act
        PersonaId to = transaction.To;

        // Assert
        Assert.That(to, Is.EqualTo(expectedPersonaId));
        Assert.That(to.Type, Is.EqualTo(PersonaType.Organization));
    }

    [Test]
    public void AmountTransferred_ReturnsQuantity()
    {
        // Arrange
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 250
        };

        // Act
        Quantity amount = transaction.AmountTransferred;

        // Assert
        Assert.That(amount.Value, Is.EqualTo(250));
    }

    #endregion

    #region Cross-Persona Transaction Tests

    [Test]
    public void Transaction_SupportsCharacterToOrganization()
    {
        // Arrange
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        PersonaId organization = PersonaId.FromOrganization(OrganizationId.New());

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = character.ToString(),
            ToPersonaId = organization.ToString(),
            Amount = 500,
            Memo = "Guild dues"
        };

        // Assert
        Assert.That(transaction.From.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(transaction.To.Type, Is.EqualTo(PersonaType.Organization));
    }

    [Test]
    public void Transaction_SupportsCharacterToCoinhouse()
    {
        // Arrange
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());
        PersonaId coinhouse = PersonaId.FromCoinhouse(new CoinhouseTag("CordorBank"));

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = character.ToString(),
            ToPersonaId = coinhouse.ToString(),
            Amount = 1000,
            Memo = "Deposit to savings"
        };

        // Assert
        Assert.That(transaction.From.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(transaction.To.Type, Is.EqualTo(PersonaType.Coinhouse));
        // CoinhouseTag normalizes to lowercase
        Assert.That(transaction.To.Value, Is.EqualTo("cordorbank"));
    }

    [Test]
    public void Transaction_SupportsSystemToCharacter()
    {
        // Arrange
        PersonaId system = PersonaId.FromSystem("QuestRewards");
        PersonaId character = PersonaId.FromCharacter(CharacterId.New());

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = system.ToString(),
            ToPersonaId = character.ToString(),
            Amount = 150,
            Memo = "Quest: Dragon Slayer"
        };

        // Assert
        Assert.That(transaction.From.Type, Is.EqualTo(PersonaType.SystemProcess));
        Assert.That(transaction.From.Value, Is.EqualTo("QuestRewards"));
        Assert.That(transaction.To.Type, Is.EqualTo(PersonaType.Character));
    }

    [Test]
    public void Transaction_SupportsOrganizationToOrganization()
    {
        // Arrange
        PersonaId org1 = PersonaId.FromOrganization(OrganizationId.New());
        PersonaId org2 = PersonaId.FromOrganization(OrganizationId.New());

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = org1.ToString(),
            ToPersonaId = org2.ToString(),
            Amount = 5000,
            Memo = "Trade payment for goods"
        };

        // Assert
        Assert.That(transaction.From.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(transaction.To.Type, Is.EqualTo(PersonaType.Organization));
    }

    [Test]
    public void Transaction_SupportsGovernmentToOrganization()
    {
        // Arrange
        PersonaId government = PersonaId.FromGovernment(GovernmentId.New());
        PersonaId organization = PersonaId.FromOrganization(OrganizationId.New());

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = government.ToString(),
            ToPersonaId = organization.ToString(),
            Amount = 10000,
            Memo = "Government subsidy"
        };

        // Assert
        Assert.That(transaction.From.Type, Is.EqualTo(PersonaType.Government));
        Assert.That(transaction.To.Type, Is.EqualTo(PersonaType.Organization));
    }

    #endregion

    #region Data Integrity Tests

    [Test]
    public void Transaction_PreservesPersonaIdFormat()
    {
        // Arrange
        const string fromId = "Character:12345678-1234-1234-1234-123456789012";
        const string toId = "Organization:87654321-4321-4321-4321-210987654321";

        Transaction transaction = new Transaction
        {
            FromPersonaId = fromId,
            ToPersonaId = toId,
            Amount = 100
        };

        // Act
        PersonaId from = transaction.From;
        PersonaId to = transaction.To;

        // Assert - round-trip preservation
        Assert.That(from.ToString(), Is.EqualTo(fromId));
        Assert.That(to.ToString(), Is.EqualTo(toId));
    }

    [Test]
    public void Transaction_WithZeroAmount_IsValid()
    {
        // Arrange & Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 0,
            Memo = "Test transaction with zero amount"
        };

        // Assert - entity allows it (validation happens at command level)
        Assert.That(transaction.Amount, Is.EqualTo(0));
    }

    [Test]
    public void Transaction_WithNegativeAmount_IsStored()
    {
        // Arrange & Act - entity allows negative (validation happens at command level)
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = -100,
            Memo = "Reversal transaction"
        };

        // Assert
        Assert.That(transaction.Amount, Is.EqualTo(-100));
    }

    [Test]
    public void Transaction_WithLongMemo_StoresUpTo500Characters()
    {
        // Arrange
        string memo = new string('x', 500);

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100,
            Memo = memo
        };

        // Assert
        Assert.That(transaction.Memo, Has.Length.EqualTo(500));
    }

    #endregion

    #region Timestamp Tests

    [Test]
    public void Timestamp_CanBeOverridden()
    {
        // Arrange
        DateTime specificTime = new DateTime(2025, 10, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100,
            Timestamp = specificTime
        };

        // Assert
        Assert.That(transaction.Timestamp, Is.EqualTo(specificTime));
    }

    [Test]
    public void Timestamp_IsUtc()
    {
        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100
        };

        // Assert
        Assert.That(transaction.Timestamp.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    #endregion

    #region Id Tests

    [Test]
    public void Id_DefaultsToZero()
    {
        // Act
        Transaction transaction = new Transaction
        {
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100
        };

        // Assert - EF Core will assign this
        Assert.That(transaction.Id, Is.EqualTo(0));
    }

    [Test]
    public void Id_CanBeSet()
    {
        // Act
        Transaction transaction = new Transaction
        {
            Id = 12345,
            FromPersonaId = "Character:00000000-0000-0000-0000-000000000001",
            ToPersonaId = "Organization:00000000-0000-0000-0000-000000000002",
            Amount = 100
        };

        // Assert
        Assert.That(transaction.Id, Is.EqualTo(12345));
    }

    #endregion
}

