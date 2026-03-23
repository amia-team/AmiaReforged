using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells;

[ServiceBinding(typeof(InfiniteCantripService))]
public class InfiniteCantripService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

        // Get the actual spell level (use MasterSpell if available, as spells can have different levels per class)
        byte spellLevel = obj.Spell.MasterSpell?.InnateSpellLevel ?? obj.Spell.InnateSpellLevel;



        // Always restore level 0 spells (cantrips)
        if (spellLevel == 0)
        {
            // Delay restoration to ensure spell slot is consumed first
            _ = NwTask.Run(async () =>
            {
                await NwTask.Delay(TimeSpan.FromMilliseconds(100));
                player.LoginCreature.RestoreSpells(0);
            });
            return;
        }

        // For level 1 spells, check if the CASTING CLASS's effective caster level is >= 20.
        // This matters for Blackguard (own spellbook CL = BG level, 1:1) vs. its stacking
        // contribution to Cleric/Druid/Ranger (class level - 5).
        if (spellLevel == 1)
        {
            NwCreature creature = player.LoginCreature;

            // Determine which class is actually casting this spell
            if (obj.ClassIndex < 0 || obj.ClassIndex >= creature.Classes.Count) return;
            ClassType castingClass = creature.Classes[obj.ClassIndex].Class.ClassType;

            int effectiveCasterLevel =
                EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(creature, castingClass);

            if (effectiveCasterLevel >= 20)
            {
                // Delay restoration to ensure spell slot is consumed first
                _ = NwTask.Run(async () =>
                {
                    await NwTask.Delay(TimeSpan.FromMilliseconds(100));
                    player.LoginCreature.RestoreSpells(1);
                });
            }
        }
    }
}
