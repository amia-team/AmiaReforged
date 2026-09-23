using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;

/// <summary>
/// Focused tests for <see cref="RegisterRuntimeCharacterCommandHandler"/>.
/// These run without any live NWN objects: the character is a Moq double and
/// the repository is an in-memory test double.
/// </summary>
[TestFixture]
public class RegisterRuntimeCharacterCommandHandlerTests
{
    private readonly Guid TestCharacterId = Guid.NewGuid();

    private InMemoryCharacterRepository _repository = null!;
    private RegisterRuntimeCharacterCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCharacterRepository();
        _handler = new RegisterRuntimeCharacterCommandHandler(_repository);
    }

    private ICharacter Character() =>
        Mock.Of<ICharacter>(c => c.GetId() == CharacterId.From(TestCharacterId));

    [Test]
    public async Task NewCharacter_IsAddedOnceAndSucceeds()
    {
        // Arrange
        Assert.That(_repository.Exists(TestCharacterId), Is.False);

        // Act
        CommandResult result = await _handler.HandleAsync(new RegisterRuntimeCharacterCommand(Character()));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.AddedCharacters, Has.Count.EqualTo(1));
        Assert.That(_repository.GetById(TestCharacterId), Is.Not.Null);
    }

    [Test]
    public async Task ExistingCharacter_IsNotReplacedAndSucceeds()
    {
        // Arrange
        ICharacter existing = Character();
        _repository.Add(existing);
        int addedBefore = _repository.AddedCharacters.Count;

        // Act
        CommandResult result = await _handler.HandleAsync(new RegisterRuntimeCharacterCommand(Character()));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.AddedCharacters.Count, Is.EqualTo(addedBefore));
        Assert.That(_repository.GetById(TestCharacterId), Is.EqualTo(existing));
    }
}
