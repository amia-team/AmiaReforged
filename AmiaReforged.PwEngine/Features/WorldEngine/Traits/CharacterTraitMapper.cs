using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Maps between domain CharacterTrait and persistent PersistentCharacterTrait
/// </summary>
[ServiceBinding(typeof(CharacterTraitMapper))]
public class CharacterTraitMapper
{
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
}
