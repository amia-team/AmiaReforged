using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy;

/// <summary>
/// Behavior tests for property rental operations.
/// </summary>
[TestFixture]
public class RentPropertyCommandHandlerTests
{
    private Mock<IRentablePropertyRepository> _mockPropertyRepository = null!;
    private Mock<IRentalPaymentCapabilityService> _mockCapabilityService = null!;
    private PropertyRentalPolicy _policy = null!;
    private RentPropertyCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPropertyRepository = new Mock<IRentablePropertyRepository>();
        _mockCapabilityService = new Mock<IRentalPaymentCapabilityService>();
        _policy = new PropertyRentalPolicy();
        _handler = new RentPropertyCommandHandler(
            _mockPropertyRepository.Object,
            _mockCapabilityService.Object,
            _policy);
    }

    [Test]
    public async Task RentProperty_PropertyNotFound_Fails()
    {
        // Given a non-existent property
        CharacterPersona tenant = PersonaTestHelpers.CreateCharacterPersona("Tenant");
        PropertyId propertyId = PropertyId.New();

        _mockPropertyRepository
            .Setup(r => r.GetSnapshotAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RentablePropertySnapshot?)null);

        var command = new RentPropertyCommand(
            Tenant: tenant.Id,
            PropertyId: propertyId,
            PaymentMethod: RentalPaymentMethod.OutOfPocket,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow)
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when property doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("could not be found").IgnoreCase);
    }

    [Test]
    public async Task RentProperty_PropertyAlreadyRented_Fails()
    {
        // Given a property that's already rented
        CharacterPersona tenant = PersonaTestHelpers.CreateCharacterPersona("Tenant");
        CharacterPersona existingTenant = PersonaTestHelpers.CreateCharacterPersona("ExistingTenant");
        PropertyId propertyId = PropertyId.New();

        var propertyDefinition = new RentablePropertyDefinition(
            Id: propertyId,
            InternalName: "test_house",
            Settlement: new SettlementTag("TestTown"),
            Category: PropertyCategory.Residential,
            MonthlyRent: GoldAmount.Parse(100),
            AllowsCoinhouseRental: true,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: null,
            PurchasePrice: null,
            MonthlyOwnershipTax: null
        );

        var property = new RentablePropertySnapshot(
            Definition: propertyDefinition,
            OccupancyStatus: PropertyOccupancyStatus.Rented,
            CurrentTenant: existingTenant.Id,
            CurrentOwner: null,
            Residents: Array.Empty<PersonaId>(),
            ActiveRental: new RentalAgreementSnapshot(
                existingTenant.Id,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                GoldAmount.Parse(100),
                RentalPaymentMethod.OutOfPocket,
                null)
        );

        _mockPropertyRepository
            .Setup(r => r.GetSnapshotAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        var command = new RentPropertyCommand(
            Tenant: tenant.Id,
            PropertyId: propertyId,
            PaymentMethod: RentalPaymentMethod.OutOfPocket,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow)
        );

        // When the command is executed  
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when property is already rented");
        Assert.That(result.ErrorMessage, Does.Contain("not currently available").IgnoreCase);
    }

    [Test]
    public async Task RentProperty_Success_PropertyBecomesRented()
    {
        // Given a vacant property and a tenant with sufficient funds
        CharacterPersona tenant = PersonaTestHelpers.CreateCharacterPersona("Tenant");
        PropertyId propertyId = PropertyId.New();

        var propertyDefinition = new RentablePropertyDefinition(
            Id: propertyId,
            InternalName: "test_house",
            Settlement: new SettlementTag("TestTown"),
            Category: PropertyCategory.Residential,
            MonthlyRent: GoldAmount.Parse(100),
            AllowsCoinhouseRental: false,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: null,
            PurchasePrice: null,
            MonthlyOwnershipTax: null
        );

        var property = new RentablePropertySnapshot(
            Definition: propertyDefinition,
            OccupancyStatus: PropertyOccupancyStatus.Vacant,
            CurrentTenant: null,
            CurrentOwner: null,
            Residents: Array.Empty<PersonaId>(),
            ActiveRental: null
        );

        var capabilities = new PaymentCapabilitySnapshot(
            HasSettlementCoinhouseAccount: false,
            HasSufficientDirectFunds: true
        );

        _mockPropertyRepository
            .Setup(r => r.GetSnapshotAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _mockCapabilityService
            .Setup(s => s.EvaluateAsync(It.IsAny<RentPropertyRequest>(), It.IsAny<RentablePropertySnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(capabilities);

        RentablePropertySnapshot? capturedProperty = null;
        _mockPropertyRepository
            .Setup(r => r.PersistRentalAsync(It.IsAny<RentablePropertySnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<RentablePropertySnapshot, CancellationToken>((prop, _) => capturedProperty = prop)
            .Returns(Task.CompletedTask);

        var command = new RentPropertyCommand(
            Tenant: tenant.Id,
            PropertyId: propertyId,
            PaymentMethod: RentalPaymentMethod.OutOfPocket,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow)
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then the rental should succeed
        Assert.That(result.Success, Is.True, "Rental should succeed with sufficient funds");
        Assert.That(capturedProperty, Is.Not.Null, "Property should be persisted");
        Assert.That(capturedProperty!.OccupancyStatus, Is.EqualTo(PropertyOccupancyStatus.Rented));
        Assert.That(capturedProperty.CurrentTenant, Is.EqualTo(tenant.Id));
        Assert.That(capturedProperty.ActiveRental, Is.Not.Null);
    }
}
