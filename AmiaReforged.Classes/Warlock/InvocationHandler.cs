using AmiaReforged.Classes.Warlock.Feats;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(InvocationHandler))]
public class InvocationHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string EldritchBlastSpellScript = "wlk_el_blst";

    public InvocationHandler()
    {
        NwModule.Instance.OnSpellCast += OnInvocationCast;
        NwModule.Instance.OnSpellInterrupt += OnInvocationInterrupt;
        Log.Info(message: "Invocation Handler initialized.");
    }

    private void OnInvocationCast(OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature warlock
            || eventData.Spell is not { } spell
            || !IsInvocation(warlock, eventData.ClassIndex, eventData.Spell)) return;

        ResetWarlockInvocations(warlock);

        if (warlock.CheckArcaneSpellFailure(spell)) return;

        eventData.PreventSpellCast = true;
    }

    private void OnInvocationInterrupt(OnSpellInterrupt eventData)
    {
        if (eventData.InterruptedCaster is not NwCreature warlock
            || !IsInvocation(warlock, eventData.ClassIndex, eventData.Spell)) return;

        ResetWarlockInvocations(warlock);
    }

    private static void ResetWarlockInvocations(NwCreature warlock)
    {
        const int blastShape = 1;
        CreaturePlugin.SetRemainingSpellSlots(warlock, WarlockExtensions.WarlockId, blastShape,
            CreaturePlugin.GetMaxSpellSlots(warlock, WarlockExtensions.WarlockId, blastShape));
        const int leastInvocation = 2;
        CreaturePlugin.SetRemainingSpellSlots(warlock, WarlockExtensions.WarlockId, leastInvocation,
            CreaturePlugin.GetMaxSpellSlots(warlock, WarlockExtensions.WarlockId, leastInvocation));
        const int lesserInvocation = 3;
        CreaturePlugin.SetRemainingSpellSlots(warlock, WarlockExtensions.WarlockId, lesserInvocation,
            CreaturePlugin.GetMaxSpellSlots(warlock, WarlockExtensions.WarlockId, lesserInvocation));
        const int greaterInvocation = 4;
        CreaturePlugin.SetRemainingSpellSlots(warlock, WarlockExtensions.WarlockId, greaterInvocation,
            CreaturePlugin.GetMaxSpellSlots(warlock, WarlockExtensions.WarlockId, greaterInvocation));
        const int darkInvocation = 5;
        CreaturePlugin.SetRemainingSpellSlots(warlock, WarlockExtensions.WarlockId, darkInvocation,
            CreaturePlugin.GetMaxSpellSlots(warlock, WarlockExtensions.WarlockId, darkInvocation));
    }

    private static bool IsInvocation(NwCreature warlock, int classIndex, NwSpell? spell)
    {
        // You cannot always guarantee that the object casting a spell actually has any classes.
        if (classIndex < 0 || classIndex >= warlock.Classes.Count) return false;

        return spell?.ImpactScript == EldritchBlastSpellScript
               || warlock.Classes[classIndex].Class.Id == WarlockExtensions.WarlockId;
    }
}
