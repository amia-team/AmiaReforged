using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;

public record WorldItem(string Name, EconomyItem Definition, QualityEnum Quality);

public record Transaction(IAgent Buyer, IAgent Seller, WorldItem Item);

public interface IAgent
{
}

