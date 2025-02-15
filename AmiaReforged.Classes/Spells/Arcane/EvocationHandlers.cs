using AmiaReforged.Classes.Spells.Arcane.SecondCircle.Evocation;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane;

[ServiceBinding(typeof(EvocationHandlers))]
public class EvocationHandlers
{
    private readonly List<string> _tagsToPurge = new()
    {
        DarknessSpell.DarknessBlindTag
    };


    public EvocationHandlers()
    {
        NwModule.Instance.OnClientEnter += PurgeLingeringEffects;
    }

    /// <summary>
    /// Gets rid of effects applied as workarounds to bugs in NWN.
    /// </summary>
    /// <param name="obj"></param>
    private void PurgeLingeringEffects(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.IsDM) return;

        NwCreature? character = obj.Player.LoginCreature;
        if (character == null) return;

        List<Effect> effects =
            character.ActiveEffects.Where(e => e.Tag != null && _tagsToPurge.Contains(e.Tag)).ToList();

        foreach (Effect effect in effects)
        {
            character.RemoveEffect(effect);
        }
    }

    /// <summary>
    /// Darkness impact script handler.
    /// </summary>
    /// <param name="info"></param>
    [ScriptHandler("NW_S0_Darkness.nss")]
    public void DarknessHandler(CallInfo info)
    {
        if (info.ObjectSelf == null) return;

        IntPtr spellTargetLocation = NWScript.GetSpellTargetLocation();

        if (spellTargetLocation == IntPtr.Zero) return;

        ISpell spell = new DarknessSpell(spellTargetLocation!, info.ObjectSelf);

        spell.Trigger();
    }

    [ScriptHandler("nw_s0_darknessa")]
    public void DarknessOnEnter(CallInfo info)
    {
        NwObject? enteringObject = info.ObjectSelf;
        if (enteringObject == null) return;
        
        NwCreature? enteringCreature = enteringObject as NwCreature;
        
        if (enteringCreature == null) return;
        
        IAreaOfEffect spell = new DarknessSpell();
        
        spell.TriggerOnEnter(enteringCreature);
    }

    [ScriptHandler("nw_s0_darknessb")]
    public void DarknessOnExit(CallInfo info)
    {
        NwObject? enteringObject = info.ObjectSelf;
        if (enteringObject == null) return;
        
        NwCreature? enteringCreature = enteringObject as NwCreature;
        
        if (enteringCreature == null) return;
        
        IAreaOfEffect spell = new DarknessSpell();
        
        spell.TriggerOnExit(enteringCreature);
    }
}