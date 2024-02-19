namespace AmiaReforged.Races.Races.Script.Types;

public interface IEffectCollector
{
    List<IntPtr> GatherEffectsForObject(uint objectId);
}