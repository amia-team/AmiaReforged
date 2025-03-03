using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NLog.Fluent;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Services.Defender;

[ServiceBinding(typeof(DefensiveStance))]
public class DefensiveStance
{
    private readonly EventService _eventService;

    private const int EventsDefensiveStanceConst = 11;
    private const string CombatModeId = "COMBAT_MODE_ID";
    private const string DefensiveStanceEffectTag = "DEFENIVE_STANCE";
    private const string DefensiveStanceVar = "ApplyingDefensiveStance";
    
    private Dictionary<NwPlayer, bool> _currentlyBeingApplied = new();

    public DefensiveStance(EventService eventService)
    {
        _eventService = eventService;
    
        EventsPlugin.SubscribeEvent(EventsPlugin.NWNX_ON_COMBAT_MODE_OFF, "stance_defdr_off");
        EventsPlugin.SubscribeEvent(EventsPlugin.NWNX_ON_COMBAT_MODE_ON, "stance_defdr_on");
        
        NwModule.Instance.OnClientEnter += ClearDefensiveStance;
    }

    private void ClearDefensiveStance(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature == null)
        {
            LogManager.GetCurrentClassLogger().Info("Could not find login creature.");
            return;
        }
        
        Effect? defensiveEffect = obj.Player.LoginCreature.ActiveEffects.FirstOrDefault(e => e.Tag == DefensiveStanceEffectTag);
        if (defensiveEffect == null) return;
        
        obj.Player.LoginCreature.RemoveEffect(defensiveEffect);
    }

    [ScriptHandler("stance_defdr_on")]
    public void DefensiveStanceOn(CallInfo script)
    {
        if (!script.ObjectSelf.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        string data = EventsPlugin.GetEventData(CombatModeId);
        int combatMode = int.Parse(data);

        if (combatMode != EventsDefensiveStanceConst)
        {
            return;
        }
        
        bool isAlreadyBeingApplied = _currentlyBeingApplied.ContainsKey(player);
        
        if (isAlreadyBeingApplied)
        {
            return;
        }
        
        _currentlyBeingApplied.Add(player, true);
        // You cannot always guarantee that their character is in a valid state (ie they crashed after activating it).
        NwCreature? character = player.LoginCreature;
        if (character == null) return;
        Effect? defensiveEffect = character.ActiveEffects.FirstOrDefault(e => e.Tag == DefensiveStanceEffectTag);
        
        if(defensiveEffect != null) return;
        
        if (NWScript.GetLocalInt(character, DefensiveStanceVar) == 1)
        {
            return;
        }
        
        NWScript.SetLocalInt(character, DefensiveStanceVar, 1);
        
        player.FloatingTextString("*Squares up their stance.*");
        
        int defenderLevel = character.Classes.FirstOrDefault(c => c.Class.ClassType == ClassType.DwarvenDefender)?.Level ?? 0;

        // If the character has at least 20 constitution and is an epic defender, they gain 1/4 of their constitution modifier as an attack bonus.
        // Otherwise, they gain no attack bonus.
        bool hasEnoughCon = character.GetRawAbilityScore(Ability.Constitution) >= 20;
        bool isAnEpicDefender = defenderLevel >= 11;
        
        int cap = defenderLevel >= 20 ? 5 : 4;
        int abIncrease = Math.Clamp(character.GetAbilityModifier(Ability.Constitution) / 4, 1, cap);
        int ab = hasEnoughCon && isAnEpicDefender ? abIncrease : 0;
        Effect attackBonus = Effect.AttackIncrease(ab);
        
        // Applies the strength bonus, inclusive of capstone bonus... ie 1 + 20/5 = 5.
        int str = 1 + defenderLevel / 5;
        Effect strengthBonus = Effect.AbilityIncrease(Ability.Strength, str);

        // Clamps the base temporary hit points between 10 and 30, then applies the capstone bonus if relevant.
        int baseTempHp = Math.Clamp(10 + defenderLevel / 7 * 10, 10, 30);
        int capstoneBonus = defenderLevel >= 20 ? 10 : 0;
        int tempHp = baseTempHp + capstoneBonus;
        Effect tempHpBonus = Effect.TemporaryHitpoints(tempHp);
        
        // Resistance bonus.
        int resistanceCap = defenderLevel >= 20 ? 7 : 5;
        int resistanceCapstone = defenderLevel >= 20 ? 2 : 0;
        int resistance = Math.Clamp(1 + defenderLevel / 4 + resistanceCapstone, 0, resistanceCap);
        Effect savingThrowBonus = Effect.SavingThrowIncrease(SavingThrow.All, resistance);
        
        int acCapstone = defenderLevel >= 20 ? 1 : 0;
        int ac = 1 + defenderLevel / 5 + acCapstone;
        Effect acBonus = Effect.ACIncrease(ac);

        // Link the effects so they are all joined together.
        Effect defensiveStance = Effect.LinkEffects(attackBonus, strengthBonus);
        defensiveStance = Effect.LinkEffects(defensiveStance, tempHpBonus);
        defensiveStance = Effect.LinkEffects(defensiveStance, savingThrowBonus);
        defensiveStance = Effect.LinkEffects(defensiveStance, acBonus);
        
        // Tag it and make it undispellable.
        defensiveStance.Tag = DefensiveStanceEffectTag;
        defensiveStance.SubType = EffectSubType.Supernatural;
        
        // Apply it to the character.
        _currentlyBeingApplied.Remove(player);
        character.ApplyEffect(EffectDuration.Permanent, defensiveStance);
    }

    [ScriptHandler("stance_defdr_off")]
    public void DefensiveStanceOff(CallInfo script)
    {
        if (!script.ObjectSelf.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        string data = EventsPlugin.GetEventData(CombatModeId);
        int combatMode = int.Parse(data);

        if (combatMode != EventsDefensiveStanceConst)
        {
            return;
        }
        
        
        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        Effect? defensiveEffect = character.ActiveEffects.FirstOrDefault(e => e.Tag == DefensiveStanceEffectTag);
        if (defensiveEffect == null) return;
        
        character.RemoveEffect(defensiveEffect);
        
        player.FloatingTextString("*Lets up their guard.*");
    }
}