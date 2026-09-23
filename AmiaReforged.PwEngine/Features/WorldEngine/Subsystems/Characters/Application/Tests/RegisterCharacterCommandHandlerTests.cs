using System.Linq;
using System.Reflection;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Application;
using Anvil.Services;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Application.Tests;

/// <summary>
/// Focused tests for the RegisterCharacterCommandHandler covering new
/// character creation, existing-character no-op, and persona backfill.
/// </summary>
[TestFixture]
public class CharacterRegistrationCommandHandlerTests
{
    private static readonly Guid TestCharacterId = Guid.NewGuid();
    private const string TestFirstName = "Aela";
    private const string TestLastName = "the-Wolf";
    private const string TestCdKey = "TESTCDKEY";

    private InMemoryPersistentCharacterRepository _repository = null!;
    private RegisterCharacterCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryPersistentCharacterRepository();
        _handler = new RegisterCharacterCommandHandler(_repository);
    }

    private RegisterCharacterCommand Command() => new(
        CharacterId.From(TestCharacterId),
        TestFirstName,
        TestLastName,
        TestCdKey);

    [Test]
    public async Task NewCharacter_CreatesPersonaWithIdNamesAndCdKey()
    {
        // Arrange
        Assert.That(_repository.GetByGuid(TestCharacterId), Is.Null);

        CommandResult result = await _handler.HandleAsync(Command());

        // Act/Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.AddedCharacters, Has.Count.EqualTo(1));

        PersistedCharacter created = _repository.AddedCharacters[0];
        Assert.That(created.Id, Is.EqualTo(TestCharacterId));
        Assert.That(created.FirstName, Is.EqualTo(TestFirstName));
        Assert.That(created.LastName, Is.EqualTo(TestLastName));
        Assert.That(created.CdKey, Is.EqualTo(TestCdKey));
        Assert.That(created.PersonaIdString, Is.EqualTo(CharacterId.From(TestCharacterId).ToPersonaId().ToString()));
    }

    [Test]
    public async Task ExistingCharacter_WithPersonaIdString_DoesNothing()
    {
        // Arrange
        _repository.AddCharacter(new PersistedCharacter
        {
            Id = TestCharacterId,
            FirstName = "Old",
            LastName = "Name",
            CdKey = TestCdKey,
            PersonaIdString = CharacterId.From(TestCharacterId).ToPersonaId().ToString()
        });
        int addedBefore = _repository.AddedCharacters.Count;

        CommandResult result = await _handler.HandleAsync(Command());

        // Act/Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.AddedCharacters.Count, Is.EqualTo(addedBefore));
        Assert.That(_repository.PersonaIdUpdates, Has.Count.EqualTo(0));

        PersistedCharacter existing = _repository.GetByGuid(TestCharacterId)!;
        Assert.That(existing.FirstName, Is.EqualTo("Old"));
        Assert.That(existing.LastName, Is.EqualTo("Name"));
        Assert.That(existing.CdKey, Is.EqualTo(TestCdKey));
    }

    [Test]
    public async Task ExistingCharacter_WithWhitespacePersonaIdString_BackfillsOnce()
    {
        // Arrange
        _repository.AddCharacter(new PersistedCharacter
        {
            Id = TestCharacterId,
            FirstName = "Old",
            LastName = "Name",
            CdKey = TestCdKey,
            PersonaIdString = "   "
        });
        int addedBefore = _repository.AddedCharacters.Count;

        CommandResult result = await _handler.HandleAsync(Command());

        // Act/Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.AddedCharacters.Count, Is.EqualTo(addedBefore));
        Assert.That(_repository.PersonaIdUpdates, Has.Count.EqualTo(1));

        (Guid Id, string PersonaIdString) update = _repository.PersonaIdUpdates[0];
        Assert.That(update.Id, Is.EqualTo(TestCharacterId));
        Assert.That(update.PersonaIdString, Is.EqualTo(CharacterId.From(TestCharacterId).ToPersonaId().ToString()));

        PersistedCharacter existing = _repository.GetByGuid(TestCharacterId)!;
        Assert.That(existing.FirstName, Is.EqualTo("Old"));
        Assert.That(existing.LastName, Is.EqualTo("Name"));
        Assert.That(existing.CdKey, Is.EqualTo(TestCdKey));
    }

    [Test]
    public async Task ExistingCharacter_WithNullPersonaIdString_BackfillsOnce()
    {
        // Arrange
        _repository.AddCharacter(new PersistedCharacter
        {
            Id = TestCharacterId,
            FirstName = "Old",
            LastName = "Name",
            CdKey = TestCdKey,
            PersonaIdString = null
        });
        int addedBefore = _repository.AddedCharacters.Count;

        CommandResult result = await _handler.HandleAsync(Command());

        // Act/Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.AddedCharacters.Count, Is.EqualTo(addedBefore));
        Assert.That(_repository.PersonaIdUpdates, Has.Count.EqualTo(1));
        Assert.That(_repository.PersonaIdUpdates[0].PersonaIdString, Is.EqualTo(CharacterId.From(TestCharacterId).ToPersonaId().ToString()));
    }

    [Test]
    public async Task Backfill_DoesNotCreateDuplicateCharacter()
    {
        // Arrange
        _repository.AddCharacter(new PersistedCharacter
        {
            Id = TestCharacterId,
            FirstName = "Old",
            LastName = "Name",
            CdKey = TestCdKey,
            PersonaIdString = null
        });
        int characterCountBefore = _repository.GetCharacters().Count;

        await _handler.HandleAsync(Command());

        // Act/Assert
        Assert.That(_repository.GetCharacters().Count, Is.EqualTo(characterCountBefore));
        Assert.That(_repository.GetByGuid(TestCharacterId)!.PersonaIdString, Is.Not.Null);
    }

    [Test]
    public void Service_Dependencies_AreCommandDispatcherNotRepository()
    {
        // Assert: the service is wired to ICommandDispatcher (not a repository) and
        // exposes the documented (ICommandDispatcher, RuntimeCharacterService) constructor.
        Type type = typeof(CharacterRegistrationService);

        IEnumerable<Type> paramTypes = type.GetConstructors()
            .Select(c => c.GetParameters().Select(p => p.ParameterType).ToList())
            .SelectMany(l => l);

        Assert.That(paramTypes.Contains(typeof(ICommandDispatcher)),
            Is.True, "Expected an ICommandDispatcher constructor parameter.");
        Assert.That(paramTypes, Does.Not.Contains(typeof(IPersistentCharacterRepository)),
            "Service must not reference IPersistentCharacterRepository.");

        Assert.That(type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(f => f.FieldType),
            Does.Not.Contains(typeof(IPersistentCharacterRepository)),
            "Service field must not reference IPersistentCharacterRepository.");
    }
}
