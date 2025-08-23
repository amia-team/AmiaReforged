using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public record ItemSnapshot(string Tag, IPQuality Quality, Material Material, JobSystemItemType Type, int BaseItemType);