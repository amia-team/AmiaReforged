using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.Percussion;

[ServiceBinding(typeof(ISpell))]
public class Percussion : ISpell
{
    private readonly ScriptHandleFactory _handleFactory;
    private readonly SchedulerService _scheduler;

    public Percussion(ScriptHandleFactory handleFactory, SchedulerService scheduler)
    {
        _scheduler = scheduler;
        _handleFactory = handleFactory;
    }

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "am_s_percussion";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster.Location == null) return;

        // Just plays a single looping drum for now.
        string soundTag = $"Percussion_{caster.Name}";
        NwSound? existingSound = caster.GetNearestObjectsByType<NwSound>().FirstOrDefault(s => s.Tag == soundTag);
        if (existingSound != null) existingSound.Destroy();

        NwSound? sound = NwSound.Create(template: "wardrum1", caster.Location);
        if (sound == null) return;
        sound.Tag = soundTag;
        Effect aoe = Effect.AreaOfEffect(PersistentVfxType.MobCircchaos!,
            _handleFactory.CreateUniqueHandler(OnEnterPercussion),
            _handleFactory.CreateUniqueHandler(HeartbeatPercussion),
            _handleFactory.CreateUniqueHandler(OnExitPercussion));
        aoe.Tag = soundTag;
        int roundsToSeconds = caster.CasterLevel * 6;

        // Applies the effect at the caster's position.
        caster.Location.ApplyEffect(EffectDuration.Temporary, aoe, TimeSpan.FromSeconds(roundsToSeconds));
        NwGameObject? createdArea = caster.GetNearestObjectsByType<NwAreaOfEffect>()
            .SingleOrDefault(a => a.Tag == soundTag);

        // Destroys the sound after the effect ends.
        _scheduler.Schedule(() => sound.Destroy(), TimeSpan.FromSeconds(roundsToSeconds));
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    private ScriptHandleResult OnEnterPercussion(CallInfo arg)
    {
        AreaOfEffectEvents.OnEnter eventData = new();
        eventData.Entering.SpeakString(message: "You hear a drumming sound.");
        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult HeartbeatPercussion(CallInfo arg)
    {
        AreaOfEffectEvents.OnHeartbeat eventData = new();
        if (eventData.Effect.Creator is NwCreature creator)
            foreach (NwCreature nwCreature in eventData.Effect.GetNearestObjectsByType<NwCreature>()
                         .Where(c => c.Distance(eventData.Effect) <= 5))
            {
                if (nwCreature.IsFriend(creator) || nwCreature == creator)
                    nwCreature.ApplyEffect(EffectDuration.Temporary,
                        Effect.SkillIncrease(NwSkill.FromSkillType(Skill.Perform)!, 2), TimeSpan.FromSeconds(6));
            }

        return ScriptHandleResult.Handled;
    }

    private ScriptHandleResult OnExitPercussion(CallInfo arg)
    {
        AreaOfEffectEvents.OnExit eventData = new();
        eventData.Exiting.SpeakString(message: "The drumming sound fades away.");
        return ScriptHandleResult.Handled;
    }
}