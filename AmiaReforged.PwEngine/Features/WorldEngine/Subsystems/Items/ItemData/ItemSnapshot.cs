using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

public record ItemSnapshot(string Tag, string Name, string Description, IPQuality Quality, MaterialEnum[] Materials, JobSystemItemType Type, int BaseItemType, byte[]? Serialized);
