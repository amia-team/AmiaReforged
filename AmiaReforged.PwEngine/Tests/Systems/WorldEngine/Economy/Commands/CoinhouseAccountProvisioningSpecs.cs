using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Commands;

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

    private CoinHouse _coinhouse = null!;
    private CoinhouseTag _coinhouseTag;

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

        _coinhouse = new CoinHouse
        {
            Id = 42,
            Tag = _coinhouseTag.Value,
            Settlement = 7,
            EngineId = Guid.NewGuid(),
            StoredGold = 0,
            Accounts = new List<CoinHouseAccount>()
        };

        _coinhouses
            .Setup(r => r.GetByTag(_coinhouseTag))
            .Returns(_coinhouse);

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
            .Setup(r => r.GetAccountFor(It.IsAny<Guid>()))
            .Returns((CoinHouseAccount?)null);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True, "expected deposit to succeed for lazy provisioning");
        Assert.That(_coinhouse.Accounts, Is.Not.Null);
        Assert.That(_coinhouse.Accounts, Has.Count.EqualTo(1));

        Guid expectedAccountId = PersonaAccountId.From(persona);
        Guid actualAccountId = _coinhouse.Accounts![0].Id;
        Assert.That(actualAccountId, Is.EqualTo(expectedAccountId));
    }
}
