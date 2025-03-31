using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(SpellCastingService))]
public class SpellCastingService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly SpellDecoratorFactory _decoratorFactory;
    private readonly Dictionary<string, ISpell> _spellImpactHandlers = new();

    public SpellCastingService(ScriptHandleFactory scriptHandleFactory, SpellDecoratorFactory decoratorFactory,
        IEnumerable<ISpell> spells)
    {
        _decoratorFactory = decoratorFactory;
        foreach (ISpell spell in spells)
        {
            Log.Info($"Registering spell for script: {spell.ImpactScript}");
            _spellImpactHandlers.Add(spell.ImpactScript, spell);
            scriptHandleFactory.RegisterScriptHandler(spell.ImpactScript, HandleSpellImpact);
        }

        NwModule.Instance.OnSpellCast += PreventRestricedCasts;
    }

    private void PreventRestricedCasts(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (obj.Spell is null) return;
        NwCreature? character = player.LoginCreature;

        if (character is null) return;
        
        // Don't restrict raise dead and resurrection in the "Welcome to Amia!" area
        if (character.Area?.ResRef == "welcometotheeete" &&
            obj.Spell.SpellType is Spell.RaiseDead or Spell.Resurrection)
            return;

        if (character.Area?.GetObjectVariable<LocalVariableInt>(name: "NoCasting").Value == 1 && obj.Item is null)
        {
            NWScript.FloatingTextStringOnCreature(sStringToDisplay: "- You cannot cast magic in this area! -",
                character,
                NWScript.FALSE);

            obj.PreventSpellCast = true;
            return;
        }

        if (obj.Item is not null && obj.Spell.IsHostileSpell && character.Area?.GetObjectVariable<LocalVariableInt>(name: "NoCasting").Value == 1)
        {
            NWScript.SendMessageToPC(character, szMessage: "You cannot use that item in this area.");
            obj.PreventSpellCast = true;
            return;
        }

        if (obj.TargetObject is null || obj.TargetObject == obj.Caster) return;

        if (!obj.TargetObject.IsPlayerControlled(out NwPlayer? nwPlayer)) return;
        if (character.Area?.PVPSetting != PVPSetting.None) return;
        NWScript.SendMessageToPC(character, szMessage: "PVP is not allowed in this area.");
        obj.PreventSpellCast = true;
    }

    private ScriptHandleResult HandleSpellImpact(CallInfo callInfo)
    {
        if (!_spellImpactHandlers.TryGetValue(callInfo.ScriptName, out ISpell? spell))
            return ScriptHandleResult.NotHandled;


        spell = _decoratorFactory.ApplyDecorators(spell);

        SpellEvents.OnSpellCast eventData = new();

        NwGameObject? caster = eventData.Caster;
        NwGameObject? target = eventData.TargetObject;

        if (caster is not NwCreature casterCreature) return ScriptHandleResult.Handled;

        if (target is null)
        {
            DoCasterLevelOverride(casterCreature);

            spell.OnSpellImpact(eventData);

            RevertCasterLevelOverride(casterCreature);
            return ScriptHandleResult.Handled;
        }
        
        if (target is NwCreature targetCreature)
        {
            bool targetIsInParty = false;

            if (casterCreature.IsPlayerControlled(out NwPlayer? player))
                targetIsInParty = player.PartyMembers.Any(p => p.LoginCreature == targetCreature) ||
                                  casterCreature.Associates.Any(a => a == targetCreature);

            PVPSetting? areaPvpSetting = casterCreature.Area?.PVPSetting;

            spell.DoSpellResist(targetCreature, casterCreature);


            if (targetIsInParty)
            {
                NWScript.SendMessageToPC(casterCreature,
                    szMessage: "You cannot target a friendly creature with this spell.");
                return ScriptHandleResult.Handled;
            }

            if (eventData.Spell.IsHostileSpell)
                if (!targetCreature.PlotFlag || !targetCreature.Immortal)
                    NWScript.AdjustReputation(caster, target, -100);

            spell.DoSpellResist(targetCreature, casterCreature);
        }

        DoCasterLevelOverride(casterCreature);

        spell.OnSpellImpact(eventData);

        RevertCasterLevelOverride(casterCreature);

        spell.CheckedSpellResistance = false;

        return ScriptHandleResult.Handled;
    }

    private void DoCasterLevelOverride(NwCreature casterCreature)
    {
        CreatureClassInfo? paleMaster =
            casterCreature.Classes.FirstOrDefault(c => c.Class.ClassType == ClassType.PaleMaster);
        if (paleMaster is null) return;

        int baseClassLevels = 0;
        foreach (CreatureClassInfo charClass in casterCreature.Classes)
        {
            if (charClass.Class.ClassType is ClassType.Bard or ClassType.Assassin or ClassType.Wizard
                or ClassType.Sorcerer)
                baseClassLevels += charClass.Level;
        }

        int levels = paleMaster.Level + baseClassLevels;
        CreaturePlugin.SetCasterLevelOverride(casterCreature, NWScript.CLASS_TYPE_PALE_MASTER, levels);
    }

    private void RevertCasterLevelOverride(NwCreature casterCreature)
    {
    }
}