using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Tests;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Commands.Tests;

/// <summary>
/// Focused tests for <see cref="UpdateCharacterStatsCommandHandler"/>. Uses the
/// in-memory repository double; requires no live NWN objects or database.
/// </summary>
[TestFixture]
public class UpdateCharacterStatsCommandHandlerTests
{
    private static readonly Guid TestCharacterId = Guid.NewGuid();

    private InMemoryCharacterStatRepository _repository = null!;
    private UpdateCharacterStatsCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCharacterStatRepository();
        _handler = new UpdateCharacterStatsCommandHandler(_repository);
    }

    private UpdateCharacterStatsCommand Command(int playTime) =>
        new(CharacterId.From(TestCharacterId), playTime);

    [Test]
    public async Task HandleAsync_KnownCharacter_RoundTripsPlayTime()
    {
        // Arrange
        _repository.Seed(new CharacterStatistics { CharacterId = TestCharacterId, PlayTime = 1 });

        // Act
        CommandResult result = await _handler.HandleAsync(Command(99));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_repository.GetCharacterStatistics(TestCharacterId)!.PlayTime, Is.EqualTo(99));
        Assert.That(_repository.SaveCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_MissingCharacter_Fails()
    {
        // Act
        CommandResult result = await _handler.HandleAsync(Command(99));

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Empty);
        Assert.That(_repository.SaveCount, Is.EqualTo(0));
    }

    [Test]
    public async Task HandleAsync_WritesOnlyPlayTimeLeavesOtherFieldsUntouched()
    {
        // Arrange
        _repository.Seed(new CharacterStatistics
        {
            CharacterId = TestCharacterId,
            PlayTime = 1,
            TimesRankedUp = 5,
            IndustriesJoined = 3,
            TimesDied = 9,
            KnowledgePoints = 100
        });

        // Act
        CommandResult result = await _handler.HandleAsync(Command(50));

        // Assert: only play time changed.
        Assert.That(result.Success, Is.True);
        CharacterStatistics stats = _repository.GetCharacterStatistics(TestCharacterId)!;
        Assert.That(stats.PlayTime, Is.EqualTo(50));
        Assert.That(stats.TimesRankedUp, Is.EqualTo(5));
        Assert.That(stats.IndustriesJoined, Is.EqualTo(3));
        Assert.That(stats.TimesDied, Is.EqualTo(9));
        Assert.That(stats.KnowledgePoints, Is.EqualTo(100));
    }

    [Test]
    public async Task CommandThenQuery_RoundTripsPlayTimeThroughSharedRepository()
    {
        // Arrange: a shared in-memory repository drives both the command and the query.
        InMemoryCharacterStatRepository repository = new();
        repository.Seed(new CharacterStatistics { CharacterId = TestCharacterId, PlayTime = 1 });
        UpdateCharacterStatsCommandHandler writer = new(repository);
        GetCharacterStatsQueryHandler reader = new(repository);

        // Act
        CommandResult write = await writer.HandleAsync(Command(123));
        CharacterStats? read = await reader.HandleAsync(new GetCharacterStatsQuery(CharacterId.From(TestCharacterId)));

        // Assert: the value written by the command is the value read back.
        Assert.That(write.Success, Is.True);
        Assert.That(read, Is.Not.Null);
        Assert.That(read!.PlayTime, Is.EqualTo(123));
    }

    [Test]
    public async Task Dispatched_SuccessfulWritePublishesCommandExecutedEvent()
    {
        // Arrange
        _repository.Seed(new CharacterStatistics { CharacterId = TestCharacterId, PlayTime = 1 });
        InMemoryEventBus eventBus = new();
        CommandDispatcher dispatcher = new(new ICommandHandlerMarker[] { _handler }, eventBus);

        // Act
        CommandResult result = await dispatcher.DispatchAsync(Command(77));

        // Assert: the generic command-executed event was published once.
        Assert.That(result.Success, Is.True);
        IReadOnlyList<IDomainEvent> published = eventBus.PublishedEvents;
        Assert.That(published, Has.Count.EqualTo(1));
        Assert.That(published[0], Is.InstanceOf<CommandExecutedEvent<UpdateCharacterStatsCommand>>());
    }
}
