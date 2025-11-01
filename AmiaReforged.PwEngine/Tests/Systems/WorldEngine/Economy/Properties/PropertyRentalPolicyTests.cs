using System;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Properties;

[TestFixture]
public class PropertyRentalPolicyTests
{
    private readonly PropertyRentalPolicy _policy = new();
    private readonly PersonaId _tenant = PersonaId.FromCharacter(CharacterId.New());

    [Test]
    public void Evaluate_ReturnsFailure_WhenPropertyNotVacant()
    {
        RentablePropertySnapshot property = CreateSnapshot(status: PropertyOccupancyStatus.Rented);
        RentPropertyRequest request = new(_tenant, property.Definition.Id, RentalPaymentMethod.OutOfPocket,
            DateOnly.FromDateTime(DateTime.UtcNow));
        PaymentCapabilitySnapshot capabilities = new(true, true);

        RentalDecision decision = _policy.Evaluate(request, property, capabilities);

        Assert.That(decision.Success, Is.False);
        Assert.That(decision.Reason, Is.EqualTo(RentalDecisionReason.PropertyUnavailable));
    }

    [Test]
    public void Evaluate_DeniesCoinhouse_WhenAccountMissing()
    {
        RentablePropertySnapshot property = CreateSnapshot(allowsCoinhouse: true, status: PropertyOccupancyStatus.Vacant);
        RentPropertyRequest request = new(_tenant, property.Definition.Id, RentalPaymentMethod.CoinhouseAccount,
            DateOnly.FromDateTime(DateTime.UtcNow));
        PaymentCapabilitySnapshot capabilities = new(false, true);

        RentalDecision decision = _policy.Evaluate(request, property, capabilities);

        Assert.That(decision.Success, Is.False);
        Assert.That(decision.Reason, Is.EqualTo(RentalDecisionReason.CoinhouseAccountRequired));
    }

    [Test]
    public void Evaluate_AllowsCoinhouse_WhenRequirementsMet()
    {
        RentablePropertySnapshot property = CreateSnapshot(allowsCoinhouse: true, status: PropertyOccupancyStatus.Vacant);
        RentPropertyRequest request = new(_tenant, property.Definition.Id, RentalPaymentMethod.CoinhouseAccount,
            DateOnly.FromDateTime(DateTime.UtcNow));
        PaymentCapabilitySnapshot capabilities = new(true, false);

        RentalDecision decision = _policy.Evaluate(request, property, capabilities);

        Assert.That(decision.Success, Is.True);
        Assert.That(decision.Reason, Is.EqualTo(RentalDecisionReason.None));
    }

    [Test]
    public void Evaluate_DeniesDirect_WhenInsufficientFunds()
    {
        RentablePropertySnapshot property = CreateSnapshot(allowsDirect: true, status: PropertyOccupancyStatus.Vacant);
        RentPropertyRequest request = new(_tenant, property.Definition.Id, RentalPaymentMethod.OutOfPocket,
            DateOnly.FromDateTime(DateTime.UtcNow));
        PaymentCapabilitySnapshot capabilities = new(true, false);

        RentalDecision decision = _policy.Evaluate(request, property, capabilities);

        Assert.That(decision.Success, Is.False);
        Assert.That(decision.Reason, Is.EqualTo(RentalDecisionReason.InsufficientDirectFunds));
    }

    [Test]
    public void IsEvictionEligible_ReturnsTrue_WhenTenantAbsentBeyondGrace()
    {
        RentablePropertySnapshot property = CreateSnapshot();
        DateOnly nextDue = DateOnly.FromDateTime(new DateTime(2025, 11, 1));
        RentalAgreementSnapshot agreement = new(
            _tenant,
            nextDue.AddMonths(-1),
            nextDue,
            GoldAmount.Parse(100),
            RentalPaymentMethod.OutOfPocket,
            LastOccupantSeenUtc: nextDue.ToDateTime(TimeOnly.MinValue).AddDays(-5));

        bool evict = _policy.IsEvictionEligible(agreement, property.Definition,
            new DateTimeOffset(new DateTime(2025, 11, 4), TimeSpan.Zero));

        Assert.That(evict, Is.True);
    }

    [Test]
    public void IsEvictionEligible_ReturnsFalse_WhenTenantSeenAfterDueDate()
    {
        RentablePropertySnapshot property = CreateSnapshot();
        DateOnly nextDue = DateOnly.FromDateTime(new DateTime(2025, 11, 1));
        RentalAgreementSnapshot agreement = new(
            _tenant,
            nextDue.AddMonths(-1),
            nextDue,
            GoldAmount.Parse(100),
            RentalPaymentMethod.OutOfPocket,
            LastOccupantSeenUtc: nextDue.ToDateTime(TimeOnly.MinValue).AddDays(1));

        bool evict = _policy.IsEvictionEligible(agreement, property.Definition,
            new DateTimeOffset(new DateTime(2025, 11, 5), TimeSpan.Zero));

        Assert.That(evict, Is.False);
    }

    private static RentablePropertySnapshot CreateSnapshot(
        PropertyOccupancyStatus status = PropertyOccupancyStatus.Vacant,
        bool allowsCoinhouse = true,
        bool allowsDirect = true)
    {
        RentablePropertyDefinition definition = new(
            PropertyId.New(),
            "cordor_townhouse",
            new SettlementTag("cordor"),
            PropertyCategory.Residential,
            GoldAmount.Parse(500),
            allowsCoinhouse,
            allowsDirect,
            SettlementCoinhouseTag: new CoinhouseTag("cordor_coinhouse"),
            PurchasePrice: null,
            MonthlyOwnershipTax: null,
            EvictionGraceDays: 2);

        return new RentablePropertySnapshot(
            definition,
            status,
            CurrentTenant: null,
            CurrentOwner: null,
            Residents: Array.Empty<PersonaId>(),
            ActiveRental: null);
    }
}
