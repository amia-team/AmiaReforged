using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;

public record KnowledgeHarvestEffect(
    string NodeTag,
    HarvestStep StepModified,
    float Value,
    EffectOperation Operation);
