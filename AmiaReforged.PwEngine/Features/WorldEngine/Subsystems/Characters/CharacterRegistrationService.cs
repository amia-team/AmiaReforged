using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

[ServiceBinding(typeof(CharacterRegistrationService))]
public class CharacterRegistrationService
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly RuntimeCharacterService _runtimeCharacterService;

    public CharacterRegistrationService(ICommandDispatcher commandDispatcher,
        RuntimeCharacterService runtimeCharacterService)
    {
        _commandDispatcher = commandDispatcher;
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

        CharacterId characterId = CharacterId.From(pcKey);
        _commandDispatcher.DispatchAsync(new RegisterCharacterCommand(
            characterId,
            player.LoginCreature.OriginalFirstName,
            player.LoginCreature.OriginalLastName,
            player.CDKey), CancellationToken.None);
    }
}
