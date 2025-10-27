using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaIdConversionTests
{
    [Test]
    public void CharacterId_ToPersonaId_CreatesCorrectPersonaId()
    {
        // Arrange
        var characterId = CharacterId.New();

        // Act
        var personaId = characterId.ToPersonaId();

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(personaId.Value, Is.EqualTo(characterId.Value.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Character:{characterId.Value}"));
    }

    [Test]
    public void OrganizationId_ToPersonaId_CreatesCorrectPersonaId()
    {
        // Arrange
        var orgId = OrganizationId.New();

        // Act
        var personaId = orgId.ToPersonaId();

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(personaId.Value, Is.EqualTo(orgId.Value.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Organization:{orgId.Value}"));
    }

    [Test]
    public void GovernmentId_ToPersonaId_CreatesCorrectPersonaId()
    {
        // Arrange
        var govId = GovernmentId.New();

        // Act
        var personaId = govId.ToPersonaId();

        // Assert
        Assert.That(personaId.Type, Is.EqualTo(PersonaType.Government));
        Assert.That(personaId.Value, Is.EqualTo(govId.Value.ToString()));
        Assert.That(personaId.ToString(), Is.EqualTo($"Government:{govId.Value}"));
    }

    [Test]
    public void CoinhouseTag_ToPersonaId_CreatesCorrectPersonaId()
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
    public void MultipleConversions_FromSameCharacterId_ProduceSamePersonaId()
    {
        // Arrange
        var characterId = CharacterId.New();

        // Act
        var personaId1 = characterId.ToPersonaId();
        var personaId2 = characterId.ToPersonaId();

        // Assert
        Assert.That(personaId1, Is.EqualTo(personaId2));
        Assert.That(personaId1.GetHashCode(), Is.EqualTo(personaId2.GetHashCode()));
    }

    [Test]
    public void DifferentIdTypes_WithSameGuid_ProduceDifferentPersonaIds()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var characterId = CharacterId.From(guid);
        var orgId = OrganizationId.From(guid);
        var govId = GovernmentId.From(guid);

        // Act
        var charPersonaId = characterId.ToPersonaId();
        var orgPersonaId = orgId.ToPersonaId();
        var govPersonaId = govId.ToPersonaId();

        // Assert - Same underlying value but different types
        Assert.That(charPersonaId.Value, Is.EqualTo(orgPersonaId.Value));
        Assert.That(charPersonaId.Value, Is.EqualTo(govPersonaId.Value));

        // But PersonaIds are NOT equal because types differ
        Assert.That(charPersonaId, Is.Not.EqualTo(orgPersonaId));
        Assert.That(charPersonaId, Is.Not.EqualTo(govPersonaId));
        Assert.That(orgPersonaId, Is.Not.EqualTo(govPersonaId));

        // Types are different
        Assert.That(charPersonaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(orgPersonaId.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(govPersonaId.Type, Is.EqualTo(PersonaType.Government));
    }

    [Test]
    public void PersonaId_ImplicitConversionToString_Works()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();

        // Act - Implicit conversion
        string personaIdString = personaId;

        // Assert
        Assert.That(personaIdString, Is.EqualTo($"Character:{characterId.Value}"));
    }

    [Test]
    public void PersonaId_CanBeUsedInDictionary()
    {
        // Arrange
        var charId = CharacterId.New();
        var orgId = OrganizationId.New();
        var dict = new Dictionary<PersonaId, string>();

        // Act
        dict[charId.ToPersonaId()] = "Character Data";
        dict[orgId.ToPersonaId()] = "Organization Data";

        // Assert
        Assert.That(dict[charId.ToPersonaId()], Is.EqualTo("Character Data"));
        Assert.That(dict[orgId.ToPersonaId()], Is.EqualTo("Organization Data"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void PersonaId_CanBeUsedInHashSet()
    {
        // Arrange
        var charId1 = CharacterId.New();
        var charId2 = CharacterId.New();
        var orgId = OrganizationId.New();
        var set = new HashSet<PersonaId>();

        // Act
        set.Add(charId1.ToPersonaId());
        set.Add(charId2.ToPersonaId());
        set.Add(orgId.ToPersonaId());
        set.Add(charId1.ToPersonaId());  // Duplicate

        // Assert
        Assert.That(set.Count, Is.EqualTo(3));  // Duplicate not added
        Assert.That(set.Contains(charId1.ToPersonaId()), Is.True);
        Assert.That(set.Contains(charId2.ToPersonaId()), Is.True);
        Assert.That(set.Contains(orgId.ToPersonaId()), Is.True);
    }

    [Test]
    public void ConvertedPersonaIds_CanBeStored_AndRetrieved()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();

        // Act - Store as string
        string storedValue = personaId.ToString();

        // Parse back
        var retrievedPersonaId = PersonaId.Parse(storedValue);

        // Assert
        Assert.That(retrievedPersonaId, Is.EqualTo(personaId));
        Assert.That(retrievedPersonaId.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(retrievedPersonaId.Value, Is.EqualTo(characterId.Value.ToString()));
    }

    [Test]
    public void AllIdTypes_ConvertToDistinctPersonaTypes()
    {
        // Arrange & Act
        var charPersonaId = CharacterId.New().ToPersonaId();
        var orgPersonaId = OrganizationId.New().ToPersonaId();
        var govPersonaId = GovernmentId.New().ToPersonaId();
        var coinhousePersonaId = PersonaId.FromCoinhouse(new CoinhouseTag("test"));
        var systemPersonaId = PersonaId.FromSystem("TestProcess");

        // Assert - All have different types
        var types = new[]
        {
            charPersonaId.Type,
            orgPersonaId.Type,
            govPersonaId.Type,
            coinhousePersonaId.Type,
            systemPersonaId.Type
        };

        Assert.That(types.Distinct().Count(), Is.EqualTo(5));
    }
}

