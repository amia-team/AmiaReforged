using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(SpellCastingService))]
public class SpellCastingService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, ISpell> _spellImpactHandlers = new Dictionary<string, ISpell>();
    private readonly SpellDecoratorFactory _decoratorFactory;

    public SpellCastingService(ScriptHandleFactory scriptHandleFactory, SpellDecoratorFactory decoratorFactory, IEnumerable<ISpell> spells)
    {
        _decoratorFactory = decoratorFactory;
        foreach (ISpell spell in spells)
        {
            Log.Info($"Registering spell impact handler for {spell.ImpactScript}");
            _spellImpactHandlers.Add(spell.ImpactScript, spell);
            scriptHandleFactory.RegisterScriptHandler(spell.ImpactScript, HandleSpellImpact);
        }
    }

    private ScriptHandleResult HandleSpellImpact(CallInfo callInfo)
    {
        Log.Info($"Handling spell impact for {callInfo.ScriptName}");
        if (!_spellImpactHandlers.TryGetValue(callInfo.ScriptName, out ISpell? spell))
        {
            return ScriptHandleResult.NotHandled;
        }
        
        Log.Info($"Spell impact handler found for {callInfo.ScriptName}");
        
        spell = _decoratorFactory.ApplyDecorators(spell);
        
        SpellEvents.OnSpellCast eventData = new();
        
        NwGameObject? caster = eventData.Caster;
        NwGameObject? target = eventData.TargetObject;
        
        if(caster is not NwCreature casterCreature || target is not NwCreature targetCreature)
        {
            Log.Error("Caster or target is not a creature");
            return ScriptHandleResult.Handled;
        }
        
        spell.DoSpellResist(targetCreature, casterCreature);

        spell.OnSpellImpact(eventData);

        return ScriptHandleResult.Handled;
    }
}