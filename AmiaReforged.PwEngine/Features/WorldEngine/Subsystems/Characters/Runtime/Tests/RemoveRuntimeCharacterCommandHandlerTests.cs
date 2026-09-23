using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;

/// <summary>
/// Focused tests for <see cref="RemoveRuntimeCharacterCommandHandler"/>.
/// These run without any live NWN objects: the repository is an in-memory test
/// double.
/// </summary>
[TestFixture]
public class RemoveRuntimeCharacterCommandHandlerTests
{
    private readonly Guid TestCharacterId = Guid.NewGuid();

    private InMemoryCharacterRepository _repository = null!;
    private RemoveRuntimeCharacterCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCharacterRepository();
        _handler = new RemoveRuntimeCharacterCommandHandler(_repository);
    }

    [Test]
    public async Task Removal_DeletesByIdWithCorrectGuidAndSucceeds()
    {
        // Arrange
        ICharacter character = Mock.Of<ICharacter>(c => c.GetId() == CharacterId.From(TestCharacterId));
        _repository.Add(character);
        Assert.That(_repository.Exists(TestCharacterId), Is.True);

        // Act
        CommandResult result = await _handler.HandleAsync(new RemoveRuntimeCharacterCommand(CharacterId.From(TestCharacterId)));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.DeletedIds, Has.Count.EqualTo(1));
        Assert.That(_repository.DeletedIds[0], Is.EqualTo(TestCharacterId));
        Assert.That(_repository.Exists(TestCharacterId), Is.False);
    }

    [Test]
    public async Task Removal_WhenMissing_IsIdempotentAndSucceeds()
    {
        // Arrange
        Assert.That(_repository.Exists(TestCharacterId), Is.False);

        // Act
        CommandResult result = await _handler.HandleAsync(new RemoveRuntimeCharacterCommand(CharacterId.From(TestCharacterId)));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.DeletedIds, Has.Count.EqualTo(1));
        Assert.That(_repository.DeletedIds[0], Is.EqualTo(TestCharacterId));
    }
}
