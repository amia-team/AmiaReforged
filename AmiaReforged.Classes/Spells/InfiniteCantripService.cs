using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(InfiniteCantripService))]
public class InfiniteCantripService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Dictionary mapping prestige classes to their caster level modifier formulas
    private readonly Dictionary<ClassType, Func<int, int>> _prestigeClassModifiers = new()
    {
        { ClassType.PaleMaster, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.DragonDisciple, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.Blackguard, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.DivineChampion, prcLevel => Math.Max(0, prcLevel) },
        { ClassType.ArcaneArcher, prcLevel => Math.Max(0, prcLevel / 2) }
    };

    // Base caster classes that can receive prestige class bonuses
    private static readonly HashSet<ClassType> BaseCasterClasses = new()
    {
        ClassType.Wizard,
        ClassType.Sorcerer,
        ClassType.Bard,
        ClassType.Assassin,
        ClassType.Druid,
        ClassType.Cleric,
        ClassType.Ranger,
        ClassType.Paladin
    };

    public InfiniteCantripService(EventService eventService)
    {
        Log.Info(message: "Infinite Cantrip Service initialized.");

        Action<OnSpellCast> onSpellCast = HandleInfiniteCantrip;

        eventService.SubscribeAll<OnSpellCast, OnSpellCast.Factory>(onSpellCast, EventCallbackType.After);
    }

    private void HandleInfiniteCantrip(OnSpellCast obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (obj.Spell is null) return;
        if (player.LoginCreature is null) return;

        // Always restore level 0 spells (cantrips)
        if (obj.Spell.InnateSpellLevel == 0)
        {
            player.LoginCreature.RestoreSpells(0);
            return;
        }

        // For level 1 spells, check if effective caster level is >= 20
        if (obj.Spell.InnateSpellLevel == 1)
        {
            int effectiveCasterLevel = GetEffectiveCasterLevel(player.LoginCreature);

            if (effectiveCasterLevel >= 20)
            {
                player.LoginCreature.RestoreSpells(1);
                Log.Debug($"{player.LoginCreature.Name}: Restored level 1 spells (effective caster level: {effectiveCasterLevel})");
            }
        }
    }

    private int GetEffectiveCasterLevel(NwCreature creature)
    {
        // Find all prestige classes that have caster level modifiers
        int totalPrestigeModifier = 0;
        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            if (_prestigeClassModifiers.ContainsKey(charClass.Class.ClassType))
            {
                int modifier = _prestigeClassModifiers[charClass.Class.ClassType](charClass.Level);
                totalPrestigeModifier += modifier;
            }
        }

        // Find the highest level base caster class
        int highestBaseCasterLevel = 0;
        foreach (CreatureClassInfo charClass in creature.Classes)
        {
            if (BaseCasterClasses.Contains(charClass.Class.ClassType))
            {
                if (charClass.Level > highestBaseCasterLevel)
                {
                    highestBaseCasterLevel = charClass.Level;
                }
            }
        }

        // Return base level + prestige modifiers
        return highestBaseCasterLevel + totalPrestigeModifier;
    }
}
