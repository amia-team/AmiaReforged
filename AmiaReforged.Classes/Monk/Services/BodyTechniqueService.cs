using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(BodyTechniqueService))]
public class BodyTechniqueService
{
    private readonly TechniqueFactory _techniqueFactory;
    private static readonly NwFeat? BodyKiPointFeat = NwFeat.FromFeatId(MonkFeat.BodyKiPoint);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public BodyTechniqueService(TechniqueFactory techniqueFactory)
    {
        _techniqueFactory = techniqueFactory;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnSpellCast += CastBodyTechnique;
        Log.Info(message: "Monk Body Technique Service initialized.");
    }

    private void CastBodyTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk) return;
        if (castData.Spell?.FeatReference is null) return;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;
        if (BodyKiPointFeat == null) return;

        int? techniqueFeatId = castData.Spell.FeatReference.Id;

        TechniqueType? techniqueType = GetTechniqueByFeat(techniqueFeatId);
        if (techniqueType is null) return;

        string abilityName = castData.Spell.FeatReference.Name.ToString();

        if (MonkUtils.AbilityRestricted(monk, abilityName, BodyKiPointFeat))
        {
            castData.PreventSpellCast = true;
            return;
        }

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(techniqueType.Value);

        techniqueHandler?.HandleCastTechnique(monk, castData);

        if (techniqueType == TechniqueType.WholenessOfBody && Random.Shared.Roll(2) == 1)
        {
            monk.ControllingPlayer?.FloatingTextString("*Ki Body Point preserved*");
            return;
        }

        monk.DecrementRemainingFeatUses(BodyKiPointFeat);
    }

    private static TechniqueType? GetTechniqueByFeat(int? techniqueFeatId)
    {
        return techniqueFeatId switch
        {
            MonkFeat.WholenessOfBodyNew => TechniqueType.WholenessOfBody,
            MonkFeat.EmptyBodyNew => TechniqueType.EmptyBody,
            MonkFeat.KiBarrier => TechniqueType.KiBarrier,
            _ => null
        };
    }

}
