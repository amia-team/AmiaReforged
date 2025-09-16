using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

// [ServiceBinding(typeof(RuntimeCharacterManager))]
public class RuntimeCharacterManager : ICharacterManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly InMemoryCharacterRepository _repository;

    public RuntimeCharacterManager(InMemoryCharacterRepository repository)
    {
        _repository = repository;

        NwModule.Instance.OnClientEnter += RegisterCharacter;
        NwModule.Instance.OnClientLeave += UnregisterCharacter;
    }

    private void RegisterCharacter(ModuleEvents.OnClientEnter obj)
    {
        Guid pcKey = PcKeyUtils.GetPcKey(obj.Player);


        if (pcKey == Guid.Empty)
        {
            Log.Error("Failed to register character.");
        }

        ObjectPlugin.ForceAssignUUID(obj.Player.LoginCreature, pcKey.ToString());
    }

    private void UnregisterCharacter(ModuleEvents.OnClientLeave obj)
    {
        throw new NotImplementedException();
    }

    public ICharacter? GetCharacter(Guid id)
    {
        return _repository.GetById(id);
    }
}
