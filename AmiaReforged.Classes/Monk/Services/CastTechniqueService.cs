using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(CastTechniqueService))]
public class CastTechniqueService
{
    private readonly TechniqueFactory _techniqueFactory;
    private readonly CooldownService _cooldownService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<TechniqueType, TimeSpan> TechniqueCooldowns = new()
    {
        { TechniqueType.WholenessOfBody, TimeSpan.FromMinutes(3) },
        { TechniqueType.EmptyBody, TimeSpan.FromMinutes(6) },
        { TechniqueType.KiBarrier, TimeSpan.FromMinutes(15) },
        { TechniqueType.QuiveringPalm, TimeSpan.FromMinutes(10) },
        { TechniqueType.KiShout, TimeSpan.FromMinutes(15) }
    };
    private static readonly HashSet<int> SupportedFeatIds =
        TechniqueCooldowns.Keys.Select(x => (int)x).ToHashSet();
    private static readonly NwFeat? WholenessOfBody = NwFeat.FromFeatId(MonkFeat.WholenessOfBodyNew);
    private const string NoSpecialAbilitiesVar = "NoSpecialAbilities";

    public CastTechniqueService(TechniqueFactory techniqueFactory, CooldownService cooldownService)
    {
        _techniqueFactory = techniqueFactory;
        _cooldownService = cooldownService;

        NwModule.Instance.OnSpellCast += CastBodyTechnique;
        Log.Info(message: "Cast Technique Service initialized.");
    }

    private void CastBodyTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk
            || !monk.KnowsFeat(WholenessOfBody!)
            || castData.Spell?.FeatReference?.Id is not { } featId
            || !SupportedFeatIds.Contains(featId)
            || IsSpecialAbilityRestricted(monk)) return;

        string techniqueName = castData.Spell.FeatReference.Name.ToString();
        TechniqueType castTechnique = (TechniqueType)castData.Spell.FeatReference.Id;

        if (IsTechniqueRestricted(monk, techniqueName)
            || !TechniqueCooldowns.TryGetValue(castTechnique, out TimeSpan cooldown))
            return;

        if (_cooldownService.IsOnCooldown(monk, techniqueName))
            return;

        _cooldownService.ApplyCooldown(monk, techniqueName, cooldown);

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(castTechnique);

        if (techniqueHandler is ICastTechnique castHandler)
        {
            castHandler.HandleCastTechnique(monk, castData);
        }
    }

    private static bool IsTechniqueRestricted(NwCreature monk, string techniqueName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;

        if (!monk.IsPlayerControlled(out NwPlayer? player))
            return hasArmor || hasShield;

        if (hasArmor || hasShield)
        {
            string reason = hasArmor ? "are wearing armor"
                : "have a shield";

            player.SendServerMessage($"Cannot use {techniqueName} because you {reason}.");
            return true;
        }

        return false;
    }

    private static bool IsSpecialAbilityRestricted(NwCreature monk)
    {
        if (monk.Area?.GetObjectVariable<LocalVariableInt>(NoSpecialAbilitiesVar).Value != 1) return false;

        monk.ControllingPlayer?
            .FloatingTextString("- You may not use this Special Ability in this area! -", false);
        return true;
    }
}
