using System;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Properties;

[TestFixture]
public class PersistentRentablePropertyRepositoryTests
{
    private IDbContextFactory<PwEngineContext> _factory = null!;
    private PersistentRentablePropertyRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _factory = new TestContextFactory(options);
        using PwEngineContext ctx = _factory.CreateDbContext();
        ctx.Database.EnsureCreated();

        _repository = new PersistentRentablePropertyRepository(_factory);
    }

    [Test]
    public async Task PersistRentalAsync_CreatesAndLoadsSnapshot()
    {
        PropertyId propertyId = PropertyId.New();
        PersonaId tenant = PersonaId.FromCharacter(CharacterId.New());
        PersonaId owner = PersonaId.FromSystem("CityOfCordor");
    PersonaId resident = PersonaId.FromCharacter(CharacterId.New());

        RentablePropertyDefinition definition = new(
            propertyId,
            "cordor_townhouse",
            new SettlementTag("cordor"),
            PropertyCategory.Residential,
            GoldAmount.Parse(500),
            AllowsCoinhouseRental: true,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: new CoinhouseTag("cordor_coinhouse"),
            PurchasePrice: GoldAmount.Parse(25000),
            MonthlyOwnershipTax: GoldAmount.Parse(125),
            EvictionGraceDays: 2)
        {
            DefaultOwner = owner
        };

        DateOnly startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        RentalAgreementSnapshot agreement = new(
            tenant,
            startDate,
            startDate.AddMonths(1),
            GoldAmount.Parse(500),
            RentalPaymentMethod.CoinhouseAccount,
            DateTimeOffset.UtcNow);

        RentablePropertySnapshot snapshot = new(
            definition,
            PropertyOccupancyStatus.Rented,
            tenant,
            owner,
            new[] { resident },
            agreement);

        await _repository.PersistRentalAsync(snapshot);

        RentablePropertySnapshot? loaded = await _repository.GetSnapshotAsync(propertyId);

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded!.Definition.InternalName, Is.EqualTo(definition.InternalName));
            Assert.That(loaded.Definition.Settlement.Value, Is.EqualTo("cordor"));
            Assert.That(loaded.Definition.MonthlyRent.Value, Is.EqualTo(500));
            Assert.That(loaded.Definition.AllowsCoinhouseRental, Is.True);
            Assert.That(loaded.Definition.AllowsDirectRental, Is.True);
            Assert.That(loaded.Definition.SettlementCoinhouseTag?.Value, Is.EqualTo("cordor_coinhouse"));
            Assert.That(loaded.CurrentTenant, Is.EqualTo(tenant));
            Assert.That(loaded.CurrentOwner, Is.EqualTo(owner));
            Assert.That(loaded.OccupancyStatus, Is.EqualTo(PropertyOccupancyStatus.Rented));
            Assert.That(loaded.ActiveRental, Is.Not.Null);
            Assert.That(loaded.ActiveRental!.PaymentMethod, Is.EqualTo(RentalPaymentMethod.CoinhouseAccount));
            Assert.That(loaded.Residents, Contains.Item(resident));
        });
    }

    [Test]
    public async Task PersistRentalAsync_ClearsRentalWhenVacated()
    {
        PropertyId propertyId = PropertyId.New();
        PersonaId owner = PersonaId.FromSystem("CityOfCordor");
        PersonaId tenant = PersonaId.FromCharacter(CharacterId.New());

        RentablePropertyDefinition definition = new(
            propertyId,
            "cordor_townhouse",
            new SettlementTag("cordor"),
            PropertyCategory.Residential,
            GoldAmount.Parse(600),
            AllowsCoinhouseRental: true,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: new CoinhouseTag("cordor_coinhouse"),
            PurchasePrice: null,
            MonthlyOwnershipTax: null,
            EvictionGraceDays: 2)
        {
            DefaultOwner = owner
        };

        RentablePropertySnapshot rented = new(
            definition,
            PropertyOccupancyStatus.Rented,
            tenant,
            owner,
            Residents: new[] { owner },
            new RentalAgreementSnapshot(
                tenant,
                DateOnly.FromDateTime(DateTime.UtcNow.Date),
                DateOnly.FromDateTime(DateTime.UtcNow.Date).AddMonths(1),
                GoldAmount.Parse(600),
                RentalPaymentMethod.OutOfPocket,
                DateTimeOffset.UtcNow));

        await _repository.PersistRentalAsync(rented);

        RentablePropertySnapshot vacated = new(
            rented.Definition,
            PropertyOccupancyStatus.Vacant,
            CurrentTenant: null,
            CurrentOwner: owner,
            Residents: rented.Residents,
            ActiveRental: null);

        await _repository.PersistRentalAsync(vacated);

        RentablePropertySnapshot? loaded = await _repository.GetSnapshotAsync(propertyId);

        Assert.That(loaded, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded!.OccupancyStatus, Is.EqualTo(PropertyOccupancyStatus.Vacant));
            Assert.That(loaded.CurrentTenant, Is.Null);
            Assert.That(loaded.ActiveRental, Is.Null);
            Assert.That(loaded.CurrentOwner, Is.EqualTo(owner));
        });
    }

    private sealed class TestContextFactory(DbContextOptions<PwEngineContext> options)
        : IDbContextFactory<PwEngineContext>
    {
        public PwEngineContext CreateDbContext() => new(options);
    }
}
