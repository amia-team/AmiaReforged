using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;

public record KnowledgeHarvestEffect(
    string NodeTag,
    HarvestStep StepModified,
    float Value,
    EffectOperation Operation);