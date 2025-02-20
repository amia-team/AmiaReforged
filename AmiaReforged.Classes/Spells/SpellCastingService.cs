using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

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
        if (!_spellImpactHandlers.TryGetValue(callInfo.ScriptName, out ISpell? spell))
        {
            return ScriptHandleResult.NotHandled;
        }
        
        
        spell = _decoratorFactory.ApplyDecorators(spell);
        
        SpellEvents.OnSpellCast eventData = new();
        
        NwGameObject? caster = eventData.Caster;
        NwGameObject? target = eventData.TargetObject;
        
        if(caster is not NwCreature casterCreature || target is not NwCreature targetCreature)
        {
            return ScriptHandleResult.Handled;
        }

        if (casterCreature.Area?.GetObjectVariable<LocalVariableInt>("NoCasting").Value == 1)
        {
            NWScript.FloatingTextStringOnCreature("- You cannot cast magic in this area! -", casterCreature, NWScript.FALSE);
            return ScriptHandleResult.Handled;
        }

        if (!targetCreature.IsReactionTypeHostile(casterCreature))
        {
            NWScript.SendMessageToPC(casterCreature, "You cannot target a friendly creature with this spell.");
            return ScriptHandleResult.Handled;
        }
        
        spell.DoSpellResist(targetCreature, casterCreature);

        spell.OnSpellImpact(eventData);

        return ScriptHandleResult.Handled;
    }
}