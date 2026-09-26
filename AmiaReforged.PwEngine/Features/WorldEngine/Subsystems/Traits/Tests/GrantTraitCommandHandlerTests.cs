using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// Direct unit coverage for the domain behavior of
/// <see cref="GrantTraitCommandHandler"/>. The handler is instantiated directly;
/// the command dispatcher is not exercised.
/// </summary>
[TestFixture]
public class GrantTraitCommandHandlerTests
{
    private const string BraveTraitTag = "brave";
    private const string HeroTraitTag = "hero";
    private const string MissingTraitTag = "missing_trait";

    private InMemoryTraitRepository _traitRepository = null!;
    private InMemoryCharacterTraitRepository _characterTraitRepository = null!;
    private GrantTraitCommandHandler _handler = null!;

    private CharacterId _characterId = CharacterId.New();

    [SetUp]
    public void SetUp()
    {
        _traitRepository = new InMemoryTraitRepository();
        _characterTraitRepository = new InMemoryCharacterTraitRepository();
        _handler = new GrantTraitCommandHandler(_traitRepository, _characterTraitRepository);
        _characterId = CharacterId.New();
    }

    private void AddDefinition(string tag, string name, bool requiresUnlock)
    {
        _traitRepository.Add(new Trait
        {
            Tag = tag,
            Name = name,
            Description = $"Definition for {tag}",
            PointCost = 0,
            RequiresUnlock = requiresUnlock
        });
    }

    [Test]
    public async Task GrantTrait_WhenDefinitionDoesNotExist_FailsWithoutCreatingCharacterTrait()
    {
        // Given - no definition for the requested trait
        GrantTraitCommand command = new(_characterId, new TraitTag(MissingTraitTag));

        // When
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Trait 'missing_trait' does not exist."));
        Assert.That(_characterTraitRepository.GetByCharacterId(_characterId), Is.Empty);
    }

    [Test]
    public async Task GrantTrait_WhenCharacterAlreadyHasTrait_FailsWithoutCreatingDuplicate()
    {
        // Given - definition exists and the character already holds the trait
        AddDefinition(BraveTraitTag, "Brave", requiresUnlock: false);

        CharacterTrait existing = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = _characterId,
            TraitTag = new TraitTag(BraveTraitTag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true,
            IsUnlocked = false
        };
        _characterTraitRepository.Add(existing);

        // When - grant the same trait again
        GrantTraitCommand command = new(_characterId, new TraitTag(BraveTraitTag));
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already has trait 'Brave'"));

        List<CharacterTrait> traits = _characterTraitRepository.GetByCharacterId(_characterId);
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].Id, Is.EqualTo(existing.Id));
    }

    [Test]
    public async Task GrantTrait_WhenValid_CreatesConfirmedActiveCharacterTrait()
    {
        // Given - a non-requires-unlock definition
        AddDefinition(BraveTraitTag, "Brave", requiresUnlock: false);

        DateTime before = DateTime.UtcNow;
        // When
        CommandResult result = await _handler.HandleAsync(
            new GrantTraitCommand(_characterId, new TraitTag(BraveTraitTag)),
            CancellationToken.None);
        DateTime after = DateTime.UtcNow;

        // Then
        Assert.That(result.Success, Is.True);

        List<CharacterTrait> traits = _characterTraitRepository.GetByCharacterId(_characterId);
        Assert.That(traits, Has.Count.EqualTo(1));

        CharacterTrait created = traits[0];
        Assert.That(created.CharacterId, Is.EqualTo(_characterId));
        Assert.That(created.TraitTag.Value, Is.EqualTo(BraveTraitTag));
        Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(created.IsConfirmed, Is.True);
        Assert.That(created.IsActive, Is.True);
        Assert.That(created.IsUnlocked, Is.False);
        Assert.That(created.DateAcquired, Is.InRange(before, after));
    }

    [Test]
    public async Task GrantTrait_WhenDefinitionRequiresUnlock_SeedsIsUnlockedTrue()
    {
        // Given - a definition that requires unlock
        AddDefinition(HeroTraitTag, "Hero", requiresUnlock: true);

        // When
        CommandResult result = await _handler.HandleAsync(
            new GrantTraitCommand(_characterId, new TraitTag(HeroTraitTag)),
            CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        List<CharacterTrait> traits = _characterTraitRepository.GetByCharacterId(_characterId);
        Assert.That(traits, Has.Count.EqualTo(1));

        CharacterTrait created = traits[0];
        Assert.That(created.IsConfirmed, Is.True);
        Assert.That(created.IsActive, Is.True);
        Assert.That(created.IsUnlocked, Is.True);
    }
}
