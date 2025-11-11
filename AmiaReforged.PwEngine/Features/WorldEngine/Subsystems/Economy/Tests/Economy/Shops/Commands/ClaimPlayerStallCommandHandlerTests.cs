using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy.Shops.Commands;

[TestFixture]
public class ClaimPlayerStallCommandHandlerTests
{
    private Mock<IPlayerStallService> _service = null!;
    private ClaimPlayerStallCommandHandler _handler = null!;
    private PersonaId _playerPersona;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IPlayerStallService>();
        _handler = new ClaimPlayerStallCommandHandler(_service.Object);
        _playerPersona = PersonaId.FromPlayerCdKey("TESTPLAYER");
    }

    [Test]
    public async Task HandleAsync_WhenServiceSucceeds_ReturnsSuccessResult()
    {
        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            stallId: 42,
            areaResRef: "market_area",
            placeableTag: "stall_42",
            ownerPlayerPersona: _playerPersona,
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
            areaResRef: "market_area",
            placeableTag: "stall_42",
            ownerPlayerPersona: _playerPersona,
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
            areaResRef: "market_area",
            placeableTag: "stall_99",
            ownerPlayerPersona: _playerPersona,
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
            Assert.That(captured!.AreaResRef, Is.EqualTo(command.AreaResRef));
            Assert.That(captured!.PlaceableTag, Is.EqualTo(command.PlaceableTag));
            Assert.That(captured!.OwnerPersona, Is.EqualTo(command.OwnerPersona));
            Assert.That(captured!.OwnerPlayerPersona, Is.EqualTo(command.OwnerPlayerPersona));
            Assert.That(captured!.OwnerDisplayName, Is.EqualTo(command.OwnerDisplayName));
            Assert.That(captured!.NextRentDueUtc, Is.EqualTo(command.NextRentDueUtc));
        });
        _service.Verify(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenCoOwnersProvided_PassesCollection()
    {
        ClaimPlayerStallRequest? captured = null;
        PlayerStallCoOwnerRequest coOwner = new(
            Persona: PersonaId.FromCharacter(CharacterId.New()),
            DisplayName: "Corin",
            CanManageInventory: true,
            CanConfigureSettings: false,
            CanCollectEarnings: true);

        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            stallId: 77,
            areaResRef: "market_area",
            placeableTag: "stall_77",
            ownerPlayerPersona: _playerPersona,
            ownerPersona: PersonaId.FromCharacter(CharacterId.New()),
            ownerDisplayName: "Aria",
            coOwners: new[] { coOwner });

        _service
            .Setup(s => s.ClaimAsync(It.IsAny<ClaimPlayerStallRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ClaimPlayerStallRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(PlayerStallServiceResult.Ok());

        await _handler.HandleAsync(command);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.CoOwners, Is.Not.Null);
        Assert.That(captured!.CoOwners, Has.Count.EqualTo(1));
        PlayerStallCoOwnerRequest first = captured!.CoOwners!.First();
        Assert.Multiple(() =>
        {
            Assert.That(first.Persona, Is.EqualTo(coOwner.Persona));
            Assert.That(first.DisplayName, Is.EqualTo("Corin"));
            Assert.That(first.CanManageInventory, Is.True);
            Assert.That(first.CanConfigureSettings, Is.False);
            Assert.That(first.CanCollectEarnings, Is.True);
        });
    }
}
