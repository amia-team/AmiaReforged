using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.Commands;

[TestFixture]
public class ReleasePlayerStallCommandHandlerTests
{
    private Mock<IPlayerStallService> _service = null!;
    private ReleasePlayerStallCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IPlayerStallService>();
        _handler = new ReleasePlayerStallCommandHandler(_service.Object);
    }

    [Test]
    public async Task HandleAsync_WhenServiceSucceeds_ReturnsSuccess()
    {
        ReleasePlayerStallCommand command = ReleasePlayerStallCommand.Create(
            stallId: 99,
            requestor: PersonaId.FromCharacter(CharacterId.New()));

        IReadOnlyDictionary<string, object> payload = new Dictionary<string, object>
        {
            ["stallId"] = 99L,
            ["releasedUtc"] = DateTime.UtcNow
        };

        _service
            .Setup(s => s.ReleaseAsync(It.IsAny<ReleasePlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerStallServiceResult.Ok(payload));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["stallId"], Is.EqualTo(99L));
        _service.Verify(s => s.ReleaseAsync(It.IsAny<ReleasePlayerStallRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenServiceFails_ReturnsFailure()
    {
        ReleasePlayerStallCommand command = ReleasePlayerStallCommand.Create(
            stallId: 100,
            requestor: PersonaId.FromCharacter(CharacterId.New()),
            force: true);

        _service
            .Setup(s => s.ReleaseAsync(It.IsAny<ReleasePlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerStallServiceResult.Fail(PlayerStallError.NotOwner, "not owner"));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("not owner"));
        _service.Verify(s => s.ReleaseAsync(It.IsAny<ReleasePlayerStallRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_PassesCommandDataToService()
    {
        ReleasePlayerStallRequest? captured = null;
        PersonaId persona = PersonaId.FromCharacter(CharacterId.New());
        ReleasePlayerStallCommand command = ReleasePlayerStallCommand.Create(77, persona, force: true);

        _service
            .Setup(s => s.ReleaseAsync(It.IsAny<ReleasePlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReleasePlayerStallRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(PlayerStallServiceResult.Ok());

        await _handler.HandleAsync(command);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.StallId, Is.EqualTo(command.StallId));
            Assert.That(captured!.Requestor, Is.EqualTo(command.Requestor));
            Assert.That(captured!.Force, Is.True);
        });
    }
}
