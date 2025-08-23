namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public record ResourceNodeDefinition(string Tag, IHarvestPrecondition[] Preconditions, HarvestOutput[] Outputs);
