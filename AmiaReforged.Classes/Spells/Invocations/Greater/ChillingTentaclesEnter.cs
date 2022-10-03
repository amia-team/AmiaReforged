using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class ChillingTentaclesEnter
{
    public int Run(uint nwnObjectId)
    {
        uint caster = GetAreaOfEffectCreator(nwnObjectId);
        uint enteringObject = GetEnteringObject();

        if (enteringObject == caster || GetIsFriend(enteringObject) == FALSE) return 0;
        int spellId = GetSpellId();
        SignalEvent(enteringObject, EventSpellCastAt(nwnObjectId, spellId));
        EldritchTentacle.StrikeTargetWithTentacle(enteringObject, caster);

        return 0;
    }
}