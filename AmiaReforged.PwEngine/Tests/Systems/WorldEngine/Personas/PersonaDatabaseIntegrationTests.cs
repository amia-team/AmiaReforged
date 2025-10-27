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
        Guid characterId = Guid.NewGuid();
        PersistedCharacter character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Aldric",
            LastName = "Stormblade",
            CdKey = "TESTKEY1",
            PersonaIdString = null  // Not populated yet
        };

        // Act
        PersonaId personaId = character.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Character:{characterId}"));
    }

    [Test]
    public void PersistedCharacter_PersonaId_ParsesFromString_When_PersonaIdString_IsPopulated()
    {
        // Arrange
        Guid characterId = Guid.NewGuid();
        string expectedPersonaIdString = $"Character:{characterId}";
        PersistedCharacter character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Aldric",
            LastName = "Stormblade",
            CdKey = "TESTKEY1",
            PersonaIdString = expectedPersonaIdString
        };

        // Act
        PersonaId personaId = character.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo(expectedPersonaIdString));
    }

    [Test]
    public void PersistedCharacter_CharacterId_ReturnsStronglyTypedId()
    {
        // Arrange
        Guid characterId = Guid.NewGuid();
        PersistedCharacter character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Aldric",
            LastName = "Stormblade",
            CdKey = "TESTKEY1"
        };

        // Act
        CharacterId strongCharacterId = character.CharacterId;

        // Assert
        Assert.That(strongCharacterId.Value, Is.EqualTo(characterId));
    }

    [Test]
    public void Organization_PersonaId_AutoGenerates_When_PersonaIdString_IsNull()
    {
        // Arrange
        Guid orgId = Guid.NewGuid();
        Organization organization = new Organization
        {
            Id = orgId,
            Name = "Merchants Guild",
            Description = "A guild of merchants",
            PersonaIdString = null  // Not populated yet
        };

        // Act
        PersonaId personaId = organization.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(personaId.Value, Is.EqualTo(orgId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Organization:{orgId}"));
    }

    [Test]
    public void Organization_PersonaId_ParsesFromString_When_PersonaIdString_IsPopulated()
    {
        // Arrange
        Guid orgId = Guid.NewGuid();
        string expectedPersonaIdString = $"Organization:{orgId}";
        Organization organization = new Organization
        {
            Id = orgId,
            Name = "Merchants Guild",
            Description = "A guild of merchants",
            PersonaIdString = expectedPersonaIdString
        };

        // Act
        PersonaId personaId = organization.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(personaId.Value, Is.EqualTo(orgId.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo(expectedPersonaIdString));
    }

    [Test]
    public void Organization_OrganizationId_ReturnsStronglyTypedId()
    {
        // Arrange
        Guid orgId = Guid.NewGuid();
        Organization organization = new Organization
        {
            Id = orgId,
            Name = "Merchants Guild",
            Description = "A guild of merchants"
        };

        // Act
        OrganizationId strongOrgId = organization.OrganizationId;

        // Assert
        Assert.That(strongOrgId.Value, Is.EqualTo(orgId));
    }

    [Test]
    public void CoinHouse_PersonaId_AutoGenerates_When_PersonaIdString_IsNull()
    {
        // Arrange
        CoinHouse coinhouse = new CoinHouse
        {
            Tag = "cordor-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            StoredGold = 10000,
            PersonaIdString = null  // Not populated yet
        };

        // Act
        PersonaId personaId = coinhouse.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(personaId.Value, Is.EqualTo("cordor-bank"));
        Assert.That(personaId.ToString(), Is.EqualTo("Coinhouse:cordor-bank"));
    }

    [Test]
    public void CoinHouse_PersonaId_ParsesFromString_When_PersonaIdString_IsPopulated()
    {
        // Arrange
        string expectedPersonaIdString = "Coinhouse:cordor-bank";
        CoinHouse coinhouse = new CoinHouse
        {
            Tag = "cordor-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            StoredGold = 10000,
            PersonaIdString = expectedPersonaIdString
        };

        // Act
        PersonaId personaId = coinhouse.PersonaId;

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(personaId.Value, Is.EqualTo("cordor-bank"));
        Assert.That(personaId.ToString(), Is.EqualTo(expectedPersonaIdString));
    }

    [Test]
    public void CoinHouse_StrongTypedProperties_WorkCorrectly()
    {
        // Arrange
        CoinHouse coinhouse = new CoinHouse
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
        Guid characterId = Guid.NewGuid();
        PersistedCharacter character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Test",
            LastName = "Character",
            CdKey = "TESTKEY1",
            PersonaIdString = $"Character:{characterId}"
        };

        // Act
        PersonaId personaId = character.PersonaId;
        string personaIdString = personaId.ToString();
        PersonaId parsedPersonaId = PersonaId.Parse(personaIdString);

        // Assert
        Assert.That(parsedPersonaId, Is.EqualTo(personaId));
        Assert.That(parsedPersonaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(parsedPersonaId.Value, Is.EqualTo(characterId.ToString()));
    }

    [Test]
    public void PersonaId_RoundTrip_Organization_ToStringAndParse()
    {
        // Arrange
        Guid orgId = Guid.NewGuid();
        Organization organization = new Organization
        {
            Id = orgId,
            Name = "Test Guild",
            PersonaIdString = $"Organization:{orgId}"
        };

        // Act
        PersonaId personaId = organization.PersonaId;
        string personaIdString = personaId.ToString();
        PersonaId parsedPersonaId = PersonaId.Parse(personaIdString);

        // Assert
        Assert.That(parsedPersonaId, Is.EqualTo(personaId));
        Assert.That(parsedPersonaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(parsedPersonaId.Value, Is.EqualTo(orgId.ToString()));
    }

    [Test]
    public void PersonaId_RoundTrip_Coinhouse_ToStringAndParse()
    {
        // Arrange
        CoinHouse coinhouse = new CoinHouse
        {
            Tag = "test-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            PersonaIdString = "Coinhouse:test-bank"
        };

        // Act
        PersonaId personaId = coinhouse.PersonaId;
        string personaIdString = personaId.ToString();
        PersonaId parsedPersonaId = PersonaId.Parse(personaIdString);

        // Assert
        Assert.That(parsedPersonaId, Is.EqualTo(personaId));
        Assert.That(parsedPersonaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(parsedPersonaId.Value, Is.EqualTo("test-bank"));
    }

    [Test]
    public void DifferentEntityTypes_HaveDifferentPersonaIds()
    {
        // Arrange
        Guid characterId = Guid.NewGuid();
        PersistedCharacter character = new PersistedCharacter
        {
            Id = characterId,
            FirstName = "Test",
            LastName = "Character",
            CdKey = "TESTKEY1"
        };

        Organization organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Guild"
        };

        CoinHouse coinhouse = new CoinHouse
        {
            Tag = "test-bank",
            Settlement = 1,
            EngineId = Guid.NewGuid()
        };

        // Act
        PersonaId charPersonaId = character.PersonaId;
        PersonaId orgPersonaId = organization.PersonaId;
        PersonaId coinhousePersonaId = coinhouse.PersonaId;

        // Assert
        Assert.That(charPersonaId.Type, Is.Not.EqualTo(orgPersonaId.Type));
        Assert.That(charPersonaId.Type, Is.Not.EqualTo(coinhousePersonaId.Type));
        Assert.That(orgPersonaId.Type, Is.Not.EqualTo(coinhousePersonaId.Type));
        Assert.That(charPersonaId, Is.Not.EqualTo(orgPersonaId));
        Assert.That(charPersonaId, Is.Not.EqualTo(coinhousePersonaId));
        Assert.That(orgPersonaId, Is.Not.EqualTo(coinhousePersonaId));
    }
}

