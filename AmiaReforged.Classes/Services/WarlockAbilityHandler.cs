using System.Collections.Concurrent;
using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Services;

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
        [1024] = EssenceType.Vitriolic,
    };

    public WarlockAbilityHandler()
    {
        NwModule.Instance.OnUseFeat += OnRemoveEldritchEssence;
        NwModule.Instance.OnSpellAction += OnEldritchEssence;
        NwModule.Instance.OnSpellCast += OnInvocationCast;
        NwModule.Instance.OnSpellAction += OnInvocationCastAction;
        NwModule.Instance.OnSpellInterrupt += OnInvocationInterrupt;
        // NwModule.Instance.OnClientEnter += GivePactFeats;
        Log.Info("Warlock Ability Script Handler initialized.");
    }

    private void OnRemoveEldritchEssence(OnUseFeat obj)
    {
        ushort featId = obj.Feat.Id;
        if (!RemoveEssence.ContainsKey(featId)) return;

        NwItem item = obj.Creature.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(item, "warlock_essence", (ushort)RemoveEssence[featId]);

        if (obj.Creature.IsPlayerControlled(out NwPlayer player)) player.SendServerMessage(NwEffects.WarlockString("Eldritch Essence removed."));
    }
    private void OnEldritchEssence(OnSpellAction obj)
    {
        int spellId = obj.Spell.Id;
        if (!Essences.ContainsKey(spellId)) return;

        obj.PreventSpellCast = true;

        NwItem item = obj.Caster.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(item, "warlock_essence", (int)Essences[spellId]);

        if (obj.Caster.IsPlayerControlled(out NwPlayer player))
        player.SendServerMessage(NwEffects.WarlockString($"Essence type set to {Essences[spellId].ToString()}."));
    }

    private void OnInvocationCast(OnSpellCast obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Caster) <= 0) return;

        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_EXPERTISE, NWScript.FALSE);
        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_IMPROVED_EXPERTISE, NWScript.FALSE);

        WarlockSpells.ResetWarlockInvocations(obj.Caster);
    }

    private void OnInvocationInterrupt(OnSpellInterrupt obj)
    {
        if (NWScript.GetLevelByClass(57, obj.InterruptedCaster) <= 0) return;
        if (!obj.InterruptedCaster.IsPlayerControlled(out NwPlayer player)) return;

        WarlockSpells.ResetWarlockInvocations(player.LoginCreature);
    }

    private void OnInvocationCastAction(OnSpellAction obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer _)) return;

        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_EXPERTISE, NWScript.FALSE);
        NWScript.SetActionMode(obj.Caster, NWScript.ACTION_MODE_IMPROVED_EXPERTISE, NWScript.FALSE);
    }

    [ScriptHandler("wlk_el_blst")]
    public void OnEldritchBlasts(CallInfo info)
    {
        EldritchBlasts attack = new();
        attack.CastEldritchBlasts(info.ObjectSelf);
    }
}