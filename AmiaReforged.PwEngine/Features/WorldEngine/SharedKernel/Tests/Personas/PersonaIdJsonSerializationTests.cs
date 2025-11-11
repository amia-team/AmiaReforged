using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Personas;

[TestFixture]
public class PersonaIdJsonSerializationTests
{
    [Test]
    public void PersonaId_SerializesToString()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        PersonaId personaId = characterId.ToPersonaId();
        string expected = $"\"Character:{characterId.Value}\"";

        // Act
        string json = JsonSerializer.Serialize(personaId);

        // Assert
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void PersonaId_DeserializesFromString()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        string json = $"\"Character:{characterId.Value}\"";

        // Act
        PersonaId personaId = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.Value.ToString()));
    }

    [Test]
    public void PersonaId_RoundTrip_Character()
    {
        // Arrange
        PersonaId original = CharacterId.New().ToPersonaId();

        // Act
        string json = JsonSerializer.Serialize(original);
        PersonaId deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_Organization()
    {
        // Arrange
        PersonaId original = OrganizationId.New().ToPersonaId();

        // Act
        string json = JsonSerializer.Serialize(original);
        PersonaId deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_Coinhouse()
    {
        // Arrange
        PersonaId original = PersonaId.FromCoinhouse(new CoinhouseTag("test-bank"));

        // Act
        string json = JsonSerializer.Serialize(original);
        PersonaId deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_Government()
    {
        // Arrange
        PersonaId original = GovernmentId.New().ToPersonaId();

        // Act
        string json = JsonSerializer.Serialize(original);
        PersonaId deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_RoundTrip_SystemProcess()
    {
        // Arrange
        PersonaId original = PersonaId.FromSystem("TaxCollector");

        // Act
        string json = JsonSerializer.Serialize(original);
        PersonaId deserialized = JsonSerializer.Deserialize<PersonaId>(json);

        // Assert
        Assert.That(deserialized, Is.EqualTo(original));
    }

    [Test]
    public void PersonaId_DeserializeInvalidFormat_ThrowsJsonException()
    {
        // Arrange
        string invalidJson = "\"InvalidFormat\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonaId>(invalidJson));
    }

    [Test]
    public void PersonaId_DeserializeInvalidType_ThrowsJsonException()
    {
        // Arrange
        string invalidJson = "\"InvalidType:12345\"";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonaId>(invalidJson));
    }

    [Test]
    public void PersonaId_DeserializeNull_ThrowsJsonException()
    {
        // Arrange
        string nullJson = "null";

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PersonaId>(nullJson));
    }

    [Test]
    public void PersonaId_InObject_SerializesCorrectly()
    {
        // Arrange
        TestClassWithPersonaId testObject = new TestClassWithPersonaId
        {
            Id = 123,
            PersonaId = CharacterId.New().ToPersonaId(),
            Name = "Test"
        };

        // Act
        string json = JsonSerializer.Serialize(testObject);
        TestClassWithPersonaId? deserialized = JsonSerializer.Deserialize<TestClassWithPersonaId>(json);

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
        List<PersonaId> personaIds = new List<PersonaId>
        {
            CharacterId.New().ToPersonaId(),
            OrganizationId.New().ToPersonaId(),
            PersonaId.FromCoinhouse(new CoinhouseTag("test-bank")),
            PersonaId.FromSystem("TestProcess")
        };

        // Act
        string json = JsonSerializer.Serialize(personaIds);
        List<PersonaId>? deserialized = JsonSerializer.Deserialize<List<PersonaId>>(json);

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
        Dictionary<string, PersonaId> dict = new Dictionary<string, PersonaId>
        {
            ["character"] = CharacterId.New().ToPersonaId(),
            ["organization"] = OrganizationId.New().ToPersonaId(),
            ["coinhouse"] = PersonaId.FromCoinhouse(new CoinhouseTag("test-bank"))
        };

        // Act
        string json = JsonSerializer.Serialize(dict);
        Dictionary<string, PersonaId>? deserialized = JsonSerializer.Deserialize<Dictionary<string, PersonaId>>(json);

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
        PersonaId personaId = CharacterId.New().ToPersonaId();
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Act
        string json = JsonSerializer.Serialize(personaId, options);
        PersonaId deserialized = JsonSerializer.Deserialize<PersonaId>(json, options);

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

