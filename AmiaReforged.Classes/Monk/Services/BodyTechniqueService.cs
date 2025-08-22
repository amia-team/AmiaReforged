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

        if (AbilityRestricted(monk, abilityName))
        {
            castData.PreventSpellCast = true;
            return;
        }

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(techniqueType.Value);

        techniqueHandler?.HandleCastTechnique(monk, castData);

        monk.DecrementRemainingFeatUses(BodyKiPointFeat);
    }

    private static bool AbilityRestricted(NwCreature monk, string abilityName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed
            = monk.GetItemInSlot(InventorySlot.RightHand) != null
              && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Torches;
        bool noBodyKi = BodyKiPointFeat != null &&
                        (!monk.KnowsFeat(BodyKiPointFeat) || monk.GetFeatRemainingUses(BodyKiPointFeat) < 1);

        if (!monk.IsPlayerControlled(out NwPlayer? player))
            return hasArmor || hasShield || hasFocusWithoutUnarmed || noBodyKi;

        if (hasArmor)
        {
            player.SendServerMessage($"Cannot use {abilityName} because you are wearing armor.");
            return hasArmor;
        }

        if (hasShield)
        {
            player.SendServerMessage($"Cannot use {abilityName} because you are wielding a shield.");
            return hasShield;
        }

        if (hasFocusWithoutUnarmed)
        {
            player.SendServerMessage($"Cannot use {abilityName} because you are wielding a focus without being unarmed.");
            return hasFocusWithoutUnarmed;
        }

        if (noBodyKi)
        {
            player.SendServerMessage($"Cannot use {abilityName} because you have no Body Ki Points left.");
            return noBodyKi;
        }

        return false;
    }

    private static TechniqueType? GetTechniqueByFeat(int? techniqueFeatId)
    {
        return techniqueFeatId switch
        {
            MonkFeat.WholenessOfBodyNew => TechniqueType.Wholeness,
            MonkFeat.EmptyBodyNew => TechniqueType.EmptyBody,
            MonkFeat.KiBarrier => TechniqueType.KiBarrier,
            _ => null
        };
    }

}
