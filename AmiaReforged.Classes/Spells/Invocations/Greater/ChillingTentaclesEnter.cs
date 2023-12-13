using AmiaReforged.Classes.EffectUtils;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class ChillingTentaclesEnter
{
    public void ChillingTentaclesEnterEffects(uint nwnObjectId)
    {
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        uint enteringObject = GetEnteringObject();

        if (!NwEffects.IsValidSpellTarget(enteringObject, 2, caster)) return;
        SignalEvent(enteringObject, EventSpellCastAt(nwnObjectId, GetSpellId()));
        EldritchTentacle.StrikeTargetWithTentacle(enteringObject, caster);
    }
}