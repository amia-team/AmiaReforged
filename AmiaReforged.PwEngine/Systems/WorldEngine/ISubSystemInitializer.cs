using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface ISubSystemInitializer
{
    void Init(EconomySubsystem subsystem);
}
