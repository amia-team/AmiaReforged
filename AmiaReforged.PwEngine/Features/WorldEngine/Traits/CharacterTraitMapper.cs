using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Bidirectional mapper between domain model (CharacterTrait) and persistence model (PersistentCharacterTrait).
/// </summary>
[ServiceBinding(typeof(CharacterTraitMapper))]
public class CharacterTraitMapper
{
    /// <summary>
    /// Converts domain model to persistence model.
    /// </summary>
    /// <param name="characterTrait">Domain character trait</param>
    /// <returns>Persistence entity</returns>
    public PersistentCharacterTrait ToPersistent(CharacterTrait characterTrait)
    {
        return new PersistentCharacterTrait
        {
            Id = characterTrait.Id,
            CharacterId = characterTrait.CharacterId,
            TraitTag = characterTrait.TraitTag,
            DateAcquired = characterTrait.DateAcquired,
            IsConfirmed = characterTrait.IsConfirmed,
            IsActive = characterTrait.IsActive,
            IsUnlocked = characterTrait.IsUnlocked,
            CustomData = characterTrait.CustomData
        };
    }

    /// <summary>
    /// Converts persistence model to domain model.
    /// </summary>
    /// <param name="persistentTrait">Persistence entity</param>
    /// <returns>Domain character trait</returns>
    public CharacterTrait ToDomain(PersistentCharacterTrait persistentTrait)
    {
        return new CharacterTrait
        {
            Id = persistentTrait.Id,
            CharacterId = persistentTrait.CharacterId,
            TraitTag = persistentTrait.TraitTag,
            DateAcquired = persistentTrait.DateAcquired,
            IsConfirmed = persistentTrait.IsConfirmed,
            IsActive = persistentTrait.IsActive,
            IsUnlocked = persistentTrait.IsUnlocked,
            CustomData = persistentTrait.CustomData
        };
    }

    public static CharacterTraitMapper Create()
    {
        return new CharacterTraitMapper();
    }
}
