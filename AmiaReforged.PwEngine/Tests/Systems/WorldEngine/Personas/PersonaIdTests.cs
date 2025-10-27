using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaIdTests
{
    [Test]
    public void FromCharacter_CreatesCorrectPersonaId()
    {
        // Arrange
        var characterId = CharacterId.New();

        // Act
        var personaId = PersonaId.FromCharacter(characterId);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.Value.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Character:{characterId.Value}"));
    }

    [Test]
    public void FromOrganization_CreatesCorrectPersonaId()
    {
        // Arrange
        var orgId = OrganizationId.New();

        // Act
        var personaId = PersonaId.FromOrganization(orgId);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(personaId.Value, Is.EqualTo(orgId.Value.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Organization:{orgId.Value}"));
    }

    [Test]
    public void FromCoinhouse_CreatesCorrectPersonaId()
    {
        // Arrange
        var tag = new CoinhouseTag("cordor-bank");

        // Act
        var personaId = PersonaId.FromCoinhouse(tag);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(personaId.Value, Is.EqualTo("cordor-bank"));
        Assert.That(personaId.ToString(), Is.EqualTo("Coinhouse:cordor-bank"));
    }

    [Test]
    public void FromGovernment_CreatesCorrectPersonaId()
    {
        // Arrange
        var govId = GovernmentId.New();

        // Act
        var personaId = PersonaId.FromGovernment(govId);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Government));
        Assert.That(personaId.Value, Is.EqualTo(govId.Value.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Government:{govId.Value}"));
    }

    [Test]
    public void FromSystem_CreatesCorrectPersonaId()
    {
        // Act
        var personaId = PersonaId.FromSystem("TaxCollector");

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.SystemProcess));
        Assert.That(personaId.Value, Is.EqualTo("TaxCollector"));
        Assert.That(personaId.ToString(), Is.EqualTo("SystemProcess:TaxCollector"));
    }

    [Test]
    public void Parse_ValidString_ReturnsCorrectPersonaId()
    {
        // Arrange
        var characterId = CharacterId.New();
        var expected = $"Character:{characterId.Value}";

        // Act
        var personaId = PersonaId.Parse(expected);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.Value.ToString()));
    }

    [Test]
    public void Parse_InvalidFormat_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PersonaId.Parse("InvalidFormat"));
        Assert.Throws<ArgumentException>(() => PersonaId.Parse(""));
        Assert.Throws<ArgumentException>(() => PersonaId.Parse("   "));
    }

    [Test]
    public void Parse_InvalidType_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => PersonaId.Parse("InvalidType:12345"));
        Assert.That(ex!.Message, Does.Contain("InvalidType"));
    }

    [Test]
    public void Constructor_EmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PersonaId(PersonaType.Character, ""));
        Assert.Throws<ArgumentException>(() => new PersonaId(PersonaType.Character, "   "));
    }

    [Test]
    public void ImplicitConversion_ToString_Works()
    {
        // Arrange
        var personaId = PersonaId.FromSystem("TestProcess");

        // Act
        string stringValue = personaId;

        // Assert
        Assert.That(stringValue, Is.EqualTo("SystemProcess:TestProcess"));
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var id1 = PersonaId.FromSystem("Process1");
        var id2 = PersonaId.FromSystem("Process1");

        // Act & Assert
        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var id1 = PersonaId.FromSystem("Process1");
        var id2 = PersonaId.FromSystem("Process2");

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void Equality_DifferentTypes_AreNotEqual()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var id1 = new PersonaId(PersonaType.Character, guid);
        var id2 = new PersonaId(PersonaType.Organization, guid);

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }
}

