using System;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops.Commands;

[TestFixture]
public class ClaimPlayerStallCommandHandlerTests
{
    private Mock<IPlayerStallService> _service = null!;
    private ClaimPlayerStallCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IPlayerStallService>();
        _handler = new ClaimPlayerStallCommandHandler(_service.Object);
    }

    [Test]
    public async Task HandleAsync_WhenServiceSucceeds_ReturnsSuccessResult()
    {
        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            stallId: 42,
            ownerPersona: PersonaId.FromCharacter(CharacterId.New()),
            ownerDisplayName: "Aria Moonwhisper");

        IReadOnlyDictionary<string, object> payload = new Dictionary<string, object>
        {
            ["stallId"] = 42L
        };

        _service
            .Setup(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerStallServiceResult.Ok(payload));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
    Assert.That(result.Data, Is.Not.Null);
    Assert.That(result.Data!.TryGetValue("stallId", out object? value), Is.True);
    Assert.That(value, Is.EqualTo(42L));
    _service.Verify(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenServiceFails_ReturnsFailureResult()
    {
        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            stallId: 42,
            ownerPersona: PersonaId.FromCharacter(CharacterId.New()),
            ownerDisplayName: "Aria");

        _service
            .Setup(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerStallServiceResult.Fail(PlayerStallError.PersistenceFailure, "persist failure"));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("persist failure"));
    _service.Verify(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PassesCommandDataToService()
    {
        ClaimPlayerStallRequest? captured = null;
        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            stallId: 99,
            ownerPersona: PersonaId.FromCharacter(CharacterId.New()),
            ownerDisplayName: "Aria",
            rentInterval: TimeSpan.FromHours(6));

        _service
            .Setup(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ClaimPlayerStallRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(PlayerStallServiceResult.Ok());

        await _handler.HandleAsync(command);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.StallId, Is.EqualTo(command.StallId));
            Assert.That(captured!.OwnerPersona, Is.EqualTo(command.OwnerPersona));
            Assert.That(captured!.OwnerDisplayName, Is.EqualTo(command.OwnerDisplayName));
            Assert.That(captured!.NextRentDueUtc, Is.EqualTo(command.NextRentDueUtc));
        });
        _service.Verify(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
