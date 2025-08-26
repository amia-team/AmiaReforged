namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface IHarvestingModifier
{
    bool IsMet();
    void Calculate(HarvestOutput output);
}

public class ToolHarvestModifier : IHarvestingModifier
{
    public bool IsMet()
    {
        throw new NotImplementedException();
    }

    public void Calculate(HarvestOutput output)
    {
        throw new NotImplementedException();
    }
}
