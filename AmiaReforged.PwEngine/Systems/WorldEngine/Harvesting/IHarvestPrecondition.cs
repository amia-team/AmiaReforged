namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface IHarvestPrecondition
{
    bool IsMet(ICharacter c);
}