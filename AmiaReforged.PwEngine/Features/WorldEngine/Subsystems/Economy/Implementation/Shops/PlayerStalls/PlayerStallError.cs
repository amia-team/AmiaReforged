namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Error codes produced when operating on player stalls.
/// </summary>
public enum PlayerStallError
{
    None = 0,
    StallNotFound,
    PersonaNotGuidBacked,
    AlreadyOwned,
    NotOwned,
    NotOwner,
    StallInactive,
    PersistenceFailure,
    DescriptorMismatch,
    OwnershipRuleViolation,
    PlaceableMismatch,
    Unauthorized,
    ProductNotFound,
    PriceOutOfRange,
    CoinhouseUnavailable,
    CoinhouseAccountMissing,
    InvalidWithdrawalAmount,
    InsufficientEscrow,
    InvalidDepositAmount,
    DepositAmountTooLarge
}
