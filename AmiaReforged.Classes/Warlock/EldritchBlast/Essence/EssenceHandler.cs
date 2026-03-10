using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

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
        eventData.Creature.ControllingPlayer?.SendServerMessage(WarlockUtils.String(message: "Eldritch Essence removed."));
    }

    private void OnEldritchEssence(OnSpellAction eventData)
    {
        int spellId = eventData.Spell.Id;
        if (!Enum.IsDefined(typeof(EssenceType), spellId)) return;

        eventData.Caster.GetObjectVariable<LocalVariableInt>(EssenceVar).Value = spellId;
        EssenceType essenceType = (EssenceType)spellId;

        eventData.Caster.ControllingPlayer?
            .SendServerMessage(WarlockUtils.String(message: $"{essenceType.ToString()} Essence applied."));

        eventData.PreventSpellCast = true;
    }
}




