using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;

public record KnowledgeHarvestEffect(
    string NodeTag,
    HarvestStep StepModified,
    float Value,
    EffectOperation Operation);
