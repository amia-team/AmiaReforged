using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters;

[ServiceBinding(typeof(CharacterRegistrationService))]
public class CharacterRegistrationService
{
    private readonly IPersistentCharacterRepository _characterRepository;
    private readonly RuntimeCharacterService _runtimeCharacterService;

    public CharacterRegistrationService(IPersistentCharacterRepository characterRepository,
        RuntimeCharacterService runtimeCharacterService)
    {
        _characterRepository = characterRepository;
        _runtimeCharacterService = runtimeCharacterService;
        NwArea travelAgency = NwModule.Instance.Areas.First(a => a.ResRef == "core_travelroom");

        travelAgency.OnEnter += RegisterNewCharacter;
    }

    private void RegisterNewCharacter(AreaEvents.OnEnter obj)
    {
        NwGameObject creature = obj.EnteringObject;
        if (!creature.IsLoginPlayerCharacter(out NwPlayer? player)) return;
        if (player.LoginCreature is null) return;
        if (player.IsDM) return;

        Guid pcKey = _runtimeCharacterService.GetPlayerKey(player);

        if (pcKey == Guid.Empty)
        {
            player.SendServerMessage("You need a PC Key to play.");
            player.LoginCreature.Location = NwModule.Instance.StartingLocation;
            return;
        }

        PersistedCharacter? character = _characterRepository.GetByGuid(pcKey);
        string personaIdString = CharacterId.From(pcKey).ToPersonaId().ToString();

        if (character is not null)
        {
            if (string.IsNullOrWhiteSpace(character.PersonaIdString))
            {
                _characterRepository.UpdatePersonaId(character.Id, personaIdString);
            }

            return;
        }

        PersistedCharacter newCharacter = new()
        {
            Id = pcKey,
            FirstName = player.LoginCreature.OriginalFirstName,
            LastName = player.LoginCreature.OriginalLastName,
            CdKey = player.CDKey,
            PersonaIdString = personaIdString
        };

        _characterRepository.AddCharacter(newCharacter);
    }
}
