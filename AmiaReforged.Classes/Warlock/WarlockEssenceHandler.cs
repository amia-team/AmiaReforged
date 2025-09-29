using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockEssenceHandler))]
public class WarlockEssenceHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly Dictionary<int, EssenceType> RemoveEssence = new()
    {
        [1299] = EssenceType.RemoveEssence
    };

    private static readonly Dictionary<int, EssenceType> Essences = new()
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

    public WarlockEssenceHandler()
    {
        NwModule.Instance.OnUseFeat += OnRemoveEldritchEssence;
        NwModule.Instance.OnSpellAction += OnEldritchEssence;
        Log.Info(message: "Warlock Essence Handler initialized.");
    }

    private void OnRemoveEldritchEssence(OnUseFeat eventData)
    {
        ushort featId = eventData.Feat.Id;
        if (!RemoveEssence.TryGetValue(featId, out EssenceType value)) return;

        NwItem pcKey = eventData.Creature.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(pcKey, sVarName: "warlock_essence", (int)value);

        if (eventData.Creature.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage(WarlockUtils.String(message: "Eldritch Essence removed."));
    }

    private void OnEldritchEssence(OnSpellAction eventData)
    {
        int spellId = eventData.Spell.Id;
        if (!Essences.TryGetValue(spellId, out EssenceType value)) return;

        eventData.PreventSpellCast = true;

        NwItem pcKey = eventData.Caster.Inventory.Items.First(i => i.Tag == "ds_pckey");
        NWScript.SetLocalInt(pcKey, sVarName: "warlock_essence", (int)value);

        if (eventData.Caster.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage(WarlockUtils.String($"Essence type set to {value.ToString()}."));
    }
}




