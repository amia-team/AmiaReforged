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
public class ListStallProductCommandHandlerTests
{
    private Mock<IPlayerStallService> _service = null!;
    private ListStallProductCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IPlayerStallService>();
        _handler = new ListStallProductCommandHandler(_service.Object);
    }

    [Test]
    public async Task HandleAsync_WhenServiceSucceeds_ReturnsSuccess()
    {
        byte[] itemData = { 0x01, 0x02 };
        ListStallProductCommand command = ListStallProductCommand.Create(
            stallId: 25,
            resRef: "resref",
            name: "Fine Blade",
            itemData: itemData,
            price: 5000,
            quantity: 1,
            consignorPersona: PersonaId.FromCharacter(CharacterId.New()));

        IReadOnlyDictionary<string, object> payload = new Dictionary<string, object>
        {
            ["stallId"] = 25L,
            ["productId"] = 123L
        };

        _service
            .Setup(s => s.ListProductAsync(It.IsAny<ListStallProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerStallServiceResult.Ok(payload));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["productId"], Is.EqualTo(123L));
        _service.Verify(s => s.ListProductAsync(It.IsAny<ListStallProductRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleAsync_WhenServiceFails_ReturnsFailure()
    {
        ListStallProductCommand command = ListStallProductCommand.Create(
            stallId: 10,
            resRef: "resref",
            name: "Blade",
            itemData: new byte[] { 0x01 },
            price: 1000,
            quantity: 1);

        _service
            .Setup(s => s.ListProductAsync(It.IsAny<ListStallProductRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PlayerStallServiceResult.Fail(PlayerStallError.StallInactive, "inactive"));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("inactive"));
    }

    [Test]
    public async Task HandleAsync_PassesCommandDataToService()
    {
        ListStallProductRequest? captured = null;
        byte[] itemData = { 0x0A, 0x0B };
        ListStallProductCommand command = ListStallProductCommand.Create(
            stallId: 33,
            resRef: "item_resref",
            name: "Item",
            itemData: itemData,
            price: 2000,
            quantity: 2,
            consignorDisplayName: "Consignor",
            notes: "note",
            sortOrder: 4,
            isActive: false);

        _service
            .Setup(s => s.ListProductAsync(It.IsAny<ListStallProductRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ListStallProductRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(PlayerStallServiceResult.Ok());

        await _handler.HandleAsync(command);

        Assert.That(captured, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(captured!.StallId, Is.EqualTo(command.StallId));
            Assert.That(captured!.ResRef, Is.EqualTo(command.ResRef));
            Assert.That(captured!.Name, Is.EqualTo(command.Name));
            Assert.That(captured!.Price, Is.EqualTo(command.Price));
            Assert.That(captured!.Quantity, Is.EqualTo(command.Quantity));
            Assert.That(captured!.ConsignorDisplayName, Is.EqualTo(command.ConsignorDisplayName));
            Assert.That(captured!.Notes, Is.EqualTo(command.Notes));
            Assert.That(captured!.ItemData, Is.Not.SameAs(itemData));
        });
    }
}
