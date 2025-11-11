using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Determines whether a renter can satisfy the payment method requested for a property.
/// </summary>
[ServiceBinding(typeof(IRentalPaymentCapabilityService))]
public sealed class RentalPaymentCapabilityService : IRentalPaymentCapabilityService
{
    private readonly ICoinhouseRepository _coinhouses;

    public RentalPaymentCapabilityService(ICoinhouseRepository coinhouses)
    {
        _coinhouses = coinhouses ?? throw new ArgumentNullException(nameof(coinhouses));
    }

    public async Task<PaymentCapabilitySnapshot> EvaluateAsync(
        RentPropertyRequest request,
        RentablePropertySnapshot property,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(property);
        cancellationToken.ThrowIfCancellationRequested();

        bool hasCoinhouseAccount = await HasCoinhouseAccountAsync(request.Tenant, property, cancellationToken);
        bool hasDirectFunds = await HasSufficientDirectFundsAsync(request.Tenant, property, cancellationToken);

        return new PaymentCapabilitySnapshot(hasCoinhouseAccount, hasDirectFunds);
    }

    private async Task<bool> HasCoinhouseAccountAsync(
        PersonaId tenant,
        RentablePropertySnapshot property,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!property.Definition.AllowsCoinhouseRental)
        {
            return false;
        }

        CoinhouseTag? coinhouseTag = property.Definition.SettlementCoinhouseTag;
        if (coinhouseTag is null)
        {
            return false;
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(tenant, coinhouseTag.Value);
        return await _coinhouses.GetAccountForAsync(accountId, cancellationToken) is not null;
    }

    private static async Task<bool> HasSufficientDirectFundsAsync(
        PersonaId tenant,
        RentablePropertySnapshot property,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!property.Definition.AllowsDirectRental)
        {
            return false;
        }

        GoldAmount monthlyRent = property.Definition.MonthlyRent;
        int required = Math.Max(monthlyRent.Value, 0);
        if (required == 0)
        {
            return true;
        }

        if (tenant.Type != PersonaType.Character)
        {
            return false;
        }

        if (!Guid.TryParse(tenant.Value, out Guid characterGuid) || characterGuid == Guid.Empty)
        {
            return false;
        }

        NwModule? module = NwModule.Instance;
        if (module is null)
        {
            return false;
        }

        try
        {
            await NwTask.SwitchToMainThread();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (NwPlayer player in module.Players)
        {
            cancellationToken.ThrowIfCancellationRequested();

            NwCreature? creature = player.LoginCreature;
            if (creature is null || !creature.IsValid)
            {
                continue;
            }

            if (creature.UUID != characterGuid)
            {
                continue;
            }

            uint available = creature.Gold;
            uint requiredGold = (uint)required;
            return available >= requiredGold;
        }

        return false;
    }
}
