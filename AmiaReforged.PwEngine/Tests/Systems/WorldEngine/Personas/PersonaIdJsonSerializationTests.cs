using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaIdJsonSerializationTests
{
    [Test]
    public void PersonaId_SerializesToString()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();
        var expected = $"\"Character:{characterId.Value}\"";

        // Act
        var json = JsonSerializer.Serialize(personaId);

        // Assert
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void PersonaId_DeserializesFromString()
    {
        // Arrange
        var characterId = CharacterId.New();
        var json = $"\"Character:{characterId.Value}\"";

        // Act
        var personaId = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.Value.ToString()));
    }

    [Test]
    public void PersonaId_RoundTrip_Character()
    {
        // Arrange
        var original = CharacterId.New().ToPersonaId();

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_Organization()
    {
        // Arrange
        var original = OrganizationId.New().ToPersonaId();

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_Coinhouse()
    {
        // Arrange
        var original = PersonaId.FromCoinhouse(new CoinhouseTag("test-bank"));

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_Government()
    {
        // Arrange
        var original = GovernmentId.New().ToPersonaId();

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_SystemProcess()
    {
        // Arrange
        var original = PersonaId.FromSystem("TaxCollector");

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_DeserializeInvalidFormat_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "\"InvalidFormat\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonaId>(invalidJson));
    }

    [Test]
    public void PersonaId_DeserializeInvalidType_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "\"InvalidType:12345\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonaId>(invalidJson));
    }

    [Test]
    public void PersonaId_DeserializeNull_ThrowsJsonException()
    {
        // Arrange
        var nullJson = "null";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonaId>(nullJson));
    }

    [Test]
    public void PersonaId_InObject_SerializesCorrectly()
    {
        // Arrange
        var testObject = new TestClassWithPersonaId
        {
            Id = 123,
            PersonaId = CharacterId.New().ToPersonaId(),
            Name = "Test"
        };

        // Act
        var json = JsonSerializer.Serialize(testObject);
        var deserialized = JsonSerializer.Deserialize<TestClassWithPersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Id, Is.EqualTo(testObject.Id));
        Assert.That(deserialized.PersonaId, Is.EqualTo(testObject.PersonaId));
        Assert.That(deserialized.Name, Is.EqualTo(testObject.Name));
    }

    [Test]
    public void PersonaId_InCollection_SerializesCorrectly()
    {
        // Arrange
        var personaIds = new List<PersonaId>
        {
            CharacterId.New().ToPersonaId(),
            OrganizationId.New().ToPersonaId(),
            PersonaId.FromCoinhouse(new CoinhouseTag("test-bank")),
            PersonaId.FromSystem("TestProcess")
        };

        // Act
        var json = JsonSerializer.Serialize(personaIds);
        var deserialized = JsonSerializer.Deserialize<List<PersonaId>>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Count, Is.EqualTo(4));
        for (int i = 0; i < personaIds.Count; i++)
        {
            Assert.That(deserialized[i], Is.EqualTo(personaIds[i]));
        }
    }

    [Test]
    public void PersonaId_InDictionary_SerializesCorrectly()
    {
        // Arrange
        var dict = new Dictionary<string, PersonaId>
        {
            ["character"] = CharacterId.New().ToPersonaId(),
            ["organization"] = OrganizationId.New().ToPersonaId(),
            ["coinhouse"] = PersonaId.FromCoinhouse(new CoinhouseTag("test-bank"))
        };

        // Act
        var json = JsonSerializer.Serialize(dict);
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, PersonaId>>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Count, Is.EqualTo(3));
        Assert.That(deserialized["character"], Is.EqualTo(dict["character"]));
        Assert.That(deserialized["organization"], Is.EqualTo(dict["organization"]));
        Assert.That(deserialized["coinhouse"], Is.EqualTo(dict["coinhouse"]));
    }

    [Test]
    public void PersonaId_WithOptions_SerializesCorrectly()
    {
        // Arrange
        var personaId = CharacterId.New().ToPersonaId();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Act
        var json = JsonSerializer.Serialize(personaId, options);
        var deserialized = JsonSerializer.Deserialize<PersonaId>(json, options);

        // Assert
        Assert.That(deserialized, Is.EqualTo(personaId));
    }

    // Helper class for testing PersonaId in objects
    private class TestClassWithPersonaId
    {
        public int Id { get; set; }
        public PersonaId PersonaId { get; set; }
        public string? Name { get; set; }
    }
}

