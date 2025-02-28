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
    private readonly Dictionary<string, ISpell> _spellImpactHandlers = new();
    private readonly SpellDecoratorFactory _decoratorFactory;

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

        if (caster is not NwCreature casterCreature)
        {
            Log.Info($"Caster for {spell.ImpactScript} is not a creature");
            return ScriptHandleResult.Handled;
        }

        if (target is null)
        {
            // This is an AOE
            if (casterCreature.Area?.GetObjectVariable<LocalVariableInt>("NoCasting").Value == 1)
            {
                NWScript.FloatingTextStringOnCreature("- You cannot cast magic in this area! -", casterCreature,
                    NWScript.FALSE);
                return ScriptHandleResult.Handled;
            }

            DoCasterLevelOverride(casterCreature, eventData.Spell.SpellSchool);

            spell.OnSpellImpact(eventData);

            RevertCasterLevelOverride(casterCreature);
            return ScriptHandleResult.Handled;
        }

        if (casterCreature.Area?.GetObjectVariable<LocalVariableInt>("NoCasting").Value == 1)
        {
            NWScript.FloatingTextStringOnCreature("- You cannot cast magic in this area! -", casterCreature,
                NWScript.FALSE);
            return ScriptHandleResult.Handled;
        }


        if (target is NwCreature targetCreature)
        {
            bool targetIsInParty = false;

            if (casterCreature.IsPlayerControlled(out NwPlayer? player))
            {
                targetIsInParty = player.PartyMembers.Any(p => p.LoginCreature == targetCreature) ||
                                  casterCreature.Associates.Any(a => a == targetCreature);
            }

            PVPSetting? areaPvpSetting = casterCreature.Area?.PVPSetting;

            spell.DoSpellResist(targetCreature, casterCreature);


            if (targetIsInParty)
            {
                NWScript.SendMessageToPC(casterCreature, "You cannot target a friendly creature with this spell.");
                return ScriptHandleResult.Handled;
            }

            if (targetCreature.IsPlayerControlled && areaPvpSetting == PVPSetting.None)
            {
                NWScript.SendMessageToPC(casterCreature, "PVP is not allowed in this area.");
                return ScriptHandleResult.Handled;
            }

            if (eventData.Spell.IsHostileSpell)
            {
                if (!targetCreature.PlotFlag || !targetCreature.Immortal)
                    NWScript.AdjustReputation(caster, target, -100);
            }

            spell.DoSpellResist(targetCreature, casterCreature);
        }

        DoCasterLevelOverride(casterCreature, eventData.Spell.SpellSchool);

        spell.OnSpellImpact(eventData);

        RevertCasterLevelOverride(casterCreature);

        return ScriptHandleResult.Handled;
    }

    private void DoCasterLevelOverride(NwCreature casterCreature, SpellSchool spellSpellSchool)
    {
        CreatureClassInfo? paleMaster =
            casterCreature.Classes.FirstOrDefault(c => c.Class.ClassType == ClassType.PaleMaster);
        if (paleMaster is null) return;

        int baseClassLevels = 0;
        foreach (CreatureClassInfo charClass in casterCreature.Classes)
        {
            if (charClass.Class.ClassType is ClassType.Bard or ClassType.Assassin or ClassType.Wizard
                or ClassType.Sorcerer)
            {
                baseClassLevels += charClass.Level;
            }
        }

        int levels = paleMaster.Level + baseClassLevels;
        CreaturePlugin.SetCasterLevelOverride(casterCreature, levels, 0);
    }

    private void RevertCasterLevelOverride(NwCreature casterCreature)
    {
    }
}