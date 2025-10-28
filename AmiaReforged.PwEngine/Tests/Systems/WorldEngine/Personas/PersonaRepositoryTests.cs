using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaRepositoryTests
{
    private InMemoryPersonaRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryPersonaRepository();
    }

    [Test]
    public void TryGetPersona_Character_Found_ReturnsTrue()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Aldric Stormblade");

        // Act
        var found = _repository.TryGetPersona(personaId, out var persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<CharacterPersona>());
        var charPersona = (CharacterPersona)persona!;
        Assert.That(charPersona.CharacterId, Is.EqualTo(characterId));
        Assert.That(charPersona.DisplayName, Is.EqualTo("Aldric Stormblade"));
    }

    [Test]
    public void TryGetPersona_Character_NotFound_ReturnsFalse()
    {
        // Arrange
        var personaId = CharacterId.New().ToPersonaId();

        // Act
        var found = _repository.TryGetPersona(personaId, out var persona);

        // Assert
        Assert.That(found, Is.False);
        Assert.That(persona, Is.Null);
    }

    [Test]
    public void TryGetPersona_Organization_Found_ReturnsTrue()
    {
        // Arrange
        var orgId = OrganizationId.New();
        var personaId = orgId.ToPersonaId();
        _repository.AddOrganization(orgId, "Merchants Guild");

        // Act
        var found = _repository.TryGetPersona(personaId, out var persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<OrganizationPersona>());
        var orgPersona = (OrganizationPersona)persona!;
        Assert.That(orgPersona.OrganizationId, Is.EqualTo(orgId));
        Assert.That(orgPersona.DisplayName, Is.EqualTo("Merchants Guild"));
    }

    [Test]
    public void TryGetPersona_Coinhouse_Found_ReturnsTrue()
    {
        // Arrange
        var tag = new CoinhouseTag("cordor-bank");
        var settlement = SettlementId.Parse(1);
        var personaId = PersonaId.FromCoinhouse(tag);
        _repository.AddCoinhouse(tag, settlement, "Cordor Central Bank");

        // Act
        var found = _repository.TryGetPersona(personaId, out var persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<CoinhousePersona>());
        var coinhousePersona = (CoinhousePersona)persona!;
        Assert.That(coinhousePersona.Tag, Is.EqualTo(tag));
        Assert.That(coinhousePersona.Settlement, Is.EqualTo(settlement));
    }

    [Test]
    public void TryGetPersona_SystemProcess_AlwaysReturns()
    {
        // Arrange
        var personaId = PersonaId.FromSystem("TaxCollector");

        // Act
        var found = _repository.TryGetPersona(personaId, out var persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<SystemPersona>());
        var systemPersona = (SystemPersona)persona!;
        Assert.That(systemPersona.ProcessName, Is.EqualTo("TaxCollector"));
    }

    [Test]
    public void GetPersona_Found_ReturnsPersona()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Test Character");

        // Act
        var persona = _repository.GetPersona(personaId);

        // Assert
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<CharacterPersona>());
    }

    [Test]
    public void GetPersona_NotFound_ThrowsException()
    {
        // Arrange
        var personaId = CharacterId.New().ToPersonaId();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _repository.GetPersona(personaId));
    }

    [Test]
    public void Exists_Character_Found_ReturnsTrue()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Test");

        // Act
        var exists = _repository.Exists(personaId);

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public void Exists_Character_NotFound_ReturnsFalse()
    {
        // Arrange
        var personaId = CharacterId.New().ToPersonaId();

        // Act
        var exists = _repository.Exists(personaId);

        // Assert
        Assert.That(exists, Is.False);
    }

    [Test]
    public void Exists_SystemProcess_AlwaysReturnsTrue()
    {
        // Arrange
        var personaId = PersonaId.FromSystem("AnyProcess");

        // Act
        var exists = _repository.Exists(personaId);

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public void GetDisplayName_Character_ReturnsName()
    {
        // Arrange
        var characterId = CharacterId.New();
        var personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Aldric Stormblade");

        // Act
        var displayName = _repository.GetDisplayName(personaId);

        // Assert
        Assert.That(displayName, Is.EqualTo("Aldric Stormblade"));
    }

    [Test]
    public void GetDisplayName_NotFound_ReturnsNull()
    {
        // Arrange
        var personaId = CharacterId.New().ToPersonaId();

        // Act
        var displayName = _repository.GetDisplayName(personaId);

        // Assert
        Assert.That(displayName, Is.Null);
    }

    [Test]
    public void GetPersonas_MultipleTypes_ReturnsAll()
    {
        // Arrange
        var char1 = CharacterId.New();
        var char2 = CharacterId.New();
        var org1 = OrganizationId.New();
        var tag1 = new CoinhouseTag("bank1");

        _repository.AddCharacter(char1, "Character 1");
        _repository.AddCharacter(char2, "Character 2");
        _repository.AddOrganization(org1, "Organization 1");
        _repository.AddCoinhouse(tag1, SettlementId.Parse(1), "Bank 1");

        var personaIds = new List<PersonaId>
        {
            char1.ToPersonaId(),
            char2.ToPersonaId(),
            org1.ToPersonaId(),
            PersonaId.FromCoinhouse(tag1),
            PersonaId.FromSystem("System1")
        };

        // Act
        var personas = _repository.GetPersonas(personaIds);

        // Assert
        Assert.That(personas.Count, Is.EqualTo(5));
        Assert.That(personas[char1.ToPersonaId()], Is.InstanceOf<CharacterPersona>());
        Assert.That(personas[char2.ToPersonaId()], Is.InstanceOf<CharacterPersona>());
        Assert.That(personas[org1.ToPersonaId()], Is.InstanceOf<OrganizationPersona>());
        Assert.That(personas[PersonaId.FromCoinhouse(tag1)], Is.InstanceOf<CoinhousePersona>());
        Assert.That(personas[PersonaId.FromSystem("System1")], Is.InstanceOf<SystemPersona>());
    }

    [Test]
    public void GetPersonas_SomeNotFound_ReturnsOnlyFound()
    {
        // Arrange
        var char1 = CharacterId.New();
        var char2 = CharacterId.New(); // Not added
        _repository.AddCharacter(char1, "Character 1");

        var personaIds = new List<PersonaId>
        {
            char1.ToPersonaId(),
            char2.ToPersonaId()
        };

        // Act
        var personas = _repository.GetPersonas(personaIds);

        // Assert
        Assert.That(personas.Count, Is.EqualTo(1));
        Assert.That(personas.ContainsKey(char1.ToPersonaId()), Is.True);
        Assert.That(personas.ContainsKey(char2.ToPersonaId()), Is.False);
    }

    [Test]
    public void GetPersonas_EmptyList_ReturnsEmpty()
    {
        // Act
        var personas = _repository.GetPersonas(new List<PersonaId>());

        // Assert
        Assert.That(personas.Count, Is.EqualTo(0));
    }
}

/// <summary>
/// In-memory implementation of IPersonaRepository for testing.
/// </summary>
public class InMemoryPersonaRepository : IPersonaRepository
{
    private readonly Dictionary<Guid, (CharacterId Id, string Name)> _characters = new();
    private readonly Dictionary<Guid, (OrganizationId Id, string Name)> _organizations = new();
    private readonly Dictionary<string, (CoinhouseTag Tag, SettlementId Settlement, string Name)> _coinhouses = new();

    public void AddCharacter(CharacterId id, string name)
    {
        _characters[id.Value] = (id, name);
    }

    public void AddOrganization(OrganizationId id, string name)
    {
        _organizations[id.Value] = (id, name);
    }

    public void AddCoinhouse(CoinhouseTag tag, SettlementId settlement, string name)
    {
        _coinhouses[tag.Value] = (tag, settlement, name);
    }

    public bool TryGetPersona(PersonaId personaId, out Persona? persona)
    {
        persona = null;

        switch (personaId.Type)
        {
            case PersonaType.Character:
                if (Guid.TryParse(personaId.Value, out var charGuid) && _characters.TryGetValue(charGuid, out var charData))
                {
                    persona = CharacterPersona.Create(charData.Id, charData.Name);
                    return true;
                }
                break;

            case PersonaType.Organization:
                if (Guid.TryParse(personaId.Value, out var orgGuid) && _organizations.TryGetValue(orgGuid, out var orgData))
                {
                    persona = OrganizationPersona.Create(orgData.Id, orgData.Name);
                    return true;
                }
                break;

            case PersonaType.Coinhouse:
                if (_coinhouses.TryGetValue(personaId.Value, out var coinhouseData))
                {
                    persona = CoinhousePersona.Create(coinhouseData.Tag, coinhouseData.Settlement, coinhouseData.Name);
                    return true;
                }
                break;

            case PersonaType.SystemProcess:
                persona = SystemPersona.Create(personaId.Value);
                return true;
        }

        return false;
    }

    public Persona GetPersona(PersonaId personaId)
    {
        if (!TryGetPersona(personaId, out var persona) || persona == null)
        {
            throw new InvalidOperationException($"Persona not found: {personaId}");
        }
        return persona;
    }

    public bool Exists(PersonaId personaId)
    {
        return personaId.Type switch
        {
            PersonaType.Character => Guid.TryParse(personaId.Value, out var charGuid) && _characters.ContainsKey(charGuid),
            PersonaType.Organization => Guid.TryParse(personaId.Value, out var orgGuid) && _organizations.ContainsKey(orgGuid),
            PersonaType.Coinhouse => _coinhouses.ContainsKey(personaId.Value),
            PersonaType.SystemProcess => true,
            _ => false
        };
    }

    public string? GetDisplayName(PersonaId personaId)
    {
        return personaId.Type switch
        {
            PersonaType.Character when Guid.TryParse(personaId.Value, out var charGuid) && _characters.TryGetValue(charGuid, out var charData)
                => charData.Name,
            PersonaType.Organization when Guid.TryParse(personaId.Value, out var orgGuid) && _organizations.TryGetValue(orgGuid, out var orgData)
                => orgData.Name,
            PersonaType.Coinhouse when _coinhouses.TryGetValue(personaId.Value, out var coinhouseData)
                => coinhouseData.Name,
            PersonaType.SystemProcess => $"System: {personaId.Value}",
            _ => null
        };
    }

    public Dictionary<PersonaId, Persona> GetPersonas(IEnumerable<PersonaId> personaIds)
    {
        var result = new Dictionary<PersonaId, Persona>();

        foreach (var personaId in personaIds)
        {
            if (TryGetPersona(personaId, out var persona) && persona != null)
            {
                result[personaId] = persona;
            }
        }

        return result;
    }
}

