using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// Direct unit tests for the RemoveTraitCommandHandler.
/// The handler is instantiated directly and driven through HandleAsync against
/// an InMemoryCharacterTraitRepository.
/// </summary>
[TestFixture]
public class RemoveTraitCommandHandlerTests
{
    private const string BraveTag = "brave";
    private const string StrongTag = "strong";

    private static CharacterTrait NewTrait(CharacterId characterId, string tag) => new()
    {
        Id = Guid.NewGuid(),
        CharacterId = characterId,
        TraitTag = new TraitTag(tag),
        DateAcquired = DateTime.UtcNow,
        IsConfirmed = true,
        IsActive = true
    };

    [Test]
    public async Task RemoveTrait_WhenCharacterDoesNotHaveTrait_Fails()
    {
        // Arrange - a character that owns no traits
        CharacterId characterId = CharacterId.From(Guid.NewGuid());
        ICharacterTraitRepository repository = InMemoryCharacterTraitRepository.Create();
        RemoveTraitCommandHandler handler = new(repository);

        // When
        CommandResult result = await handler.HandleAsync(
            new RemoveTraitCommand(characterId, new TraitTag(BraveTag)));

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo($"Character does not have trait '{BraveTag}'."));
        Assert.That(repository.GetByCharacterId(characterId), Is.Empty);
    }

    [Test]
    public async Task RemoveTrait_WhenCharacterHasTrait_RemovesMatchingTrait()
    {
        // Arrange - a character owning two traits
        CharacterId characterId = CharacterId.From(Guid.NewGuid());
        ICharacterTraitRepository repository = InMemoryCharacterTraitRepository.Create();
        repository.Add(NewTrait(characterId, BraveTag));
        repository.Add(NewTrait(characterId, StrongTag));
        RemoveTraitCommandHandler handler = new(repository);

        // When
        CommandResult result = await handler.HandleAsync(
            new RemoveTraitCommand(characterId, new TraitTag(BraveTag)));

        // Then
        Assert.That(result.Success, Is.True);

        List<CharacterTrait> remaining = repository.GetByCharacterId(characterId);
        Assert.That(remaining, Has.Count.EqualTo(1));
        Assert.That(remaining.Any(t => t.TraitTag.Value == BraveTag), Is.False);
        Assert.That(remaining.Any(t => t.TraitTag.Value == StrongTag), Is.True);
    }

    [Test]
    public async Task RemoveTrait_DoesNotRemoveSameTraitFromAnotherCharacter()
    {
        // Arrange - two characters that both own the same trait
        CharacterId characterA = CharacterId.From(Guid.NewGuid());
        CharacterId characterB = CharacterId.From(Guid.NewGuid());
        ICharacterTraitRepository repository = InMemoryCharacterTraitRepository.Create();
        CharacterTrait aTrait = NewTrait(characterA, BraveTag);
        CharacterTrait bTrait = NewTrait(characterB, BraveTag);
        repository.Add(aTrait);
        repository.Add(bTrait);
        RemoveTraitCommandHandler handler = new(repository);

        // When - removal is requested for Character A only
        CommandResult result = await handler.HandleAsync(
            new RemoveTraitCommand(characterA, new TraitTag(BraveTag)));

        // Then
        Assert.That(result.Success, Is.True);

        Assert.That(repository.GetByCharacterId(characterA).Any(t => t.TraitTag.Value == BraveTag), Is.False);
        List<CharacterTrait> bTraits = repository.GetByCharacterId(characterB);
        Assert.That(bTraits, Has.Count.EqualTo(1));
        Assert.That(bTraits[0].Id, Is.EqualTo(bTrait.Id));
    }
}
