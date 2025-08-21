using System.Collections.Immutable;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public interface IReactionActor
{
    Guid ActorId { get; }
    ImmutableHashSet<KnowledgeKey> Knowledge { get; }
    ImmutableArray<ToolInstance> Tools { get; }
}
