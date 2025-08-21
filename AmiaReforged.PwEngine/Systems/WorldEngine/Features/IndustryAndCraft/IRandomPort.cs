namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IRandomPort
{
    // Returns double in [0,1)
    double NextUnit();
}