using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using PersonaRepositoryStub = AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers.InMemoryPersonaRepository;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaDescriptorServiceTests
{
    [Test]
    public void Describe_WithCharacterPersona_ReturnsOwnershipMetadata()
    {
    PersonaRepositoryStub personas = PersonaRepositoryStub.Create();
        CharacterPersona character = PersonaTestHelpers.CreateCharacterPersona("Arduin");
        personas.Add(character);

        FakeCharacterRepository characters = new();
        characters.AddCharacter(new PersistedCharacter
        {
            Id = character.CharacterId.Value,
            FirstName = "Arduin",
            LastName = "Storm",
            CdKey = "ABC12345"
        });

        PersonaDescriptorService service = new(personas, characters);

        PersonaDescriptor descriptor = service.Describe(character.Id);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Id, Is.EqualTo(character.Id));
            Assert.That(descriptor.DisplayName, Is.EqualTo(character.DisplayName));
            Assert.That(descriptor.CharacterId, Is.EqualTo(character.CharacterId));
            Assert.That(descriptor.HasKnownOwner, Is.True);
            Assert.That(descriptor.PrimaryOwnerCdKey, Is.EqualTo("ABC12345"));
        });
    }

    [Test]
    public void DescribeMany_IgnoresMissingPersonas()
    {
    PersonaRepositoryStub personas = PersonaRepositoryStub.Create();
        CharacterPersona character = PersonaTestHelpers.CreateCharacterPersona("Selune");
        personas.Add(character);

        FakeCharacterRepository characters = new();
        characters.AddCharacter(new PersistedCharacter
        {
            Id = character.CharacterId.Value,
            FirstName = "Selune",
            LastName = "Bright",
            CdKey = "SELU001"
        });

        PersonaDescriptorService service = new(personas, characters);

        PersonaId[] ids =
        {
            character.Id,
            PersonaId.FromSystem("missing")
        };

        IReadOnlyList<PersonaDescriptor> descriptors = service.DescribeMany(ids);

        Assert.That(descriptors.Count, Is.EqualTo(1));
        Assert.That(descriptors[0].Id, Is.EqualTo(character.Id));
        Assert.That(descriptors[0].PrimaryOwnerCdKey, Is.EqualTo("SELU001"));
    }

    [Test]
    public void TryDescribe_WhenPersonaMissing_ReturnsFalse()
    {
    PersonaDescriptorService service = new(PersonaRepositoryStub.Create(), new FakeCharacterRepository());

        bool result = service.TryDescribe(PersonaId.FromSystem("unknown"), out PersonaDescriptor? descriptor);

        Assert.That(result, Is.False);
        Assert.That(descriptor, Is.Null);
    }

    private sealed class FakeCharacterRepository : IPersistentCharacterRepository
    {
        private readonly Dictionary<Guid, PersistedCharacter> _characters = new();

        public void AddCharacter(PersistedCharacter character)
        {
            _characters[character.Id] = character;
        }

        public List<PersistedCharacter> GetCharacters()
        {
            return _characters.Values.ToList();
        }

        public PersistedCharacter? GetByGuid(Guid id)
        {
            _characters.TryGetValue(id, out PersistedCharacter? character);
            return character;
        }

        public List<PersistedCharacter> GetCharactersByCdKey(string cdKey)
        {
            return _characters.Values.Where(c => string.Equals(c.CdKey, cdKey, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public void ChangeCharacterOwner(PersistedCharacter character, string cdKey)
        {
            if (!_characters.ContainsKey(character.Id))
            {
                character.CdKey = cdKey;
                _characters[character.Id] = character;
                return;
            }

            _characters[character.Id].CdKey = cdKey;
        }

        public void DeleteCharacter(PersistedCharacter character)
        {
            _characters.Remove(character.Id);
        }

        public void SaveChanges()
        {
            // No persistence required for the fake implementation.
        }
    }
}
