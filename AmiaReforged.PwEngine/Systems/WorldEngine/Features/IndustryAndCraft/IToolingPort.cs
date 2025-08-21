using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IToolingPort
{
    Task<IReadOnlyList<ToolInstance>> GetToolsAsync(Guid actorId, CancellationToken ct = default);
}