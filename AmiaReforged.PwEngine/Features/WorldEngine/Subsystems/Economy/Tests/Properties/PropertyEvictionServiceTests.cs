using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Properties;

[TestFixture]
public class PropertyEvictionServiceTests
{
    private Mock<IRentablePropertyRepository> _repository = null!;
    private Mock<ICommandHandler<EvictPropertyCommand>> _evictCommandHandler = null!;
    private PropertyRentalPolicy _policy = null!;
    private List<RentablePropertySnapshot> _allProperties = null!;
    private DateTimeOffset _currentTime;
    private PropertyEvictionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _allProperties = new List<RentablePropertySnapshot>();
        _currentTime = new DateTimeOffset(new DateTime(2025, 11, 10, 12, 0, 0), TimeSpan.Zero);

        _repository = new Mock<IRentablePropertyRepository>(MockBehavior.Strict);
        _repository
            .Setup(r => r.GetAllPropertiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new List<RentablePropertySnapshot>(_allProperties));

        _evictCommandHandler = new Mock<ICommandHandler<EvictPropertyCommand>>(MockBehavior.Strict);
        _evictCommandHandler
            .Setup(h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        _policy = new PropertyRentalPolicy(_repository.Object);

        _service = new PropertyEvictionService(
            _repository.Object,
            _evictCommandHandler.Object,
            _policy,
            () => _currentTime);
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenNoProperties_DoesNotInvokeCommandHandler()
    {
        _allProperties.Clear();

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenPropertyVacant_DoesNotEvict()
    {
        RentablePropertySnapshot vacantProperty = CreateProperty(
            status: PropertyOccupancyStatus.Vacant,
            activeRental: null);

        _allProperties.Add(vacantProperty);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenPropertyOwned_DoesNotEvict()
    {
        RentablePropertySnapshot ownedProperty = CreateProperty(
            status: PropertyOccupancyStatus.Owned,
            activeRental: null);

        _allProperties.Add(ownedProperty);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenRentedWithNoActiveRental_DoesNotEvict()
    {
        // Edge case: property marked as rented but no rental agreement exists
        RentablePropertySnapshot inconsistentProperty = CreateProperty(
            status: PropertyOccupancyStatus.Rented,
            activeRental: null);

        _allProperties.Add(inconsistentProperty);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenTenantSeenAfterRentDue_DoesNotEvict()
    {
        DateOnly rentDue = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-5));
        DateTime tenantSeenAfterDue = _currentTime.DateTime.AddDays(-2);

        RentablePropertySnapshot property = CreateProperty(
            status: PropertyOccupancyStatus.Rented,
            activeRental: new RentalAgreementSnapshot(
                PersonaId.FromCharacter(CharacterId.New()),
                rentDue.AddMonths(-1),
                rentDue,
                GoldAmount.Parse(500),
                RentalPaymentMethod.OutOfPocket,
                tenantSeenAfterDue));

        _allProperties.Add(property);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenRentOverdueButWithinGracePeriod_DoesNotEvict()
    {
        // Rent due 1 day ago, grace period is 2 days, tenant not seen recently
        DateOnly rentDue = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-1));
        DateTime tenantLastSeen = rentDue.ToDateTime(TimeOnly.MinValue).AddDays(-3);

        RentablePropertySnapshot property = CreateProperty(
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 rentDue.AddMonths(-1),
                 rentDue,
                 GoldAmount.Parse(500),
                 RentalPaymentMethod.OutOfPocket,
                 tenantLastSeen));

        _allProperties.Add(property);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenGracePeriodExpiredAndTenantAbsent_EvictsProperty()
    {
        // Rent due 5 days ago, grace period is 2 days, tenant last seen before rent was due
        DateOnly rentDue = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-5));
        DateTime tenantLastSeen = rentDue.ToDateTime(TimeOnly.MinValue).AddDays(-3);

        RentablePropertySnapshot property = CreateProperty(
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 rentDue.AddMonths(-1),
                 rentDue,
                 GoldAmount.Parse(500),
                 RentalPaymentMethod.OutOfPocket,
                 tenantLastSeen));

        _allProperties.Add(property);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(
                It.Is<EvictPropertyCommand>(cmd => cmd.Property == property),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenMultiplePropertiesEligible_EvictsAll()
    {
        DateOnly rentDue = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-10));
        DateTime tenantLastSeen = rentDue.ToDateTime(TimeOnly.MinValue).AddDays(-1);

        RentablePropertySnapshot property1 = CreateProperty(
            propertyId: "property_1",
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 rentDue.AddMonths(-1),
                 rentDue,
                 GoldAmount.Parse(500),
                 RentalPaymentMethod.OutOfPocket,
                 tenantLastSeen));

        RentablePropertySnapshot property2 = CreateProperty(
            propertyId: "property_2",
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 rentDue.AddMonths(-1),
                 rentDue,
                 GoldAmount.Parse(750),
                 RentalPaymentMethod.CoinhouseAccount,
                 tenantLastSeen));

        _allProperties.Add(property1);
        _allProperties.Add(property2);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _evictCommandHandler.Verify(
            h => h.HandleAsync(
                It.Is<EvictPropertyCommand>(cmd => cmd.Property == property1),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(
                It.Is<EvictPropertyCommand>(cmd => cmd.Property == property2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenSomeEligibleSomeNot_OnlyEvictsEligible()
    {
        DateOnly overdueRent = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-10));
        DateOnly currentRent = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(5));
        DateTime tenantLastSeen = overdueRent.ToDateTime(TimeOnly.MinValue).AddDays(-1);
        DateTime tenantRecentlySeen = _currentTime.DateTime.AddDays(-1);

        RentablePropertySnapshot evictableProperty = CreateProperty(
            propertyId: "evict_me",
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 overdueRent.AddMonths(-1),
                 overdueRent,
                 GoldAmount.Parse(500),
                 RentalPaymentMethod.OutOfPocket,
                 tenantLastSeen));

        RentablePropertySnapshot currentProperty = CreateProperty(
            propertyId: "keep_me",
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 currentRent.AddMonths(-1),
                 currentRent,
                 GoldAmount.Parse(500),
                 RentalPaymentMethod.OutOfPocket,
                 tenantRecentlySeen));

        RentablePropertySnapshot vacantProperty = CreateProperty(
            propertyId: "vacant",
            status: PropertyOccupancyStatus.Vacant,
            activeRental: null);

        _allProperties.Add(evictableProperty);
        _allProperties.Add(currentProperty);
        _allProperties.Add(vacantProperty);

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(
                It.Is<EvictPropertyCommand>(cmd => cmd.Property == evictableProperty),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(
                It.Is<EvictPropertyCommand>(cmd => cmd.Property == currentProperty),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _evictCommandHandler.Verify(
            h => h.HandleAsync(
                It.Is<EvictPropertyCommand>(cmd => cmd.Property == vacantProperty),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenCommandHandlerFails_ContinuesWithOtherProperties()
    {
        DateOnly rentDue = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-10));
        DateTime tenantLastSeen = rentDue.ToDateTime(TimeOnly.MinValue).AddDays(-1);

        RentablePropertySnapshot property1 = CreateProperty(
            propertyId: "fail_eviction",
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 rentDue.AddMonths(-1),
                 rentDue,
                 GoldAmount.Parse(500),
                 RentalPaymentMethod.OutOfPocket,
                 tenantLastSeen));

        RentablePropertySnapshot property2 = CreateProperty(
            propertyId: "succeed_eviction",
            status: PropertyOccupancyStatus.Rented,
            evictionGraceDays: 2,
            activeRental: new RentalAgreementSnapshot(
                 PersonaId.FromCharacter(CharacterId.New()),
                 rentDue.AddMonths(-1),
                 rentDue,
                 GoldAmount.Parse(750),
                 RentalPaymentMethod.CoinhouseAccount,
                 tenantLastSeen));

        _allProperties.Add(property1);
        _allProperties.Add(property2);

        // Configure first call to fail, second to succeed
        _evictCommandHandler
            .SetupSequence(h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Fail("Simulated database error"))
            .ReturnsAsync(CommandResult.Ok());

        await _service.ExecuteEvictionCycleAsync(CancellationToken.None);

        // Verify both properties were attempted
        _evictCommandHandler.Verify(
            h => h.HandleAsync(It.IsAny<EvictPropertyCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task ExecuteEvictionCycleAsync_WhenCancellationRequested_StopsProcessing()
    {
        DateOnly rentDue = DateOnly.FromDateTime(_currentTime.DateTime.AddDays(-10));
        DateTime tenantLastSeen = rentDue.ToDateTime(TimeOnly.MinValue).AddDays(-1);

        // Add many properties
        for (int i = 0; i < 100; i++)
        {
            _allProperties.Add(CreateProperty(
                propertyId: $"property_{i}",
                status: PropertyOccupancyStatus.Rented,
                evictionGraceDays: 2,
                activeRental: new RentalAgreementSnapshot(
                     PersonaId.FromCharacter(CharacterId.New()),
                     rentDue.AddMonths(-1),
                     rentDue,
                     GoldAmount.Parse(500),
                     RentalPaymentMethod.OutOfPocket,
                     tenantLastSeen)));
        }

        CancellationTokenSource cts = new();
        cts.Cancel(); // Cancel immediately

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _service.ExecuteEvictionCycleAsync(cts.Token));
    }

    private static RentablePropertySnapshot CreateProperty(
        string propertyId = "test_property",
        PropertyOccupancyStatus status = PropertyOccupancyStatus.Vacant,
        int evictionGraceDays = 2,
        RentalAgreementSnapshot? activeRental = null)
    {
        RentablePropertyDefinition definition = new(
            PropertyId.New(),
            propertyId,
            new SettlementTag("cordor"),
            PropertyCategory.Residential,
            GoldAmount.Parse(500),
            AllowsCoinhouseRental: true,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: new CoinhouseTag("cordor_coinhouse"),
            PurchasePrice: null,
            MonthlyOwnershipTax: null,
            EvictionGraceDays: evictionGraceDays);

        PersonaId? currentTenant = activeRental?.Tenant;
        List<PersonaId> residents = currentTenant.HasValue
            ? new List<PersonaId> { currentTenant.Value }
            : new List<PersonaId>();

        return new RentablePropertySnapshot(
            definition,
            status,
            CurrentTenant: currentTenant,
            CurrentOwner: null,
            Residents: residents,
            ActiveRental: activeRental);
    }
}
