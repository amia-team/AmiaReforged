using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(SpellCastingService))]
public class SpellCastingService
{
    private readonly Dictionary<string, ISpell> _spellImpactHandlers = new Dictionary<string, ISpell>();

    public SpellCastingService(ScriptHandleFactory scriptHandleFactory, IEnumerable<ISpell> spells)
    {
        foreach (ISpell spell in spells)
        {
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
        
        SpellEvents.OnSpellCast eventData = new();
        spell.OnSpellImpact(eventData);

        return ScriptHandleResult.Handled;
    }
}