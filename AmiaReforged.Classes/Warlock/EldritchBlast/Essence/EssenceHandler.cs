using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence;

[ServiceBinding(typeof(EssenceHandler))]
public class EssenceHandler
{
    private const string EssenceVar = "warlock_essence";
    private const int RemoveEssenceId = 1299;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EssenceHandler()
    {
        NwModule.Instance.OnUseFeat += OnRemoveEldritchEssence;
        NwModule.Instance.OnSpellAction += OnEldritchEssence;
        Log.Info(message: "Warlock Essence Handler initialized.");
    }

    private void OnRemoveEldritchEssence(OnUseFeat eventData)
    {
        if (eventData.Feat.Id != RemoveEssenceId) return;

        eventData.Creature.GetObjectVariable<LocalVariableInt>(EssenceVar).Delete();
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;
        player.SendServerMessage("Eldritch Essence removed.".ColorWarlock());
    }

    private void OnEldritchEssence(OnSpellAction eventData)
    {
        int spellId = eventData.Spell.Id;
        if (!Enum.IsDefined(typeof(EssenceType), spellId)) return;

        eventData.Caster.GetObjectVariable<LocalVariableInt>(EssenceVar).Value = spellId;

        if (!eventData.Caster.IsPlayerControlled(out NwPlayer? player)) return;
        string essenceName = eventData.Spell.Name.ToString();

        player.SendServerMessage($"{essenceName} applied.".ColorWarlock());
        player.FloatingTextString($"*{essenceName} Activated*".ColorWarlock(), false, false);

        eventData.PreventSpellCast = true;
    }
}




