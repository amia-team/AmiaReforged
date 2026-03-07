using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Stub implementation of <see cref="IKnowledgeEffectProcessor"/>.
/// Logs each effect for now — real dispatchers (codex grants, recipe unlocks)
/// will be added incrementally as those subsystems mature.
/// </summary>
[ServiceBinding(typeof(IKnowledgeEffectProcessor))]
public class StubKnowledgeEffectProcessor : IKnowledgeEffectProcessor
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task ProcessEffectsAsync(CharacterId characterId, Knowledge knowledge, List<KnowledgeEffect> effects)
    {
        foreach (KnowledgeEffect effect in effects)
        {
            Log.Info(
                $"[KnowledgeEffect] Character {characterId} learned '{knowledge.Tag}' — " +
                $"effect {effect.EffectType}: target='{effect.TargetTag}' " +
                $"(metadata keys: {string.Join(", ", effect.Metadata.Keys)})");
        }

        return Task.CompletedTask;
    }
}
