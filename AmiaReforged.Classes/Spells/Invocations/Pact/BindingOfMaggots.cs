using AmiaReforged.Classes.EffectUtils;
using NUnit.Framework.Constraints;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class BindingOfMaggots
{
    public void CastBindingOfMaggots(uint nwnObjectId)
    {
        if (NwEffects.IsPolymorphed(nwnObjectId)){
            SendMessageToPC(nwnObjectId, "You cannot cast while polymorphed.");
            return;
        }

        IntPtr binding = EffectAreaOfEffect(38, "wlk_bindingent", "****", "****");  // VFX_PER_GLYPH
        IntPtr location = GetSpellTargetLocation();
        float duration = TurnsToSeconds(1);
        string plcTag = "circle"+GetSubString(GetName(nwnObjectId), 0, 2);

        DestroyObject(GetObjectByTag(plcTag));
        NwEffects.RemoveAoeWithTag(location, nwnObjectId, "VFX_PER_GLYPH", RADIUS_SIZE_COLOSSAL);

        CreateObject(OBJECT_TYPE_PLACEABLE, "amia_plc_req055", location, 0, plcTag);
        DestroyObject(GetObjectByTag(plcTag), duration);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, binding, location, duration);
    }
}