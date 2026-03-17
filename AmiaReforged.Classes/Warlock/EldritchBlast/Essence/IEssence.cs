using Anvil.API;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence;

public interface IEssence
{
    EssenceType Essence { get; }
    EssenceData GetEssenceData(int invocationCl, NwCreature warlock);
}
