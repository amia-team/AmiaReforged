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
        CharacterId characterId = CharacterId.New();
        PersonaId personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Aldric Stormblade");

        // Act
        bool found = _repository.TryGetPersona(personaId, out Persona? persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<CharacterPersona>());
        CharacterPersona charPersona = (CharacterPersona)persona!;
        Assert.That(charPersona.CharacterId, Is.EqualTo(characterId));
        Assert.That(charPersona.DisplayName, Is.EqualTo("Aldric Stormblade"));
    }

    [Test]
    public void TryGetPersona_Character_NotFound_ReturnsFalse()
    {
        // Arrange
        PersonaId personaId = CharacterId.New().ToPersonaId();

        // Act
        bool found = _repository.TryGetPersona(personaId, out Persona? persona);

        // Assert
        Assert.That(found, Is.False);
        Assert.That(persona, Is.Null);
    }

    [Test]
    public void TryGetPersona_Organization_Found_ReturnsTrue()
    {
        // Arrange
        OrganizationId orgId = OrganizationId.New();
        PersonaId personaId = orgId.ToPersonaId();
        _repository.AddOrganization(orgId, "Merchants Guild");

        // Act
        bool found = _repository.TryGetPersona(personaId, out Persona? persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<OrganizationPersona>());
        OrganizationPersona orgPersona = (OrganizationPersona)persona!;
        Assert.That(orgPersona.OrganizationId, Is.EqualTo(orgId));
        Assert.That(orgPersona.DisplayName, Is.EqualTo("Merchants Guild"));
    }

    [Test]
    public void TryGetPersona_Coinhouse_Found_ReturnsTrue()
    {
        // Arrange
        CoinhouseTag tag = new CoinhouseTag("cordor-bank");
        SettlementId settlement = SettlementId.Parse(1);
        PersonaId personaId = PersonaId.FromCoinhouse(tag);
        _repository.AddCoinhouse(tag, settlement, "Cordor Central Bank");

        // Act
        bool found = _repository.TryGetPersona(personaId, out Persona? persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<CoinhousePersona>());
        CoinhousePersona coinhousePersona = (CoinhousePersona)persona!;
        Assert.That(coinhousePersona.Tag, Is.EqualTo(tag));
        Assert.That(coinhousePersona.Settlement, Is.EqualTo(settlement));
    }

    [Test]
    public void TryGetPersona_SystemProcess_AlwaysReturns()
    {
        // Arrange
        PersonaId personaId = PersonaId.FromSystem("TaxCollector");

        // Act
        bool found = _repository.TryGetPersona(personaId, out Persona? persona);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<SystemPersona>());
        SystemPersona systemPersona = (SystemPersona)persona!;
        Assert.That(systemPersona.ProcessName, Is.EqualTo("TaxCollector"));
    }

    [Test]
    public void GetPersona_Found_ReturnsPersona()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        PersonaId personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Test Character");

        // Act
        Persona persona = _repository.GetPersona(personaId);

        // Assert
        Assert.That(persona, Is.Not.Null);
        Assert.That(persona, Is.InstanceOf<CharacterPersona>());
    }

    [Test]
    public void GetPersona_NotFound_ThrowsException()
    {
        // Arrange
        PersonaId personaId = CharacterId.New().ToPersonaId();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _repository.GetPersona(personaId));
    }

    [Test]
    public void Exists_Character_Found_ReturnsTrue()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        PersonaId personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Test");

        // Act
        bool exists = _repository.Exists(personaId);

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public void Exists_Character_NotFound_ReturnsFalse()
    {
        // Arrange
        PersonaId personaId = CharacterId.New().ToPersonaId();

        // Act
        bool exists = _repository.Exists(personaId);

        // Assert
        Assert.That(exists, Is.False);
    }

    [Test]
    public void Exists_SystemProcess_AlwaysReturnsTrue()
    {
        // Arrange
        PersonaId personaId = PersonaId.FromSystem("AnyProcess");

        // Act
        bool exists = _repository.Exists(personaId);

        // Assert
        Assert.That(exists, Is.True);
    }

    [Test]
    public void GetDisplayName_Character_ReturnsName()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        PersonaId personaId = characterId.ToPersonaId();
        _repository.AddCharacter(characterId, "Aldric Stormblade");

        // Act
        string? displayName = _repository.GetDisplayName(personaId);

        // Assert
        Assert.That(displayName, Is.EqualTo("Aldric Stormblade"));
    }

    [Test]
    public void GetDisplayName_NotFound_ReturnsNull()
    {
        // Arrange
        PersonaId personaId = CharacterId.New().ToPersonaId();

        // Act
        string? displayName = _repository.GetDisplayName(personaId);

        // Assert
        Assert.That(displayName, Is.Null);
    }

    [Test]
    public void GetPersonas_MultipleTypes_ReturnsAll()
    {
        // Arrange
        CharacterId char1 = CharacterId.New();
        CharacterId char2 = CharacterId.New();
        OrganizationId org1 = OrganizationId.New();
        CoinhouseTag tag1 = new CoinhouseTag("bank1");

        _repository.AddCharacter(char1, "Character 1");
        _repository.AddCharacter(char2, "Character 2");
        _repository.AddOrganization(org1, "Organization 1");
        _repository.AddCoinhouse(tag1, SettlementId.Parse(1), "Bank 1");

        List<PersonaId> personaIds = new List<PersonaId>
        {
            char1.ToPersonaId(),
            char2.ToPersonaId(),
            org1.ToPersonaId(),
            PersonaId.FromCoinhouse(tag1),
            PersonaId.FromSystem("System1")
        };

        // Act
        Dictionary<PersonaId, Persona> personas = _repository.GetPersonas(personaIds);

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
        CharacterId char1 = CharacterId.New();
        CharacterId char2 = CharacterId.New(); // Not added
        _repository.AddCharacter(char1, "Character 1");

        List<PersonaId> personaIds = new List<PersonaId>
        {
            char1.ToPersonaId(),
            char2.ToPersonaId()
        };

        // Act
        Dictionary<PersonaId, Persona> personas = _repository.GetPersonas(personaIds);

        // Assert
        Assert.That(personas.Count, Is.EqualTo(1));
        Assert.That(personas.ContainsKey(char1.ToPersonaId()), Is.True);
        Assert.That(personas.ContainsKey(char2.ToPersonaId()), Is.False);
    }

    [Test]
    public void GetPersonas_EmptyList_ReturnsEmpty()
    {
        // Act
        Dictionary<PersonaId, Persona> personas = _repository.GetPersonas(new List<PersonaId>());

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
                if (Guid.TryParse(personaId.Value, out Guid charGuid) && _characters.TryGetValue(charGuid, out (CharacterId Id, string Name) charData))
                {
                    persona = CharacterPersona.Create(charData.Id, charData.Name);
                    return true;
                }
                break;

            case PersonaType.Organization:
                if (Guid.TryParse(personaId.Value, out Guid orgGuid) && _organizations.TryGetValue(orgGuid, out (OrganizationId Id, string Name) orgData))
                {
                    persona = OrganizationPersona.Create(orgData.Id, orgData.Name);
                    return true;
                }
                break;

            case PersonaType.Coinhouse:
                if (_coinhouses.TryGetValue(personaId.Value, out (CoinhouseTag Tag, SettlementId Settlement, string Name) coinhouseData))
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
        if (!TryGetPersona(personaId, out Persona? persona) || persona == null)
        {
            throw new InvalidOperationException($"Persona not found: {personaId}");
        }
        return persona;
    }

    public bool Exists(PersonaId personaId)
    {
        return personaId.Type switch
        {
            PersonaType.Character => Guid.TryParse(personaId.Value, out Guid charGuid) && _characters.ContainsKey(charGuid),
            PersonaType.Organization => Guid.TryParse(personaId.Value, out Guid orgGuid) && _organizations.ContainsKey(orgGuid),
            PersonaType.Coinhouse => _coinhouses.ContainsKey(personaId.Value),
            PersonaType.SystemProcess => true,
            _ => false
        };
    }

    public string? GetDisplayName(PersonaId personaId)
    {
        return personaId.Type switch
        {
            PersonaType.Character when Guid.TryParse(personaId.Value, out Guid charGuid) && _characters.TryGetValue(charGuid, out (CharacterId Id, string Name) charData)
                => charData.Name,
            PersonaType.Organization when Guid.TryParse(personaId.Value, out Guid orgGuid) && _organizations.TryGetValue(orgGuid, out (OrganizationId Id, string Name) orgData)
                => orgData.Name,
            PersonaType.Coinhouse when _coinhouses.TryGetValue(personaId.Value, out (CoinhouseTag Tag, SettlementId Settlement, string Name) coinhouseData)
                => coinhouseData.Name,
            PersonaType.SystemProcess => $"System: {personaId.Value}",
            _ => null
        };
    }

    public Dictionary<PersonaId, Persona> GetPersonas(IEnumerable<PersonaId> personaIds)
    {
        Dictionary<PersonaId, Persona> result = new Dictionary<PersonaId, Persona>();

        foreach (PersonaId personaId in personaIds)
        {
            if (TryGetPersona(personaId, out Persona? persona) && persona != null)
            {
                result[personaId] = persona;
            }
        }

        return result;
    }
}

