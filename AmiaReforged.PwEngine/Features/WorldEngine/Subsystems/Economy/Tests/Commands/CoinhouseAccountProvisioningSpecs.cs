using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Commands;

/// <summary>
/// RSpec-style specs for lazy CoinHouse account provisioning.
/// </summary>
[TestFixture]
public class CoinhouseAccountProvisioningSpecs
{
    private Mock<ICoinhouseRepository> _coinhouses = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private Mock<IEventBus> _eventBus = null!;
    private DepositGoldCommandHandler _handler = null!;

    private CoinhouseDto _coinhouse = null!;
    private CoinhouseTag _coinhouseTag;
    private CoinhouseAccountDto? _savedAccount;

    [SetUp]
    public void Setup()
    {
        _coinhouses = new Mock<ICoinhouseRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _eventBus = new Mock<IEventBus>();

        _handler = new DepositGoldCommandHandler(
            _coinhouses.Object,
            _transactions.Object,
            _eventBus.Object);

        _coinhouseTag = EconomyTestHelpers.CreateCoinhouseTag("rspec-bank");

        _coinhouse = new CoinhouseDto
        {
            Id = 42,
            Tag = _coinhouseTag,
            Settlement = 7,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(_coinhouseTag)
        };

        _coinhouses
            .Setup(r => r.GetByTagAsync(It.IsAny<CoinhouseTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseTag tag, CancellationToken _) =>
                tag.Value == _coinhouseTag.Value ? _coinhouse : null);

        _coinhouses
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((dto, _) => _savedAccount = dto)
            .Returns(Task.CompletedTask);

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 555;
                return t;
            });
    }

    [Test]
    public async Task it_provisions_coinhouse_account_with_stable_identity_for_system_persona()
    {
        PersonaId persona = PersonaId.FromSystem("sim-engine");
        DepositGoldCommand command = DepositGoldCommand.Create(persona, _coinhouseTag, 25, "Initial float");

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True, "expected deposit to succeed for lazy provisioning");
    Guid expectedAccountId = PersonaAccountId.ForCoinhouse(persona, _coinhouseTag);
        Assert.That(_savedAccount, Is.Not.Null, "expected newly provisioned account to be persisted");
        Assert.That(_savedAccount!.Id, Is.EqualTo(expectedAccountId));
        Assert.That(_savedAccount.Debit, Is.EqualTo(25));
        Assert.That(_savedAccount.CoinHouseId, Is.EqualTo(_coinhouse.Id));
        Assert.That(_savedAccount.Coinhouse, Is.Not.Null);
        Assert.That(_savedAccount.Coinhouse!.Id, Is.EqualTo(_coinhouse.Id));

        _coinhouses.Verify(r => r.SaveAccountAsync(
                It.Is<CoinhouseAccountDto>(a => a.Id == expectedAccountId && a.Debit == 25),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
