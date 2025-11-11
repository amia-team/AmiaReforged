using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy;

/// <summary>
/// Behavior tests for rent payment operations.
/// </summary>
[TestFixture]
public class PayRentCommandHandlerTests
{
    private Mock<IRentablePropertyRepository> _mockPropertyRepository = null!;
    private PropertyRentalPolicy _policy = null!;
    private PayRentCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPropertyRepository = new Mock<IRentablePropertyRepository>();
        _policy = new PropertyRentalPolicy();
        _handler = new PayRentCommandHandler(
            _mockPropertyRepository.Object,
            _policy);
    }

    [Test]
    public async Task PayRent_NoActiveRental_Fails()
    {
        // Given a property with no active rental
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

        var command = new PayRentCommand(
            Property: property,
            Tenant: tenant.Id,
            PaymentMethod: RentalPaymentMethod.OutOfPocket
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when no active rental");
        Assert.That(result.ErrorMessage, Does.Contain("no active rental").IgnoreCase);
    }

    [Test]
    public async Task PayRent_WrongTenant_Fails()
    {
        // Given a property rented by someone else
        CharacterPersona actualTenant = PersonaTestHelpers.CreateCharacterPersona("ActualTenant");
        CharacterPersona wrongTenant = PersonaTestHelpers.CreateCharacterPersona("WrongTenant");
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

        var rental = new RentalAgreementSnapshot(
            Tenant: actualTenant.Id,
            StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
            NextPaymentDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            MonthlyRent: GoldAmount.Parse(100),
            PaymentMethod: RentalPaymentMethod.OutOfPocket,
            LastOccupantSeenUtc: null
        );

        var property = new RentablePropertySnapshot(
            Definition: propertyDefinition,
            OccupancyStatus: PropertyOccupancyStatus.Rented,
            CurrentTenant: actualTenant.Id,
            CurrentOwner: null,
            Residents: new[] { actualTenant.Id },
            ActiveRental: rental
        );

        var command = new PayRentCommand(
            Property: property,
            Tenant: wrongTenant.Id,
            PaymentMethod: RentalPaymentMethod.OutOfPocket
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should fail
        Assert.That(result.Success, Is.False, "Should fail when tenant doesn't match");
        Assert.That(result.ErrorMessage, Does.Contain("tenant").IgnoreCase);
    }

    [Test]
    public async Task PayRent_SuccessfulPayment_AdvancesDueDate()
    {
        // Given a valid rental with upcoming payment
        CharacterPersona tenant = PersonaTestHelpers.CreateCharacterPersona("Tenant");
        PropertyId propertyId = PropertyId.New();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly nextDueDate = today.AddMonths(1);

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

        var rental = new RentalAgreementSnapshot(
            Tenant: tenant.Id,
            StartDate: today,
            NextPaymentDueDate: nextDueDate,
            MonthlyRent: GoldAmount.Parse(100),
            PaymentMethod: RentalPaymentMethod.OutOfPocket,
            LastOccupantSeenUtc: null
        );

        var property = new RentablePropertySnapshot(
            Definition: propertyDefinition,
            OccupancyStatus: PropertyOccupancyStatus.Rented,
            CurrentTenant: tenant.Id,
            CurrentOwner: null,
            Residents: new[] { tenant.Id },
            ActiveRental: rental
        );

        var command = new PayRentCommand(
            Property: property,
            Tenant: tenant.Id,
            PaymentMethod: RentalPaymentMethod.OutOfPocket
        );

        // When the command is executed
        CommandResult result = await _handler.HandleAsync(command);

        // Then it should succeed
        Assert.That(result.Success, Is.True, "Should succeed for valid payment");
        
        // And the property should be persisted
        _mockPropertyRepository.Verify(
            r => r.PersistRentalAsync(
                It.Is<RentablePropertySnapshot>(p => 
                    p.Definition.Id == propertyId && 
                    p.ActiveRental != null &&
                    p.ActiveRental.NextPaymentDueDate > nextDueDate),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
