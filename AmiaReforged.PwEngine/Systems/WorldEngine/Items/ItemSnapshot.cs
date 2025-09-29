using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public record ItemSnapshot(string Tag, string Name, string Description, IPQuality Quality, MaterialEnum[] Materials, JobSystemItemType Type, int BaseItemType, byte[]? Serialized);
