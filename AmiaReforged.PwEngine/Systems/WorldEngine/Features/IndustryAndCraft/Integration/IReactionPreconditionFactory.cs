using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration;

public interface IReactionPreconditionFactory
{
    IReactionPrecondition? Create(string type, IReadOnlyDictionary<string, object> parameters);
}
