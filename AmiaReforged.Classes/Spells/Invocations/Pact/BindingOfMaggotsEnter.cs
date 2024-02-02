using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public class BindingOfMaggotsEnter
{
    public void BindingOfMaggotsEnterEffects(NwObject aoe)
    {
        // Declaring variables for the damage part of the spell
        uint caster = GetAreaOfEffectCreator(aoe);
        uint enteringObject = GetEnteringObject();
        int warlockLevels = GetLevelByClass(57, caster);
        float effectDuration = warlockLevels < 10 ? RoundsToSeconds(1) : RoundsToSeconds(warlockLevels / 10);
        IntPtr hostileEffects = NwEffects.LinkEffectList(new List<IntPtr>
        {
            EffectCutsceneImmobilize(),
            EffectVisualEffect(VFX_FNF_DEMON_HAND)
        });

        // Declaring variables for the summon part of the spell
        int summonCount = warlockLevels < 5 ? 1 : 1 + warlockLevels / 5;
        float summonDuration = RoundsToSeconds(5 + warlockLevels / 2);
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(SupernaturalEffect(EffectVisualEffect(VFX_DUR_CESSATE_NEUTRAL)), "wlk_summon_cd");
        IntPtr location = GetLocation(enteringObject);

        // Trigger only if a hostile creature enters
        if (!NwEffects.IsValidSpellTarget(enteringObject, 3, caster)) return;

        SignalEvent(enteringObject, EventSpellCastAt(caster, 1011));

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is active, don't summon; else summon and set summonCooldown
        if (NwEffects.GetHasEffectByTag("wlk_summon_cd", caster) == FALSE)
        {
            // Apply summonCooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
            DelayCommand(summonCooldown, () => FloatingTextStringOnCreature(NwEffects.WarlockString("Soul Larvae can be summoned again."), caster, 0));
            // Summon new
            SummonEffects.SummonMany(caster, summonDuration, summonCount, "wlkfiend", location, 0.3f, 2f, 1f, 3f, VFX_FNF_SUMMON_EPIC_UNDEAD, 0.1f);
            DelayCommand(3.1f, () =>  SummonEffects.SetSummonsFacing(summonCount, location));
        }

        //---------------------------
        // * HOSTILE SPELL EFFECT
        //---------------------------

        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_EVIL_HELP), enteringObject);

        if (NwEffects.ResistSpell(caster, enteringObject)) return;
        if (GetHasSpellEffect(SPELL_PROTECTION_FROM_EVIL | SPELL_HOLY_AURA, enteringObject) == TRUE)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_GLOBE_USE), enteringObject);
            return;
        }

        bool passedWillSave = WillSave(enteringObject, NwEffects.CalculateDC(caster), SAVING_THROW_TYPE_EVIL, caster) == TRUE;

        if (passedWillSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE), enteringObject);
            return;
        }
        if (!passedWillSave) ApplyEffectToObject(DURATION_TYPE_TEMPORARY, hostileEffects, enteringObject, effectDuration);
    }
}