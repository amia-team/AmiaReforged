using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;

/// <summary>
/// Focused tests for <see cref="ObservePlayerPersonaCommandHandler"/>.
/// These run without any live NWN objects or database: the persistent persona
/// repository is an in-memory test double.
/// </summary>
[TestFixture]
public class ObservePlayerPersonaCommandHandlerTests
{
    private InMemoryPersistentPlayerPersonaRepository _playerPersonas = null!;
    private ObservePlayerPersonaCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _playerPersonas = new InMemoryPersistentPlayerPersonaRepository();
        _handler = new ObservePlayerPersonaCommandHandler(_playerPersonas);
    }

    [Test]
    public async Task HandleAsync_CallsUpsertWithCapturedIdentityDisplayNameAndTime()
    {
        // Arrange
        DateTime observed = new(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        // Act
        CommandResult result = await _handler.HandleAsync(
            new ObservePlayerPersonaCommand("CDKEY123", "PlayerName", observed));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_playerPersonas.UpsertCalls, Has.Count.EqualTo(1));
        (string cdKey, string displayName, DateTime? utc) = _playerPersonas.UpsertCalls[0];
        Assert.That(cdKey, Is.EqualTo("CDKEY123"));
        Assert.That(displayName, Is.EqualTo("PlayerName"));
        Assert.That(utc, Is.EqualTo(observed));
    }

    [Test]
    public async Task HandleAsync_RepeatedObservation_ContinuesToUpsert()
    {
        // Act
        await _handler.HandleAsync(new ObservePlayerPersonaCommand("CDKEY123", "First", DateTime.UtcNow));
        CommandResult result = await _handler.HandleAsync(
            new ObservePlayerPersonaCommand("CDKEY123", "Second", DateTime.UtcNow));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_playerPersonas.UpsertCalls, Has.Count.EqualTo(2));
        Assert.That(_playerPersonas.UpsertCalls[1].DisplayName, Is.EqualTo("Second"));
    }
}
