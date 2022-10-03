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

    private static readonly ConcurrentDictionary<ushort, EssenceType> Essences = new()
    {
        [1278] = EssenceType.Frightful,
        [1279] = EssenceType.Vitriolic,
        [1280] = EssenceType.Brimstone,
        [1281] = EssenceType.Utterdark,
        [1282] = EssenceType.Draining,
        [1292] = EssenceType.Hellrime,
        [1293] = EssenceType.Beshadowed,
        [1294] = EssenceType.Binding,
        [1295] = EssenceType.Bewitching,
        [1296] = EssenceType.Hindering,
        [1299] = EssenceType.NoEssence
    };

    private static readonly List<ushort> MobilityFeats = new()
    {
        1318
    };

    public WarlockAbilityHandler()
    {
        NwModule.Instance.OnUseFeat += OnEldritchEssence;
        NwModule.Instance.OnSpellCast += OnInvocationCast;
        NwModule.Instance.OnSpellAction += OnInvocationCastAction;
        NwModule.Instance.OnSpellInterrupt += OnInvocationInterrupt;
        NwModule.Instance.OnClientEnter += GivePactFeats;
        Log.Info("Warlock Ability Script Handler initialized.");
    }

    private void GivePactFeats(ModuleEvents.OnClientEnter obj)
    {
        if (obj.Player.LoginCreature.Feats.Any(f => f.Id == 1310))
            obj.Player.LoginCreature.AddFeat(NwFeat.FromFeatId(1314), 1);
    }

    private void OnEldritchEssence(OnUseFeat obj)
    {
        ushort featId = obj.Feat.Id;
        if (!Essences.ContainsKey(featId)) return;

        NwItem item = obj.Creature.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(item, "warlock_essence", (int)Essences[featId]);

        if (obj.Creature.IsPlayerControlled(out NwPlayer player))
            player.SendServerMessage($"Essence type set to {Essences[featId].ToString()}");
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
    public void OnEldritchAttack(CallInfo info)
    {
        EldritchAttack attack = new();
        attack.Run(info.ObjectSelf);
    }

    [ScriptHandler("wlk_fiendresil")]
    public void OnFiendResil(CallInfo info)
    {
        FiendishResilience script = new();
        script.Run(info.ObjectSelf);
    }

    //
    // [ScriptHandler("wlk_mobility")]
    // public void OnWarlockMobility(CallInfo info)
    // {
    //     if (!info.ObjectSelf.IsPlayerControlled(out NwPlayer player)) return;
    //
    //     IMobilityStrategy script = MobilityStrategyFactory.CreateMobilityStrategy(NWScript.GetSpellId());
    //     script.Move(player.LoginCreature, NWScript.GetSpellTargetLocation());
    // }
}