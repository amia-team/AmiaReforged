using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;

[ServiceBinding(typeof(RuntimeCharacterService))]
public class RuntimeCharacterService
{
    private readonly ICharacterRepository _repository;
    private readonly Dictionary<NwPlayer, Guid> _playerKeys = new();

    public RuntimeCharacterService(ICharacterRepository repository)
    {
        _repository = repository;
        NwModule.Instance.OnAcquireItem += ReCache;
        NwModule.Instance.OnClientEnter += Register;
        NwModule.Instance.OnClientLeave += Unregister;
    }

    private void Unregister(ModuleEvents.OnClientLeave obj)
    {
        if (obj.Player.IsDM) return;
        _playerKeys.Remove(obj.Player);
        if (obj.Player.LoginCreature == null) return;

        DeleteRuntimeCharacter(obj.Player.LoginCreature);
        NWScript.SetLocalInt(obj.Player.LoginCreature, WorldConstants.PcCachedLvar, NWScript.FALSE);
    }

    private void ReCache(ModuleEvents.OnAcquireItem obj)
    {
        NwItem? objItem = obj.Item;
        if (objItem is null) return;
        if (objItem.Tag != "ds_pckey") return;
        if (!obj.AcquiredBy.IsPlayerControlled(out NwPlayer? player)) return;

        Guid key = PcKeyUtils.GetPcKey(player);

        _playerKeys[player] = key;

        if (key == Guid.Empty) return;
        if (player.LoginCreature == null) return;

        ObjectPlugin.ForceAssignUUID(player.LoginCreature, key.ToUUIDString());
        CreateRuntimeCharacter(player.LoginCreature);
        SetIsCached(player.LoginCreature);
    }

    private void Register(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        Guid key = PcKeyUtils.GetPcKey(obj.Player);

        _playerKeys.TryAdd(obj.Player, key);

        if (key == Guid.Empty) return;

        if (obj.Player.LoginCreature == null) return;

        CreateRuntimeCharacter(obj.Player.LoginCreature);
        ObjectPlugin.ForceAssignUUID(obj.Player.LoginCreature, key.ToUUIDString());
        SetIsCached(obj.Player.LoginCreature);
    }

    private void CreateRuntimeCharacter(NwCreature creature)
    {
        RuntimeCharacter? character = RuntimeCharacter.For(creature);
        if (character != null)
        {
            _repository.Add(character);
        }
    }

    private void DeleteRuntimeCharacter(NwCreature creature)
    {
        Guid id = creature.UUID;
        _repository.DeleteById(id);
    }

    private void SetIsCached(NwCreature playerLoginCreature)
    {
        NWScript.SetLocalInt(playerLoginCreature, WorldConstants.PcCachedLvar, NWScript.TRUE);
    }

    public Guid GetPlayerKey(NwPlayer player)
    {
        return _playerKeys[player];
    }

    public RuntimeCharacter? GetRuntimeCharacter(NwCreature creature)
    {
        return _repository.GetById(creature.UUID) as RuntimeCharacter;
    }
}
