using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape;

public interface IShape
{
    ShapeType ShapeType { get; }
    public void CastEldritchShape(NwCreature warlock, int warlockLevel, int invocationDc, EssenceData essence,
        SpellEvents.OnSpellCast castData);
}
