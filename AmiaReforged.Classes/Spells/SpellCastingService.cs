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
    private const string WelcomeAreaResRef = "welcometotheeete";
    private const string UniquePowerScriptName = "NW_S3_ActItem01";
    private const string RecallResRef = "recall";
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

        NwModule.Instance.OnSpellCast += PreventRestrictedCasts;
    }

    private void PreventRestrictedCasts(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (obj.Spell is null) return;
        if (obj.Caster is not NwCreature caster) return;

        // Don't restrict raise dead and resurrection in the "Welcome to Amia!" area
        bool isWelcomeArea = caster.Area?.ResRef == WelcomeAreaResRef;

        if (isWelcomeArea && obj.Spell.SpellType is Spell.RaiseDead or Spell.Resurrection)
            return;

        // Don't restrict DM avatars, let the hellballs rolL! DM possessed NPCs are still restricted.
        if (caster.IsDMAvatar) return;

        // Don't restrict items that use Unique Power or Unique Power Self unless it's a recall stone
        if (obj.Item is not null && obj.Spell.ImpactScript == UniquePowerScriptName &&
            !obj.Item.ResRef.Contains(RecallResRef))
            return;

        // Restrict casting in no casting areas
        bool isNoCastingArea = caster.Area?.GetObjectVariable<LocalVariableInt>(name: "NoCasting").Value == 1;

        if (isNoCastingArea)
        {
            player.FloatingTextString("- You cannot cast magic in this area! -", false);

            obj.PreventSpellCast = true;
            return;
        }

        // Restrict hostile spellcasting in no PvP areas
        if (caster.Area?.PVPSetting != PVPSetting.None || !obj.Spell.IsHostileSpell || obj.TargetObject == caster ||
            !obj.TargetObject.IsPlayerControlled(out NwPlayer? _)) return;
        
        player.SendServerMessage("PVP is not allowed in this area.");
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
            DoCasterLevelOverride(casterCreature, eventData);

            spell.OnSpellImpact(eventData);

            RevertCasterLevelOverride(casterCreature);
            return ScriptHandleResult.Handled;
        }

        if (casterCreature is { IsLoginPlayerCharacter: true, IsDMAvatar: false } 
            && target is NwCreature targetCreature 
            && eventData.Spell.IsHostileSpell)
        {
            bool targetIsInParty = casterCreature.Faction.GetMembers().Any(member => member == targetCreature);
            
            if (targetIsInParty == false)
            {
                spell.DoSpellResist(targetCreature, casterCreature);
                CreatureEvents.OnSpellCastAt.Signal(caster, targetCreature, eventData.Spell);
            }
        }

        if (target is NwItem targetItem && eventData.Item is null)
        {
            CraftSpell craftSpell = new(eventData, targetItem); 
            craftSpell.DoCraftSpell();
        }
            

        DoCasterLevelOverride(casterCreature, eventData);

        spell.OnSpellImpact(eventData);

        RevertCasterLevelOverride(casterCreature);

        spell.CheckedSpellResistance = false;

        return ScriptHandleResult.Handled;
    }

    private void DoCasterLevelOverride(NwCreature casterCreature, SpellEvents.OnSpellCast eventData)
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

        int pmLevelMod = eventData.Spell.SpellSchool == SpellSchool.Necromancy
            ? paleMaster.Level
            : Math.Clamp(paleMaster.Level - 6, 0, paleMaster.Level); // Prevent negative integers
        
        int levels = paleMaster.Level + baseClassLevels;
        CreaturePlugin.SetCasterLevelOverride(casterCreature, NWScript.CLASS_TYPE_PALE_MASTER, levels);
    }

    private void RevertCasterLevelOverride(NwCreature casterCreature)
    {
    }
}