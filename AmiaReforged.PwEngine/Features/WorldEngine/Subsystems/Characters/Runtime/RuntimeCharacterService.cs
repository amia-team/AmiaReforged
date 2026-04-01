using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;

[ServiceBinding(typeof(RuntimeCharacterService))]
public class RuntimeCharacterService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ICharacterRepository _repository;
    private readonly IPersistentPlayerPersonaRepository _playerPersonas;
    private readonly Dictionary<NwPlayer, Guid> _playerKeys = new();

    /// <summary>
    /// Raised after a character is fully registered with a valid key.
    /// Subscribers can safely call <see cref="TryGetPlayerKey"/> for this character.
    /// </summary>
    public event Action<CharacterId>? CharacterReady;

    /// <summary>
    /// Raised just before a character's key is removed on logout.
    /// Subscribers should tear down any character-scoped state here.
    /// </summary>
    public event Action<CharacterId>? CharacterLeaving;

    public RuntimeCharacterService(ICharacterRepository repository, IPersistentPlayerPersonaRepository playerPersonas)
    {
        _repository = repository;
        _playerPersonas = playerPersonas;
        NwModule.Instance.OnAcquireItem += ReCache;
        NwModule.Instance.OnClientEnter += Register;
        NwModule.Instance.OnClientLeave += Unregister;
    }

    private void Unregister(ModuleEvents.OnClientLeave obj)
    {
        if (obj.Player.IsDM) return;

        // Notify subscribers before removing the key so they can still resolve the character
        if (_playerKeys.TryGetValue(obj.Player, out Guid leavingKey) && leavingKey != Guid.Empty)
        {
            CharacterLeaving?.Invoke(CharacterId.From(leavingKey));
        }

        TouchPlayerPersona(obj.Player);
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

        ObservePlayerPersona(player);

        Guid key = PcKeyUtils.GetPcKey(player);

        bool wasEmpty = _playerKeys.TryGetValue(player, out Guid previousKey) && previousKey == Guid.Empty;
        _playerKeys[player] = key;

        if (key == Guid.Empty) return;
        if (player.LoginCreature == null) return;

        ObjectPlugin.ForceAssignUUID(player.LoginCreature, key.ToUUIDString());
        CreateRuntimeCharacter(player.LoginCreature);
        SetIsCached(player.LoginCreature);

        // If the key just became valid (e.g. pckey item loaded after login), notify subscribers
        if (wasEmpty)
        {
            CharacterReady?.Invoke(CharacterId.From(key));
        }
    }

    private void Register(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        ObservePlayerPersona(obj.Player);

        Guid key = PcKeyUtils.GetPcKey(obj.Player);

        _playerKeys.TryAdd(obj.Player, key);

        if (key == Guid.Empty) return;

        if (obj.Player.LoginCreature == null) return;

        CreateRuntimeCharacter(obj.Player.LoginCreature);
        ObjectPlugin.ForceAssignUUID(obj.Player.LoginCreature, key.ToUUIDString());
        SetIsCached(obj.Player.LoginCreature);

        CharacterReady?.Invoke(CharacterId.From(key));
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

    private void ObservePlayerPersona(NwPlayer player)
    {
        if (player is not { IsValid: true })
        {
            return;
        }

        string cdKey = player.CDKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cdKey))
        {
            return;
        }

        string displayName = string.IsNullOrWhiteSpace(player.PlayerName) ? cdKey : player.PlayerName;

        try
        {
            _playerPersonas.Upsert(cdKey, displayName, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to persist player persona for CD key {CdKey}.", cdKey);
        }
    }

    private void TouchPlayerPersona(NwPlayer player)
    {
        if (player is not { IsValid: true })
        {
            return;
        }

        string cdKey = player.CDKey ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cdKey))
        {
            return;
        }

        try
        {
            _playerPersonas.Touch(cdKey, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to mark player persona activity for CD key {CdKey}.", cdKey);
        }
    }

    public Guid GetPlayerKey(NwPlayer player)
    {
        return _playerKeys[player];
    }

    public bool TryGetPlayerKey(NwPlayer player, out Guid key)
    {
        return _playerKeys.TryGetValue(player, out key);
    }

    public bool TryGetPlayer(Guid key, out NwPlayer? player)
    {
        foreach ((NwPlayer candidate, Guid storedKey) in _playerKeys)
        {
            if (storedKey == key && candidate is { IsValid: true })
            {
                player = candidate;
                return true;
            }
        }

        player = null;
        return false;
    }

    public RuntimeCharacter? GetRuntimeCharacter(NwCreature creature)
    {
        return _repository.GetById(creature.UUID) as RuntimeCharacter;
    }
}
