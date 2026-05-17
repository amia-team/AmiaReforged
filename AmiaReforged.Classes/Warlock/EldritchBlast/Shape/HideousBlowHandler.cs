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
        NwModule.Instance.OnCreatureDamage += ProcHideousBlow;
    }

    private void ProcHideousBlow(OnCreatureDamage eventData)
    {
        if (eventData.DamagedBy is not NwCreature warlock
            || eventData.Spell != null // Don't trigger on spells
            || !warlock.HasSpellUse(HideousBlow!))
            return;

        NwGameObject target = eventData.Target;
        Location? location = target.Location;
        if (location == null) return;

        CreaturePlugin.DoItemCastSpell(warlock, target, location, (int)HideousBlow, 0, 0);
    }
}
