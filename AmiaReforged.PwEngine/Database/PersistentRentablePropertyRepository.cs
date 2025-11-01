using AmiaReforged.PwEngine.Database.Entities.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IRentablePropertyRepository))]
public sealed class PersistentRentablePropertyRepository(IDbContextFactory<PwEngineContext> factory)
    : IRentablePropertyRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task<RentablePropertySnapshot?> GetSnapshotAsync(
        PropertyId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = factory.CreateDbContext();
            Guid propertyGuid = id;

            RentablePropertyRecord? entity = await ctx.RentableProperties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == propertyGuid, cancellationToken);

            return entity is null ? null : ToSnapshot(entity);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load rentable property {PropertyId}", id);
            throw;
        }
    }

    public async Task PersistRentalAsync(
        RentablePropertySnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using PwEngineContext ctx = factory.CreateDbContext();
            Guid propertyGuid = snapshot.Definition.Id;

            RentablePropertyRecord? entity = await ctx.RentableProperties
                .FirstOrDefaultAsync(p => p.Id == propertyGuid, cancellationToken);

            if (entity is null)
            {
                entity = new RentablePropertyRecord
                {
                    Id = propertyGuid,
                    InternalName = snapshot.Definition.InternalName,
                    SettlementTag = snapshot.Definition.Settlement.Value,
                };
                ctx.RentableProperties.Add(entity);
            }

            MapToEntity(snapshot, entity);
            await ctx.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist rentable property {PropertyId}", snapshot.Definition.Id);
            throw;
        }
    }

    private static RentablePropertySnapshot ToSnapshot(RentablePropertyRecord entity)
    {
        RentablePropertyDefinition definition = new(
            PropertyId.Parse(entity.Id),
            entity.InternalName,
            new SettlementTag(entity.SettlementTag),
            entity.Category,
            GoldAmount.Parse(entity.MonthlyRent),
            entity.AllowsCoinhouseRental,
            entity.AllowsDirectRental,
            entity.SettlementCoinhouseTag is null ? null : new CoinhouseTag(entity.SettlementCoinhouseTag),
            entity.PurchasePrice.HasValue ? GoldAmount.Parse(entity.PurchasePrice.Value) : null,
            entity.MonthlyOwnershipTax.HasValue ? GoldAmount.Parse(entity.MonthlyOwnershipTax.Value) : null,
            entity.EvictionGraceDays)
        {
            DefaultOwner = ParsePersona(entity.DefaultOwnerPersona)
        };

        PersonaId? currentTenant = ParsePersona(entity.CurrentTenantPersona);
        PersonaId? currentOwner = ParsePersona(entity.CurrentOwnerPersona);

        RentalAgreementSnapshot? activeRental = null;
        if (entity.RentalPaymentMethod.HasValue && entity.RentalStartDate.HasValue && entity.NextPaymentDueDate.HasValue)
        {
            PersonaId tenantPersona = currentTenant
                ?? throw new InvalidOperationException(
                    $"Property {entity.Id} has rental metadata but no tenant persona stored.");

            GoldAmount monthlyRent = entity.RentalMonthlyRent.HasValue
                ? GoldAmount.Parse(entity.RentalMonthlyRent.Value)
                : definition.MonthlyRent;

            activeRental = new RentalAgreementSnapshot(
                tenantPersona,
                entity.RentalStartDate.Value,
                entity.NextPaymentDueDate.Value,
                monthlyRent,
                entity.RentalPaymentMethod.Value,
                entity.LastOccupantSeenUtc);
        }

        return new RentablePropertySnapshot(
            definition,
            entity.OccupancyStatus,
            currentTenant,
            currentOwner,
            activeRental);
    }

    private static void MapToEntity(RentablePropertySnapshot snapshot, RentablePropertyRecord entity)
    {
        RentablePropertyDefinition definition = snapshot.Definition;

        entity.InternalName = definition.InternalName;
        entity.SettlementTag = definition.Settlement.Value;
        entity.Category = definition.Category;
        entity.MonthlyRent = definition.MonthlyRent.Value;
        entity.AllowsCoinhouseRental = definition.AllowsCoinhouseRental;
        entity.AllowsDirectRental = definition.AllowsDirectRental;
        entity.SettlementCoinhouseTag = definition.SettlementCoinhouseTag?.Value;
        entity.PurchasePrice = definition.PurchasePrice?.Value;
        entity.MonthlyOwnershipTax = definition.MonthlyOwnershipTax?.Value;
        entity.EvictionGraceDays = definition.EvictionGraceDays;
        entity.DefaultOwnerPersona = definition.DefaultOwner?.ToString();

        entity.OccupancyStatus = snapshot.OccupancyStatus;
        entity.CurrentTenantPersona = snapshot.CurrentTenant?.ToString();
        entity.CurrentOwnerPersona = snapshot.CurrentOwner?.ToString();

        if (snapshot.ActiveRental is { } rental)
        {
            entity.RentalStartDate = rental.StartDate;
            entity.NextPaymentDueDate = rental.NextPaymentDueDate;
            entity.RentalMonthlyRent = rental.MonthlyRent.Value;
            entity.RentalPaymentMethod = rental.PaymentMethod;
            entity.LastOccupantSeenUtc = rental.LastOccupantSeenUtc;
        }
        else
        {
            entity.RentalStartDate = null;
            entity.NextPaymentDueDate = null;
            entity.RentalMonthlyRent = null;
            entity.RentalPaymentMethod = null;
            entity.LastOccupantSeenUtc = null;
        }
    }

    private static PersonaId? ParsePersona(string? personaString)
    {
        if (string.IsNullOrWhiteSpace(personaString))
            return null;

        return PersonaId.Parse(personaString);
    }
}
