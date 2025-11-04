namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

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
    PriceOutOfRange
}
