using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;

/// <summary>
/// Focused tests for <see cref="TouchPlayerPersonaCommandHandler"/>.
/// These run without any live NWN objects or database: the persistent persona
/// repository is an in-memory test double that records <see cref="Touch"/> calls.
/// </summary>
[TestFixture]
public class TouchPlayerPersonaCommandHandlerTests
{
    private InMemoryPersistentPlayerPersonaRepository _playerPersonas = null!;
    private TouchPlayerPersonaCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _playerPersonas = new InMemoryPersistentPlayerPersonaRepository();
        _handler = new TouchPlayerPersonaCommandHandler(_playerPersonas);
    }

    [Test]
    public async Task HandleAsync_TouchesPersonaWithCapturedIdentityAndTime()
    {
        // Arrange
        DateTime activated = new(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        // Act
        CommandResult result = await _handler.HandleAsync(new TouchPlayerPersonaCommand("CDKEY123", activated));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_playerPersonas.TouchCalls, Has.Count.EqualTo(1));
        (string cdKey, DateTime utc) = _playerPersonas.TouchCalls[0];
        Assert.That(cdKey, Is.EqualTo("CDKEY123"));
        Assert.That(utc, Is.EqualTo(activated));
    }

    [Test]
    public async Task HandleAsync_RepeatedTouch_ContinuesToRecordActivity()
    {
        // Act
        await _handler.HandleAsync(new TouchPlayerPersonaCommand("CDKEY123", DateTime.UtcNow));
        CommandResult result = await _handler.HandleAsync(new TouchPlayerPersonaCommand("CDKEY123", DateTime.UtcNow));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_playerPersonas.TouchCalls, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task HandleAsync_WithoutCancellation_TouchesPersona()
    {
        // Act
        CommandResult result = await _handler.HandleAsync(
            new TouchPlayerPersonaCommand("CDKEY123", DateTime.UtcNow), CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_playerPersonas.TouchCalls, Has.Count.EqualTo(1));
    }
}
