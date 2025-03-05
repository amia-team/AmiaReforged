using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(BodyTechniqueHandler))]
public class BodyTechniqueHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public BodyTechniqueHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        // Register method to listen for the OnSpellCast event.
        NwModule.Instance.OnSpellCast += CastBodyTechnique;
        Log.Info(message: "Monk Body Technique Handler initialized.");
    }

    private void CastBodyTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk) return;
        if (castData.Spell?.FeatReference is null) return;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;

        int technique = castData.Spell.FeatReference.Id;
        bool isBodyTechnique = technique is MonkFeat.EmptyBody or MonkFeat.KiBarrier or MonkFeat.WholenessOfBody;
        if (!isBodyTechnique) return;

        NwFeat bodyKiPointFeat = NwFeat.FromFeatId(MonkFeat.BodyKiPoint)!;

        if (!monk.KnowsFeat(bodyKiPointFeat) || monk.GetFeatRemainingUses(bodyKiPointFeat) < 1)
        {
            castData.PreventSpellCast = true;
            if (monk.IsPlayerControlled(out NwPlayer? player))
                player.SendServerMessage
                ($"Cannot use {castData.Spell.FeatReference.Name} because your body ki is depleted.",
                    MonkColors.MonkColorScheme);
            return;
        }

        switch (technique)
        {
            case MonkFeat.EmptyBody:
                EmptyBody.CastEmptyBody(castData);
                break;
            case MonkFeat.KiBarrier:
                KiBarrier.CastKiBarrier(castData);
                break;
            case MonkFeat.WholenessOfBody:
                WholenessOfBody.CastWholenessOfBody(castData);
                break;
        }

        monk.DecrementRemainingFeatUses(bodyKiPointFeat);
    }
}