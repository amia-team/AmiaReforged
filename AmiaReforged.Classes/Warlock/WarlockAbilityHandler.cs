using System.Collections.Concurrent;
using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockAbilityHandler))]
public class WarlockAbilityHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly ConcurrentDictionary<ushort, EssenceType> RemoveEssence = new()
    {
        [1299] = EssenceType.RemoveEssence
    };

    private static readonly ConcurrentDictionary<int, EssenceType> Essences = new()
    {
        [1015] = EssenceType.Beshadowed,
        [1016] = EssenceType.Bewitching,
        [1017] = EssenceType.Binding,
        [1018] = EssenceType.Brimstone,
        [1019] = EssenceType.Draining,
        [1020] = EssenceType.Frightful,
        [1021] = EssenceType.Hellrime,
        [1022] = EssenceType.Screaming,
        [1023] = EssenceType.Utterdark,
        [1024] = EssenceType.Vitriolic
    };

    public WarlockAbilityHandler()
    {
        NwModule.Instance.OnUseFeat += OnRemoveEldritchEssence;
        NwModule.Instance.OnSpellAction += OnEldritchEssence;
        NwModule.Instance.OnSpellAction += OnFriendlyCast;
        NwModule.Instance.OnSpellCast += OnIllegalCast;
        NwModule.Instance.OnSpellCast += OnInvocationCast;
        NwModule.Instance.OnSpellAction += OnInvocationCastAction;
        NwModule.Instance.OnSpellInterrupt += OnInvocationInterrupt;
        Log.Info(message: "Warlock Ability Script Handler initialized.");
    }

    private void OnRemoveEldritchEssence(OnUseFeat obj)
    {
        ushort featId = obj.Feat.Id;
        if (!RemoveEssence.ContainsKey(featId)) return;

        NwItem item = obj.Creature.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(item, sVarName: "warlock_essence", (ushort)RemoveEssence[featId]);

        if (obj.Creature.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage(WarlockConstants.String(message: "Eldritch Essence removed."));
    }

    private void OnEldritchEssence(OnSpellAction obj)
    {
        int spellId = obj.Spell.Id;
        if (!Essences.ContainsKey(spellId)) return;

        obj.PreventSpellCast = true;

        NwItem item = obj.Caster.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(item, sVarName: "warlock_essence", (int)Essences[spellId]);

        if (obj.Caster.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage(WarlockConstants.String($"Essence type set to {Essences[spellId].ToString()}."));
    }

    private void OnInvocationCast(OnSpellCast obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Caster) <= 0) return;

        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_EXPERTISE, NWScript.FALSE);
        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_IMPROVED_EXPERTISE, NWScript.FALSE);

        WarlockSpells.ResetWarlockInvocations(obj.Caster);
    }

    private void OnFriendlyCast(OnSpellAction obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Caster) <= 0) return;
        if (obj.TargetObject is NwCreature creature)
        {
            if (!(obj.Spell.Id == 981 || obj.Spell.Id == 982 || obj.Spell.Id == 1005)) return;

            if (!obj.Caster.IsReactionTypeFriendly(creature)) return;  
            obj.PreventSpellCast = true;
            if (obj.Caster.IsPlayerControlled(out NwPlayer? player))
                player.SendServerMessage(
                    message: "You cannot perform that action on a friendly target due to PvP settings");
            return;
        }
    }

    private void OnIllegalCast(OnSpellCast obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Caster) <= 0) return;
        if (NWScript.GetLocalInt(NWScript.GetArea(obj.Caster), sVarName: "NoCasting") == 0) return;
        if (!(obj.Spell.UserType == SpellUserType.Spells ||
              obj.Spell.UserType == SpellUserType.CreaturePower)) return;

        obj.PreventSpellCast = true;

        if (obj.Caster.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage(message: "- You cannot cast magic in this area! -");
    }

    private void OnInvocationInterrupt(OnSpellInterrupt obj)
    {
        if (NWScript.GetLevelByClass(57, obj.InterruptedCaster) <= 0) return;
        if (!obj.InterruptedCaster.IsPlayerControlled(out NwPlayer? player)) return;

        WarlockSpells.ResetWarlockInvocations(player.LoginCreature);
    }

    private void OnInvocationCastAction(OnSpellAction obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? _)) return;

        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_EXPERTISE, NWScript.FALSE);
        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_IMPROVED_EXPERTISE, NWScript.FALSE);
    }

    [ScriptHandler(scriptName: "wlk_el_blst")]
    public void OnEldritchBlasts(CallInfo info)
    {
        EldritchBlasts attack = new();
        attack.CastEldritchBlasts(info.ObjectSelf);
    }
}