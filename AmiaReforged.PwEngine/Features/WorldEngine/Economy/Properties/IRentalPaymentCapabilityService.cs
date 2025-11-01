namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Resolves whether the renter can satisfy the requested payment option.
/// </summary>
public interface IRentalPaymentCapabilityService
{
    Task<PaymentCapabilitySnapshot> EvaluateAsync(RentPropertyRequest request, RentablePropertySnapshot property,
        CancellationToken cancellationToken = default);
}
