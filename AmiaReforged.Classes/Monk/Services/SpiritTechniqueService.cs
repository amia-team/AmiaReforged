using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(SpiritTechniqueService))]
public class SpiritTechniqueService
{
    private readonly TechniqueFactory _techniqueFactory;

    private static readonly NwFeat? SpiritKiPointFeat = NwFeat.FromFeatId(MonkFeat.SpiritKiPoint);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SpiritTechniqueService(TechniqueFactory techniqueFactory)
    {
        _techniqueFactory = techniqueFactory;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnSpellCast += CastSpiritTechnique;
        Log.Info(message: "Monk Spirit Technique Service initialized.");
    }

    private void CastSpiritTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk) return;
        if (castData.Spell?.FeatReference is null) return;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;
        if (SpiritKiPointFeat == null) return;

        int? techniqueFeatId = castData.Spell.FeatReference.Id;

        TechniqueType? techniqueType = GetTechniqueByFeat(techniqueFeatId);
        if (techniqueType is null) return;

        string abilityName = castData.Spell.FeatReference.Name.ToString();

        if (MonkUtils.AbilityRestricted(monk, abilityName, SpiritKiPointFeat))
        {
            castData.PreventSpellCast = true;
            return;
        }

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(techniqueType.Value);

        techniqueHandler?.HandleCastTechnique(monk, castData);

        monk.DecrementRemainingFeatUses(SpiritKiPointFeat);
    }

    private static TechniqueType? GetTechniqueByFeat(int? techniqueFeatId)
    {
        return techniqueFeatId switch
        {
            MonkFeat.QuiveringPalmNew => TechniqueType.QuiveringPalm,
            MonkFeat.KiShout => TechniqueType.KiShout,
            _ => null
        };
    }
}
