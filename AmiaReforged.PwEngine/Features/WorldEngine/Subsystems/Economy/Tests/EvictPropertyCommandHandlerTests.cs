using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests;

/// <summary>
/// Behavior tests for property eviction operations.
/// </summary>
[TestFixture]
public class EvictPropertyCommandHandlerTests
{
    private Mock<IPersistentObjectRepository> _mockObjectRepository = null!;
    private Mock<IRegionRepository> _mockRegionRepository = null!;
    private RegionIndex _regionIndex = null!;
    private Mock<IRentablePropertyRepository> _mockPropertyRepository = null!;
    private Mock<IForeclosureStorageService> _mockForeclosureStorage = null!;
    private EvictPropertyCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockObjectRepository = new Mock<IPersistentObjectRepository>();
        _mockRegionRepository = new Mock<IRegionRepository>();
        _regionIndex = new RegionIndex(_mockRegionRepository.Object);
        _mockPropertyRepository = new Mock<IRentablePropertyRepository>();
        _mockForeclosureStorage = new Mock<IForeclosureStorageService>();
        _handler = new EvictPropertyCommandHandler(
            _mockObjectRepository.Object,
            _regionIndex,
            _mockPropertyRepository.Object,
            _mockForeclosureStorage.Object);
    }

    [Test]
    public async Task EvictProperty_NoTenant_Fails()
    {
        // Given a vacant property with no tenant
        PropertyId propertyId = PropertyId.New();

        RentablePropertyDefinition propertyDefinition = new RentablePropertyDefinition(
            Id: propertyId,
            InternalName: "test_house",
            Settlement: new SettlementTag("TestTown"),
            Category: PropertyCategory.Residential,
            MonthlyRent: GoldAmount.Parse(100),
            AllowsCoinhouseRental: false,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: EconomyTestHelpers.CreateCoinhouseTag("TestBank"),
            PurchasePrice: null,
            MonthlyOwnershipTax: null
        );

        RentablePropertySnapshot property = new RentablePropertySnapshot(
            Definition: propertyDefinition,
            OccupancyStatus: PropertyOccupancyStatus.Vacant,
            CurrentTenant: null,
            CurrentOwner: null,
            Residents: Array.Empty<PersonaId>(),
            ActiveRental: null
        );

        EvictPropertyCommand command = new EvictPropertyCommand(Property: property);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when property has no tenant");
        Assert.That(result.ErrorMessage, Does.Contain("no current tenant").IgnoreCase);
    }

    [Test]
    public async Task EvictProperty_WithTenant_ClearsPropertyState()
    {
        // Given a rented property with tenant
        PropertyId propertyId = PropertyId.New();

        // Use a government persona (not character) so ResolveCharacterId returns null = no notification = no NwTask call
        GovernmentPersona tenant = PersonaTestHelpers.CreateGovernmentPersona();

        RentablePropertyDefinition propertyDefinition = new RentablePropertyDefinition(
            Id: propertyId,
            InternalName: "test_house",
            Settlement: new SettlementTag("TestTown"),
            Category: PropertyCategory.Residential,
            MonthlyRent: GoldAmount.Parse(100),
            AllowsCoinhouseRental: false,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: null, // No coinhouse means no foreclosure
            PurchasePrice: null,
            MonthlyOwnershipTax: null
        );

        RentalAgreementSnapshot rental = new RentalAgreementSnapshot(
            Tenant: tenant.Id,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NextPaymentDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            MonthlyRent: GoldAmount.Parse(100),
            PaymentMethod: RentalPaymentMethod.OutOfPocket,
            LastOccupantSeenUtc: null
        );

        RentablePropertySnapshot property = new RentablePropertySnapshot(
            Definition: propertyDefinition,
            OccupancyStatus: PropertyOccupancyStatus.Rented,
            CurrentTenant: tenant.Id,
            CurrentOwner: null,
            Residents: new[] { tenant.Id },
            ActiveRental: rental
        );

        // Mock empty region index and object repository
        _mockRegionRepository.Setup(r => r.All()).Returns(new List<RegionDefinition>());
        _mockObjectRepository.Setup(r => r.GetObjectsForArea(It.IsAny<string>()))
            .Returns(new List<PersistentObject>());

        EvictPropertyCommand command = new EvictPropertyCommand(Property: property);

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should succeed (no coinhouse = no foreclosure = simple eviction)
        Assert.That(result.Success, Is.True, $"Should succeed for valid eviction. Error: {result.ErrorMessage}");

        // And property should be persisted as vacant
        _mockPropertyRepository.Verify(
            r => r.PersistRentalAsync(
                It.Is<RentablePropertySnapshot>(p =>
                    p.Definition.Id == propertyId &&
                    p.CurrentTenant == null &&
                    p.ActiveRental == null &&
                    p.OccupancyStatus == PropertyOccupancyStatus.Vacant),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
