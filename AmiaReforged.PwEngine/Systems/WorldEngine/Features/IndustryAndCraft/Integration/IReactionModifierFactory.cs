using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration;

public interface IReactionModifierFactory
{
    IReactionModifier? Create(string type, IReadOnlyDictionary<string, object> parameters);
}
