using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using AmiaReforged.Classes.Warlock.EldritchBlast.Shape;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using AmiaReforged.Classes.Warlock.Constants;

namespace AmiaReforged.Classes.Warlock.EldritchBlast;

[ServiceBinding(typeof(BlastHandler))]
public class BlastHandler
{
    private const string EldritchBlastSpellScript = "wlk_el_blst";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly EssenceFactory _essenceFactory;
    private readonly ShapeFactory _shapeFactory;

    public BlastHandler(EssenceFactory essenceFactory, ShapeFactory shapeFactory, ScriptHandleFactory scriptHandleFactory)
    {
        _essenceFactory = essenceFactory;
        _shapeFactory = shapeFactory;

        scriptHandleFactory.RegisterScriptHandler(EldritchBlastSpellScript, HandleEldritchBlast);

        Log.Info("Eldritch Blast Handler initialized and script handler registered.");
    }

    private ScriptHandleResult HandleEldritchBlast(CallInfo info)
    {
        SpellEvents.OnSpellCast eventData = new();
        if (eventData.Caster is not NwCreature warlock) return ScriptHandleResult.Handled;

        ShapeType shapeType = (ShapeType)eventData.Spell.Id;
        int warlockLevel = warlock.WarlockLevel();
        int dc = warlock.InvocationDc(warlockLevel);

        EssenceData essenceData = _essenceFactory.GetEssenceData(warlock, warlockLevel);

        SpellEvents.OnSpellCast castData = new();

        _shapeFactory.GetShapeType(shapeType)?
            .CastEldritchShape(warlock, warlockLevel, dc, essenceData, castData);

        if (warlock.KnowsFeat(WarlockFeats.EldritchMaster!))
            warlock.ApplyEldritchMasterAttackBonus();

        return ScriptHandleResult.Handled;
    }
}
