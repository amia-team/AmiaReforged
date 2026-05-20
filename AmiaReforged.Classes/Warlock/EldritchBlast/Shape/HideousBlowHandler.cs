using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Shape;

[ServiceBinding(typeof(HideousBlowHandler))]
public class HideousBlowHandler
{
    private const Spell HideousBlow = (Spell)ShapeType.HideousBlow;

    public HideousBlowHandler()
    {
        NwModule.Instance.OnCreatureAttack += ProcHideousBlow;
    }

    private void ProcHideousBlow(OnCreatureAttack eventData)
    {
        if (!IsHit(eventData.AttackResult) || !eventData.Attacker.HasSpellUse(HideousBlow!))
            return;

        NwGameObject target = eventData.Target;
        Location? location = target.Location;
        if (location == null) return;

        CreaturePlugin.DoItemCastSpell(eventData.Attacker, target, location, (int)HideousBlow, 0, 0);
    }

    private static bool IsHit(AttackResult attackResult) => attackResult is AttackResult.Hit or AttackResult.CriticalHit
        or AttackResult.AutomaticHit or AttackResult.DevastatingCritical;
}
