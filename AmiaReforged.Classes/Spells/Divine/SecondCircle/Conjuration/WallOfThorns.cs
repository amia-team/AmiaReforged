using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Moq;

namespace AmiaReforged.Classes.Spells.Divine.SecondCircle.Conjuration;

/// <summary>
/// Level: Druid 2
/// Area of effect: Wall 30 ft long
/// Duration: 1 Round/2 Caster Level
/// Valid Metamagic: Still, Silent, Extend, Empower, Maximize
/// Save: None
/// Spell Resistance: No
/// This spell creates a barrier of very tough, pliable, tangled brush bearing needle-sharp thorns of a finger's length.
/// Any creature forced into or attempting to move through a wall of thorns is attacked with an AB of caster level
/// + wisdom modifier + Spell Focus Bonus against the target's AC. Should the roll hit, the victim will take 2d6
/// points of piercing damage and have their movement speed reduced by 50%.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class WallOfThorns(ScriptHandleFactory scriptHandleFactory) : ISpell
{
    private const PersistentVfxType PerWallthorn = (PersistentVfxType)59;
    private const VfxType DurThornWall = (VfxType)2548;
    public string ImpactScript => "wall_of_thorns";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetLocation == null || eventData.Caster is not { } casterObject) return;

        MetaMagic metaMagic = eventData.MetaMagicFeat;
        TimeSpan duration = NwTimeSpan.FromRounds(casterObject.CasterLevel / 2);
        if (metaMagic == MetaMagic.Extend) duration *= 2;

        PersistentVfxTableEntry? thornWallVfx = PerWallthorn;
        if (thornWallVfx == null) return;

        int wisAb = 0;
        int spellFocusAb = 0;
        if (casterObject is NwCreature casterCreature)
        {
            wisAb = casterCreature.GetAbilityModifier(Ability.Wisdom);
            spellFocusAb = casterCreature.KnowsFeat(Feat.EpicSpellFocusConjuration!) ? 6 :
                 casterCreature.KnowsFeat(Feat.GreaterSpellFocusConjuration!) ? 4 :
                 casterCreature.KnowsFeat(Feat.SpellFocusConjuration!) ? 2 : 0;
        }

        int thornAb = casterObject.CasterLevel + wisAb + spellFocusAb;

        ScriptCallbackHandle thornWallEnter =
            scriptHandleFactory.CreateUniqueHandler(info
                => OnEnterThorns(info, casterObject, thornAb, metaMagic, eventData.Spell));

        Effect wallOfThorns = Effect.AreaOfEffect(thornWallVfx, onEnterHandle: thornWallEnter);
        wallOfThorns.SubType = EffectSubType.Supernatural;
        eventData.TargetLocation.ApplyEffect(EffectDuration.Temporary, wallOfThorns, duration);

        _ = CreateThornWallVfx(casterObject, eventData.TargetLocation, duration);
    }

    /// <summary>
    /// The only way I figured out how to get the thorn wall VFX to orientate properly in relation to the caster...
    /// </summary>
    private static async Task CreateThornWallVfx(NwGameObject casterObject, Location spellLocation, TimeSpan duration)
    {
        if (casterObject.Area == null) return;

        Location locationFacingCaster = Location.Create(casterObject.Area, spellLocation.Position, casterObject.Rotation);
        NwCreature? dummyObject = NwCreature.Create("gen_sum_cre", locationFacingCaster);
        if (dummyObject == null) return;

        dummyObject.Commandable = false;
        dummyObject.Immortal = true;
        Effect cutsceneGhost = Effect.CutsceneGhost();
        cutsceneGhost.SubType = EffectSubType.Unyielding;
        dummyObject.ApplyEffect(EffectDuration.Permanent, cutsceneGhost);
        dummyObject.UiDiscoveryFlags = ObjectUiDiscovery.None;
        dummyObject.MouseCursor = MouseCursor.Walk;
        dummyObject.Useable = false;

        dummyObject.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(DurThornWall), duration);

        await NwTask.Delay(duration);
        dummyObject.Immortal = false;
        dummyObject.Destroy();
    }

    private static ScriptHandleResult OnEnterThorns(CallInfo info, NwGameObject casterObject, int thornAb,
        MetaMagic metaMagic, NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature targetCreature
            || casterObject is NwCreature casterCreature
            && targetCreature != casterCreature && casterCreature.IsReactionTypeFriendly(targetCreature))
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(casterObject, targetCreature, spell);

        if (targetCreature.AC - thornAb > 0) return ScriptHandleResult.Handled;

        ApplyDamage(targetCreature, metaMagic);

        ApplySlow(targetCreature, metaMagic, spell, casterObject.CasterLevel);

        return ScriptHandleResult.Handled;
    }

    private static void ApplySlow(NwCreature targetCreature, MetaMagic metaMagic, NwSpell spell, int casterLevel)
    {
        TimeSpan slowDuration = NwTimeSpan.FromRounds(casterLevel);
        if (metaMagic == MetaMagic.Extend) slowDuration *= 2;

        Effect? slowEffect = targetCreature.ActiveEffects.FirstOrDefault(e => e.Spell == spell);
        if (slowEffect != null) targetCreature.RemoveEffect(slowEffect);

        slowEffect = Effect.MovementSpeedDecrease(50);
        slowEffect.SubType = EffectSubType.Supernatural;
        targetCreature.ApplyEffect(EffectDuration.Temporary, slowEffect, slowDuration);
    }

    private static void ApplyDamage(NwCreature targetCreature, MetaMagic metaMagic)
    {
        int damageRoll = SpellUtils.MaximizeSpell(metaMagic, 6, 2);
        damageRoll = SpellUtils.EmpowerSpell(metaMagic, damageRoll);

        Effect damageEffect = Effect.Damage(damageRoll, DamageType.Piercing);
        damageEffect.SubType = EffectSubType.Supernatural;

        targetCreature.ApplyEffect(EffectDuration.Temporary, damageEffect);
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
