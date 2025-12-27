using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Conjuration;

/// <summary>
/// Web spell - Creates a mass of sticky webs that entangle targets.
/// Ported from nw_s0_web.nss, nw_s0_weba.nss, nw_s0_webb.nss, nw_s0_webc.nss
/// 
/// Also supports Greater Shadow Conjuration Web variant which deals cold/negative damage
/// and uses Illusion spell focus feats for DC bonuses.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class Web : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_Web";
    
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    private readonly ScriptHandleFactory _handleFactory;
    private readonly ShifterDcService _shifterDcService;
    private readonly List<NwAreaOfEffect> _webAreas = new();

    private const float OneRound = 6f;
    private const string WebEntangleTag = "WebEntangle";
    private const string WebSlowTag = "WebSlow";
    
    // Local variable keys stored on AoE
    private const string SaveDcKey = "web_save_dc";
    private const string SpellIdKey = "nSpellId";
    private const string DamageDiceKey = "web_damage_dice";
    
    // Creature variable for incorporeal check
    private const string IncorporealVar = "CREATURE_VAR_IS_INCORPOREAL";

    public Web(ScriptHandleFactory handleFactory, ShifterDcService shifterDcService)
    {
        _handleFactory = handleFactory;
        _shifterDcService = shifterDcService;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        // Cleanup invalid areas
        foreach (NwAreaOfEffect aoe in _webAreas.Where(w => !w.IsValid).ToList())
        {
            _webAreas.Remove(aoe);
        }

        if (eventData.Caster is not NwCreature casterCreature) return;
        
        if (eventData.TargetObject != null)
        {
            SpellUtils.SignalSpell(casterCreature, eventData.TargetObject, eventData.Spell);
        }

        // TODO: Revisit spider companion web spam fix
        // Dirty fix for the spider companion web spam. Will always make sure it only fires once.
        if (NWScript.GetRacialType(casterCreature) == NWScript.RACIAL_TYPE_VERMIN && 
            NWScript.GetIsPC(casterCreature) == NWScript.FALSE)
        {
            int hitDice = casterCreature.Level;
            for (int i = 1; i < 1 + hitDice / 5; i++)
            {
                NWScript.DecrementRemainingSpellUses(casterCreature, NWScript.SPELL_WEB);
            }
        }

        PersistentVfxTableEntry? webTableEntry = PersistentVfxType.PerWeb;
        if (webTableEntry == null)
        {
            Log.Error("Web VFX table entry not found.");
            return;
        }

        Effect web = Effect.AreaOfEffect(webTableEntry,
            _handleFactory.CreateUniqueHandler(OnEnterWeb),
            _handleFactory.CreateUniqueHandler(OnHeartbeatWeb),
            _handleFactory.CreateUniqueHandler(OnExitWeb));

        Location? location = eventData.TargetLocation ?? eventData.TargetObject?.Location;
        if (location == null)
        {
            Log.Error("Web target location not found.");
            return;
        }

        // Calculate duration: caster level / 2, minimum 1 round
        int effectiveCasterLevel = _shifterDcService.GetShifterCasterLevel(casterCreature, casterCreature.CasterLevel);
        int durationRounds = Math.Max(1, effectiveCasterLevel / 2);
        float duration = NWScript.RoundsToSeconds(durationRounds);
        
        // Apply Extend metamagic
        if (eventData.MetaMagicFeat == MetaMagic.Extend)
        {
            duration *= 2;
        }

        location.ApplyEffect(EffectDuration.Temporary, web, TimeSpan.FromSeconds(duration));

        // Find the created AoE and store spell data on it
        NwAreaOfEffect? webAoE = location.GetNearestObjectsByType<NwAreaOfEffect>()
            .FirstOrDefault(aoe => aoe.Spell == NwSpell.FromSpellId(NWScript.SPELL_WEB));

        if (webAoE == null)
        {
            Log.Error("Failed to create Web AoE");
            return;
        }

        // Calculate and store the DC (with Shifter support)
        int baseDc = eventData.SaveDC;
        int shifterDc = _shifterDcService.GetShifterDc(casterCreature, baseDc);
        
        // For Greater Shadow Conjuration Web, adjust DC based on Illusion/Conjuration focus
        int spellId = (int)eventData.Spell.Id;
        int finalDc = shifterDc;
        int damageDice = 2; // Base 2d6 for Greater Shadow Conjuration Web
        
        if (spellId == NWScript.SPELL_GREATER_SHADOW_CONJURATION_WEB)
        {
            // Illusion focus bonuses
            if (NWScript.GetHasFeat(NWScript.FEAT_SPELL_FOCUS_ILLUSION, casterCreature) == NWScript.TRUE)
            {
                finalDc += 2;
            }
            if (NWScript.GetHasFeat(NWScript.FEAT_GREATER_SPELL_FOCUS_ILLUSION, casterCreature) == NWScript.TRUE)
            {
                finalDc += 2;
                damageDice = 3;
            }
            if (NWScript.GetHasFeat(NWScript.FEAT_EPIC_SPELL_FOCUS_ILLUSION, casterCreature) == NWScript.TRUE)
            {
                finalDc += 2;
                damageDice = 4;
            }
            
            // Conjuration focus penalties (reverse math - these were already added by engine)
            if (NWScript.GetHasFeat(NWScript.FEAT_SPELL_FOCUS_CONJURATION, casterCreature) == NWScript.TRUE)
            {
                finalDc -= 2;
            }
            if (NWScript.GetHasFeat(NWScript.FEAT_GREATER_SPELL_FOCUS_CONJURATION, casterCreature) == NWScript.TRUE)
            {
                finalDc -= 2;
            }
            if (NWScript.GetHasFeat(NWScript.FEAT_EPIC_SPELL_FOCUS_CONJURATION, casterCreature) == NWScript.TRUE)
            {
                finalDc -= 2;
            }
        }

        NWScript.SetLocalInt(webAoE, SaveDcKey, finalDc);
        NWScript.SetLocalInt(webAoE, SpellIdKey, spellId);
        NWScript.SetLocalInt(webAoE, DamageDiceKey, damageDice);

        _webAreas.Add(webAoE);
    }

    private ScriptHandleResult OnEnterWeb(CallInfo arg)
    {
        AreaOfEffectEvents.OnEnter evtData = new();
        
        if (evtData.Entering is not NwCreature creature) return ScriptHandleResult.Handled;
        if (evtData.Effect.Creator is not NwCreature caster)
        {
            Log.Info("Unable to find caster for Web AoE.");
            return ScriptHandleResult.Handled;
        }

        NwAreaOfEffect aoe = evtData.Effect;
        
        // Skip friendly targets and DMs
        if (!SpellUtils.IsValidHostileTarget(creature, caster))
        {
            return ScriptHandleResult.Handled;
        }

        // Woodland Stride and Incorporeal immunity
        if (NWScript.GetHasFeat(NWScript.FEAT_WOODLAND_STRIDE, creature) == NWScript.TRUE)
        {
            return ScriptHandleResult.Handled;
        }
        if (NWScript.GetLocalInt(creature, IncorporealVar) == NWScript.TRUE)
        {
            return ScriptHandleResult.Handled;
        }

        // Signal spell cast
        NWScript.SignalEvent(creature, NWScript.EventSpellCastAt(caster, NWScript.SPELL_WEB));

        // Spell resistance check
        if (SpellUtils.MyResistSpell(caster, creature))
        {
            return ScriptHandleResult.Handled;
        }

        // Get stored DC
        int saveDc = NWScript.GetLocalInt(aoe, SaveDcKey);
        if (saveDc <= 0) saveDc = 14; // Fallback

        // Reflex save to avoid entangle
        if (creature.RollSavingThrow(SavingThrow.Reflex, saveDc, SavingThrowType.None) == SavingThrowResult.Failure)
        {
            ApplyEntangleEffects(creature, caster, aoe);
        }

        // Apply movement speed decrease regardless of save (but after spell resistance)
        ApplyMovementPenalty(creature);

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult OnHeartbeatWeb(CallInfo arg)
    {
        AreaOfEffectEvents.OnHeartbeat evtData = new();
        
        if (evtData.Effect is not NwAreaOfEffect aoe) return ScriptHandleResult.Handled;
        if (aoe.Creator is not NwCreature caster)
        {
            Log.Info("Unable to find caster for Web AoE.");
            return ScriptHandleResult.Handled;
        }

        int saveDc = NWScript.GetLocalInt(aoe, SaveDcKey);
        if (saveDc <= 0) saveDc = 14;
        
        int spellId = NWScript.GetLocalInt(aoe, SpellIdKey);
        int damageDice = NWScript.GetLocalInt(aoe, DamageDiceKey);
        if (damageDice <= 0) damageDice = 2;

        List<NwCreature> creatures = aoe.GetObjectsInEffectArea<NwCreature>().ToList();

        foreach (NwCreature creature in creatures)
        {
            if (!SpellUtils.IsValidHostileTarget(creature, caster)) continue;

            // Woodland Stride and Incorporeal immunity
            if (NWScript.GetHasFeat(NWScript.FEAT_WOODLAND_STRIDE, creature) == NWScript.TRUE) continue;
            if (NWScript.GetLocalInt(creature, IncorporealVar) == NWScript.TRUE) continue;

            // Signal spell cast
            NWScript.SignalEvent(creature, NWScript.EventSpellCastAt(caster, NWScript.SPELL_WEB));

            // Spell resistance check
            if (SpellUtils.MyResistSpell(caster, creature)) continue;

            // Reflex save to avoid entangle
            if (creature.RollSavingThrow(SavingThrow.Reflex, saveDc, SavingThrowType.None) == SavingThrowResult.Failure)
            {
                ApplyEntangleEffects(creature, caster, aoe);
                
                // Greater Shadow Conjuration Web deals cold and negative damage
                if (spellId == NWScript.SPELL_GREATER_SHADOW_CONJURATION_WEB)
                {
                    ApplyGreaterShadowDamage(creature, damageDice);
                }
            }
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnExitWeb(CallInfo arg)
    {
        AreaOfEffectEvents.OnExit evtData = new();
        NwGameObject obj = evtData.Exiting;

        // Remove all Web effects from this creature
        foreach (Effect effect in obj.ActiveEffects.ToList())
        {
            if (effect.Tag == WebEntangleTag || effect.Tag == WebSlowTag)
            {
                // Only remove if the effect creator matches the AoE creator
                if (effect.Creator == evtData.Effect.Creator)
                {
                    obj.RemoveEffect(effect);
                }
            }
        }

        return ScriptHandleResult.Handled;
    }

    private static void ApplyEntangleEffects(NwCreature creature, NwCreature caster, NwAreaOfEffect aoe)
    {
        Effect entangle = Effect.Entangle();
        Effect webVfx = Effect.VisualEffect(VfxType.DurWeb);
        Effect linked = Effect.LinkEffects(entangle, webVfx);
        linked.Tag = WebEntangleTag;

        creature.ApplyEffect(EffectDuration.Temporary, linked, TimeSpan.FromSeconds(OneRound));

        // Dex fix from original script - apply minor Dex boost after delay to fix animation issues
        // Note: Using DelayCommand pattern via async/await would be cleaner but keeping sync for now
        Effect dexFix = Effect.AbilityIncrease(Ability.Dexterity, 1);
        dexFix.SubType = EffectSubType.Supernatural;
        NWScript.DelayCommand(5.5f, () => creature.ApplyEffect(EffectDuration.Temporary, dexFix, TimeSpan.FromSeconds(1)));
    }

    private static void ApplyMovementPenalty(NwCreature creature)
    {
        // Remove existing slow effect first
        Effect? existingSlow = creature.ActiveEffects.FirstOrDefault(e => e.Tag == WebSlowTag);
        if (existingSlow != null)
        {
            creature.RemoveEffect(existingSlow);
        }

        // Calculate movement penalty based on Strength (65 - STR*2, clamped 1-99)
        int strength = creature.GetAbilityScore(Ability.Strength);
        int slowPercent = Math.Clamp(65 - (strength * 2), 1, 99);

        Effect moveSlow = Effect.MovementSpeedDecrease(slowPercent);
        moveSlow.Tag = WebSlowTag;
        
        // Permanent until exit
        creature.ApplyEffect(EffectDuration.Permanent, moveSlow);
    }

    private static void ApplyGreaterShadowDamage(NwCreature creature, int damageDice)
    {
        // Cold damage
        int coldDamage = NWScript.d6(damageDice);
        Effect cold = Effect.Damage(coldDamage, DamageType.Cold);
        Effect coldVfx = Effect.VisualEffect(VfxType.ImpFrostS);
        Effect coldLinked = Effect.LinkEffects(cold, coldVfx);
        creature.ApplyEffect(EffectDuration.Instant, coldLinked);

        // Negative damage
        int negDamage = NWScript.d6(damageDice);
        Effect neg = Effect.Damage(negDamage, DamageType.Negative);
        Effect negVfx = Effect.VisualEffect(VfxType.ImpNegativeEnergy);
        Effect negLinked = Effect.LinkEffects(neg, negVfx);
        creature.ApplyEffect(EffectDuration.Instant, negLinked);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}
