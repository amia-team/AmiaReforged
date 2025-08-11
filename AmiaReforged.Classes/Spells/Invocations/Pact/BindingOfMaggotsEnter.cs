using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
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
        float summonDuration = RoundsToSeconds(SummonUtility.PactSummonDuration(caster));
        float summonCooldown = TurnsToSeconds(1);
        IntPtr cooldownEffect = TagEffect(ExtraordinaryEffect(EffectVisualEffect(VFX_NONE)),
            sNewTag: "wlk_summon_cd");
        IntPtr location = GetLocation(enteringObject);

        // Trigger only if a hostile creature enters
        if (!NwEffects.IsValidSpellTarget(enteringObject, 3, caster)) return;

        SignalEvent(enteringObject, EventSpellCastAt(caster, 1011));

        //---------------------------
        // * SUMMONING
        //---------------------------

        // If summonCooldown is active, don't summon; else summon and set summonCooldown
        if (NwEffects.GetHasEffectByTag(effectTag: "wlk_summon_cd", caster) == FALSE)
        {
            // Unsummon previous warlock summons
            NwCreature? warlock = caster.ToNwObject() as NwCreature;
            if (warlock == null) return;

            foreach (NwCreature associate in warlock.Associates)
            {
                if (associate.ResRef.Contains("wlk"))
                    associate.Unsummon();
            }

            _ = SummonUtility.SummonMany(warlock, VFX_IMP_DESTRUCTION, VFX_IMP_DESTRUCTION, summonDuration, summonCount,
                "wlkfiend", location, 0.5f, 2f, 0.8f, 1.8f);

            // Apply cooldown
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, cooldownEffect, caster, summonCooldown);
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

        bool passedWillSave =
            WillSave(enteringObject, WarlockConstants.CalculateDc(caster), SAVING_THROW_TYPE_EVIL, caster) == TRUE;

        if (passedWillSave)
        {
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_WILL_SAVING_THROW_USE),
                enteringObject);
            return;
        }

        if (!passedWillSave)
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, hostileEffects, enteringObject, effectDuration);
    }
}
