using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaDatabaseIntegrationTests
{
    [Test]
    public void PersistedCharacter_PersonaId_AutoGenerates_When_PersonaIdString_IsNull()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Aldric",
            LastName = "Stormblade",
            CdKey = "TESTKEY1",
            PersonaIdString = null  // Not populated yet
        };

        // Act
        var personaId = character.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Character:{characterId}"));
    }

    [Test]
    public void PersistedCharacter_PersonaId_ParsesFromString_When_PersonaIdString_IsPopulated()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var expectedPersonaIdString = $"Character:{characterId}";
        var character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Aldric",
            LastName = "Stormblade",
            CdKey = "TESTKEY1",
            PersonaIdString = expectedPersonaIdString
        };

        // Act
        var personaId = character.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo(expectedPersonaIdString));
    }

    [Test]
    public void PersistedCharacter_CharacterId_ReturnsStronglyTypedId()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Aldric",
            LastName = "Stormblade",
            CdKey = "TESTKEY1"
        };

        // Act
        var strongCharacterId = character.CharacterId;

        // Assert
        Assert.That(strongCharacterId.Value, Is.EqualTo(characterId));
    }

    [Test]
    public void Organization_PersonaId_AutoGenerates_When_PersonaIdString_IsNull()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = orgId,
            Name = "Merchants Guild",
            Description = "A guild of merchants",
            PersonaIdString = null  // Not populated yet
        };

        // Act
        var personaId = organization.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(personaId.Value, Is.EqualTo(orgId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Organization:{orgId}"));
    }

    [Test]
    public void Organization_PersonaId_ParsesFromString_When_PersonaIdString_IsPopulated()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var expectedPersonaIdString = $"Organization:{orgId}";
        var organization = new Organization
        {
            Id = orgId,
            Name = "Merchants Guild",
            Description = "A guild of merchants",
            PersonaIdString = expectedPersonaIdString
        };

        // Act
        var personaId = organization.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(personaId.Value, Is.EqualTo(orgId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo(expectedPersonaIdString));
    }

    [Test]
    public void Organization_OrganizationId_ReturnsStronglyTypedId()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = orgId,
            Name = "Merchants Guild",
            Description = "A guild of merchants"
        };

        // Act
        var strongOrgId = organization.OrganizationId;

        // Assert
        Assert.That(strongOrgId.Value, Is.EqualTo(orgId));
    }

    [Test]
    public void CoinHouse_PersonaId_AutoGenerates_When_PersonaIdString_IsNull()
    {
        // Arrange
        var coinhouse = new CoinHouse
        {
            Tag = "cordor-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            StoredGold = 10000,
            PersonaIdString = null  // Not populated yet
        };

        // Act
        var personaId = coinhouse.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(personaId.Value, Is.EqualTo("cordor-bank"));
        Assert.That(personaId.ToString(), Is.EqualTo("Coinhouse:cordor-bank"));
    }

    [Test]
    public void CoinHouse_PersonaId_ParsesFromString_When_PersonaIdString_IsPopulated()
    {
        // Arrange
        var expectedPersonaIdString = "Coinhouse:cordor-bank";
        var coinhouse = new CoinHouse
        {
            Tag = "cordor-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            StoredGold = 10000,
            PersonaIdString = expectedPersonaIdString
        };

        // Act
        var personaId = coinhouse.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(personaId.Value, Is.EqualTo("cordor-bank"));
        Assert.That(personaId.ToString(), Is.EqualTo(expectedPersonaIdString));
    }

    [Test]
    public void CoinHouse_StrongTypedProperties_WorkCorrectly()
    {
        // Arrange
        var coinhouse = new CoinHouse
        {
            Tag = "cordor-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            StoredGold = 10000
        };

        // Act & Assert
        Assert.That(coinhouse.CoinhouseTag.Value, Is.EqualTo("cordor-bank"));
        Assert.That(coinhouse.SettlementId.Value, Is.EqualTo(1));
        Assert.That(coinhouse.Balance.Value, Is.EqualTo(10000));
    }

    [Test]
    public void PersonaId_RoundTrip_Character_ToStringAndParse()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Test",
            LastName = "Character",
            CdKey = "TESTKEY1",
            PersonaIdString = $"Character:{characterId}"
        };

        // Act
        var personaId = character.PersonaId;
        var personaIdString = personaId.ToString();
        var parsedPersonaId = PersonaId.Parse(personaIdString);

        // Assert
        Assert.That(parsedPersonaId, Is.EqualTo(personaId));
        Assert.That(parsedPersonaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(parsedPersonaId.Value, Is.EqualTo(characterId.ToString()));
    }

    [Test]
    public void PersonaId_RoundTrip_Organization_ToStringAndParse()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var organization = new Organization
        {
            Id = orgId,
            Name = "Test Guild",
            PersonaIdString = $"Organization:{orgId}"
        };

        // Act
        var personaId = organization.PersonaId;
        var personaIdString = personaId.ToString();
        var parsedPersonaId = PersonaId.Parse(personaIdString);

        // Assert
        Assert.That(parsedPersonaId, Is.EqualTo(personaId));
        Assert.That(parsedPersonaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(parsedPersonaId.Value, Is.EqualTo(orgId.ToString()));
    }

    [Test]
    public void PersonaId_RoundTrip_Coinhouse_ToStringAndParse()
    {
        // Arrange
        var coinhouse = new CoinHouse
        {
            Tag = "test-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            PersonaIdString = "Coinhouse:test-bank"
        };

        // Act
        var personaId = coinhouse.PersonaId;
        var personaIdString = personaId.ToString();
        var parsedPersonaId = PersonaId.Parse(personaIdString);

        // Assert
        Assert.That(parsedPersonaId, Is.EqualTo(personaId));
        Assert.That(parsedPersonaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(parsedPersonaId.Value, Is.EqualTo("test-bank"));
    }

    [Test]
    public void DifferentEntityTypes_HaveDifferentPersonaIds()
    {
        // Arrange
        var characterId = Guid.NewGuid();
        var character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Test",
            LastName = "Character",
            CdKey = "TESTKEY1"
        };

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Guild"
        };

        var coinhouse = new CoinHouse
        {
            Tag = "test-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid()
        };

        // Act
        var charPersonaId = character.PersonaId;
        var orgPersonaId = organization.PersonaId;
        var coinhousePersonaId = coinhouse.PersonaId;

        // Assert
        Assert.That(charPersonaId.Type, Is.Not.EqualTo(orgPersonaId.Type));
        Assert.That(charPersonaId.Type, Is.Not.EqualTo(coinhousePersonaId.Type));
        Assert.That(orgPersonaId.Type, Is.Not.EqualTo(coinhousePersonaId.Type));
        Assert.That(charPersonaId, Is.Not.EqualTo(orgPersonaId));
        Assert.That(charPersonaId, Is.Not.EqualTo(coinhousePersonaId));
        Assert.That(orgPersonaId, Is.Not.EqualTo(coinhousePersonaId));
    }
}

