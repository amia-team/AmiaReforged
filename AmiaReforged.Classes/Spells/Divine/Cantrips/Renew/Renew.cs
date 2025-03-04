using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.Renew;

[ServiceBinding(typeof(ISpell))]
public class Renew : ISpell
{
    private readonly SchedulerService _schedulerService;
    public ResistSpellResult Result { get; set; }
    public string ImpactScript => "am_s_renew";

    public Renew(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        // This is similar to wilt, but it heals instead of damaging.
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;
        
        if(NWScript.GetRacialType(target) == NWScript.RACIAL_TYPE_UNDEAD) return;

        if (caster is not NwCreature casterCreature) return;

        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int extraDice = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;

        int numDice = casterCreature.CasterLevel / 2 + extraDice;

        int durationInRounds = hasEpicFocus ? 2 : 3;

        if (NWScript.GetLocalInt(target, $"renew_{caster.Name}") == NWScript.TRUE) return;

        int healing = NWScript.d3(numDice) / 2;
        int healingPerRound = healing / durationInRounds;
        
        Effect vfx = Effect.VisualEffect(VfxType.ImpHealingS);
        target.ApplyEffect(EffectDuration.Instant, vfx);

        // Apply the first round of healing immediately.
        Effect healingEffect = Effect.Heal(healingPerRound);

        // Subtract 1 from the duration because the first round of healing is applied immediately.
        for (int i = 0; i < durationInRounds - 1; i++)
        {
            _schedulerService.Schedule(() => { target.ApplyEffect(EffectDuration.Instant, healingEffect); },
                TimeSpan.FromSeconds(i * 6));
        }
    }

    public void SetResult(ResistSpellResult result)
    {
        Result = result;
    }
}