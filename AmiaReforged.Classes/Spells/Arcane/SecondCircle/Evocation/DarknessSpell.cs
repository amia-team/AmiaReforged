using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Evocation;

[ServiceBinding(typeof(ISpell))]
public class DarknessSpell : ISpell
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const double OneRound = 7;
    public const string DarknessBlindTag = "DARKNESS_BLIND";
    private readonly ScriptHandleFactory _handleFactory;

    public List<NwAreaOfEffect> DarknessAreas = new();

    public DarknessSpell(ScriptHandleFactory handleFactory, SchedulerService schedulerService)
    {
        _handleFactory = handleFactory;
        NwModule.Instance.OnEffectApply += OnEffectApply;
        NwModule.Instance.OnEffectRemove += OnEffectRemove;
        
        schedulerService.ScheduleRepeating(ClearInvalidArea, TimeSpan.FromSeconds(60));
    }

    private void OnEffectApply(OnEffectApply obj)
    {
        if (obj.Object is not NwCreature c) return;

        if (obj.Effect.EffectType is not (EffectType.Ultravision or EffectType.TrueSeeing)) return;

        if (!IsInDarknessAoE(obj.Object, out NwAreaOfEffect? aoe)) return;

        // We remove the blindness effect from the creature.
        Effect? darknessBlind = c.ActiveEffects.FirstOrDefault(e => e.Tag is DarknessBlindTag);
            
        if (darknessBlind is null) return;
            
        c.RemoveEffect(darknessBlind);
    }

    private void ClearInvalidArea()
    {
        foreach (NwAreaOfEffect area in DarknessAreas.ToList().Where(area => !area.IsValid))
        {
            DarknessAreas.Remove(area);
        }
    }

    private void OnEffectRemove(OnEffectRemove obj)
    {
        if (obj.Object is not NwCreature c) return;

        if (obj.Effect.EffectType is not (EffectType.Ultravision or EffectType.TrueSeeing)) return;

        if (IsInDarknessAoE(obj.Object, out NwAreaOfEffect? aoe))
        {
            c.ApplyEffect(EffectDuration.Temporary, DarknessBlind(), TimeSpan.FromSeconds(OneRound));
        }
    }

    public void SetResult(ResistSpellResult result)
    {
        Result = result;
    }

    private bool IsInDarknessAoE(NwObject objObject, out NwAreaOfEffect? aoe)
    {
        aoe = DarknessAreas.FirstOrDefault(darknessArea =>
            darknessArea.GetObjectsInEffectArea<NwCreature>().Contains(objObject));

        return aoe != null;
    }


    private static Effect DarknessBlind()
    {
        Effect blind = Effect.Blindness();
        
        blind.DurationType = EffectDuration.Temporary;
        blind.Tag = DarknessBlindTag;
        blind.IgnoreImmunity = true;
        blind.SubType = EffectSubType.Supernatural;
        
        return blind;
    }


    public ResistSpellResult Result { get; set; }
    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public string ImpactScript => "NW_S0_Darkness";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;

        if (caster is null)
        {
            Log.Error("Spell was cast, but the caster was not found?");
            return;
        }


        PersistentVfxTableEntry? darknessTableEntry = PersistentVfxType.PerDarkness;
        if (darknessTableEntry is null)
        {
            Log.Error($"Invalid entry value for spell: {eventData.Spell.Name}");
            return;
        }

        Effect darkness = Effect.AreaOfEffect(darknessTableEntry, _handleFactory.CreateUniqueHandler(OnEnterDarkness),
            _handleFactory.CreateUniqueHandler(OnHeartbeatDarkness),
            _handleFactory.CreateUniqueHandler(OnExitDarkness));

        Location? location = eventData.TargetLocation;

        if (location is null)
        {
            Log.Error($"Invalid target");

            if (eventData.Caster.IsPlayerControlled(out NwPlayer? player))
            {
                player.FloatingTextString("Invalid target", false);
            }

            return;
        }

        float dur = NWScript.RoundsToSeconds(caster.CasterLevel);
        location.ApplyEffect(EffectDuration.Temporary, darkness, TimeSpan.FromSeconds(dur));

        NwAreaOfEffect? darknessAoE = location.GetNearestObjectsByType<NwAreaOfEffect>().FirstOrDefault();

        if (darknessAoE is null)
        {
            Log.Error("Failed to create darkness AoE");
            return;
        }

        DarknessAreas.Add(darknessAoE);
        
    }

    private ScriptHandleResult OnEnterDarkness(CallInfo arg)
    {
        AreaOfEffectEvents.OnEnter eventData = new();
        NwGameObject enteringObject = eventData.Entering;

        Effect darknessBlindness = DarknessBlind();
        Effect invisibility = DarknessInvis();
        enteringObject.ApplyEffect(EffectDuration.Temporary, invisibility, TimeSpan.FromSeconds(OneRound));
        
        if (!ImmuneToDarkness(enteringObject))
        {
            enteringObject.ApplyEffect(EffectDuration.Temporary, darknessBlindness, TimeSpan.FromSeconds(OneRound));
        }

        return ScriptHandleResult.Handled;
    }

    private static bool ImmuneToDarkness(NwGameObject enteringObject) =>
        enteringObject.ActiveEffects.Any(e => e.EffectType is EffectType.Ultravision or EffectType.TrueSeeing);

    private ScriptHandleResult OnHeartbeatDarkness(CallInfo arg)
    {
        AreaOfEffectEvents.OnHeartbeat eventData = new();

        NwGameObject effect = eventData.Effect;

        if (effect is not NwAreaOfEffect e)
        {
            return ScriptHandleResult.Handled;
        }
        
        List<NwCreature> creatures = e.GetObjectsInEffectArea<NwCreature>().ToList();

        foreach (NwCreature creature in creatures)
        {
            Effect? darknessBlind = creature.ActiveEffects.FirstOrDefault(eff => eff.Tag is DarknessBlindTag);
            Effect? darknessInvis = creature.ActiveEffects.FirstOrDefault(eff => eff.Tag is DarknessInvisTag);
            
            if (ImmuneToDarkness(creature))
            {
                if (darknessBlind is null) continue;
                creature.RemoveEffect(darknessBlind);
            }
            else
            {
                // Remove the effect if it exists.
                if (darknessBlind is not null) creature.RemoveEffect(darknessBlind);
                
                creature.ApplyEffect(EffectDuration.Temporary, DarknessBlind(), TimeSpan.FromSeconds(OneRound));
            }
            
            if(darknessInvis is not null) creature.RemoveEffect(darknessInvis);
            creature.ApplyEffect(EffectDuration.Temporary, DarknessInvis(), TimeSpan.FromSeconds(OneRound));

        }
        
        return ScriptHandleResult.Handled;
    }

    private Effect DarknessInvis()
    {
        Effect invis = Effect.Invisibility(InvisibilityType.Darkness);
        
        invis.DurationType = EffectDuration.Temporary;
        invis.Tag = DarknessInvisTag;
        invis.IgnoreImmunity = true;
        invis.SubType = EffectSubType.Magical;
        
        return invis;
    }

    private const string DarknessInvisTag = "DARKNESS_INVIS";

    private ScriptHandleResult OnExitDarkness(CallInfo arg)
    {
        AreaOfEffectEvents.OnExit eventData = new();

        NwGameObject exitingObject = eventData.Exiting;
        
        Effect? darknessInvis = exitingObject.ActiveEffects.FirstOrDefault(e => e.Tag is DarknessInvisTag);
        
        if (darknessInvis is not null)
        {
            exitingObject.RemoveEffect(darknessInvis);            
        }

        Effect? darknessBlind = exitingObject.ActiveEffects.FirstOrDefault(e => e.Tag is DarknessBlindTag);
        if (darknessBlind is null)
        {
            // Exit early, don't need to do anything.
            return ScriptHandleResult.Handled;
        }

        exitingObject.RemoveEffect(darknessBlind);

        return ScriptHandleResult.Handled;
    }
}