using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using AmiaReforged.Classes.Warlock.EldritchBlast.Shape;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.Classes.Warlock.Constants;

namespace AmiaReforged.Classes.Warlock.EldritchBlast;

[ServiceBinding(typeof(EldritchBlastHandler))]
public class EldritchBlastHandler(EssenceFactory essenceFactory, ShapeFactory shapeFactory)
{
    public void HandleEldritchBlast(NwCreature warlock, int invocationCl,
        SpellEvents.OnSpellCast castData)
    {
        ShapeType shapeType = (ShapeType)castData.Spell.Id;
        int dc = warlock.InvocationDc(invocationCl);

        EssenceData essenceData = essenceFactory.GetEssenceData(warlock, invocationCl);

        shapeFactory.GetShapeType(shapeType)?
            .CastEldritchShape(warlock, invocationCl, dc, essenceData, castData);

        if (warlock.KnowsFeat(WarlockFeat.EldritchMaster!))
            warlock.ApplyEldritchMasterAttackBonus();
    }
}
