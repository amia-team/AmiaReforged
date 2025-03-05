using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.Wilt;

[ServiceBinding(typeof(ISpell))]
public class Wilt : ISpell
{
    private readonly SchedulerService _schedulerService;
    public ResistSpellResult Result { get; set; }
    public string ImpactScript => "am_s_wilt";

    public Wilt(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (caster is not NwCreature casterCreature) return;

        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int extraDice = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;
        int numDice = casterCreature.CasterLevel / 2 + extraDice;

        int durationInRounds = hasEpicFocus ? 2 : 3;

        Effect effect = Effect.VisualEffect(VfxType.ComBloodCrtRed);
        target.ApplyEffect(EffectDuration.Instant, effect);

        if(NWScript.GetLocalInt(target, $"wilt_{caster.Name}") == NWScript.TRUE) return;
        
        if(NWScript.GetRacialType(target) == NWScript.RACIAL_TYPE_UNDEAD) return;
        
        if (Result == ResistSpellResult.Failed)
        {
            NWScript.SetLocalInt(target, $"wilt_{caster.Name}", NWScript.TRUE);
            int damage = NWScript.d3(numDice) / 2;
            int damagePerRound = damage / durationInRounds;
            
            // Apply the first round of damage immediately.
            Effect damageEffect = Effect.Damage(damagePerRound);
            target.ApplyEffect(EffectDuration.Instant, damageEffect);

            // We subtract 1 from the duration because the first round of damage is applied immediately.
            for (int i = 0; i < durationInRounds - 1; i++)
            {
                _schedulerService.Schedule(() => target.ApplyEffect(EffectDuration.Instant, damageEffect),
                    TimeSpan.FromSeconds(i * 6));
            }
        }
        
        // Removes the local int after the duration has passed.
        _schedulerService.Schedule(() => NWScript.DeleteLocalInt(target, $"wilt_{caster.Name}"), TimeSpan.FromSeconds(durationInRounds * 6));
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }
}