using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(CastTechniqueService))]
public class CastTechniqueService
{
    private readonly TechniqueFactory _techniqueFactory;
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
    private static string GetCooldownTag(TechniqueType technique) => $"{technique}_cd";
    private const int MinimumCastTechniqueLevel = 7;

    public CastTechniqueService(TechniqueFactory techniqueFactory)
    {
        _techniqueFactory = techniqueFactory;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnSpellCast += CastBodyTechnique;
        Log.Info(message: "Cast Technique Service initialized.");
    }

    private void CastBodyTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk) return;
        if (!monk.IsMonkLevel(MinimumCastTechniqueLevel)) return;
        if (castData.Spell?.FeatReference?.Id is not { } featId || !SupportedFeatIds.Contains(featId)) return;

        string techniqueName = castData.Spell.FeatReference.Name.ToString();
        TechniqueType castTechnique = (TechniqueType)castData.Spell.FeatReference.Id;

        // Intercept code here to try weightless leap for Floating Leaf monk
        if (castTechnique == TechniqueType.EmptyBody && MonkUtils.GetMonkPath(monk) == PathType.FloatingLeaf)
        {
            if (FloatingLeaf.TryWeightlessLeap(monk))
                return;
        }

        string techniqueCdTag = GetCooldownTag(castTechnique);

        if (TechniqueOnCooldown(monk, techniqueCdTag, techniqueName)) return;

        if (TechniqueRestricted(monk, techniqueName)) return;

        Effect techniqueCd = Effect.VisualEffect(VfxType.None);
        techniqueCd.SubType = EffectSubType.Supernatural;
        techniqueCd.Tag = techniqueCdTag;

        if (!TechniqueCooldowns.TryGetValue(castTechnique, out TimeSpan cooldown)) return;

        monk.ApplyEffect(EffectDuration.Temporary, techniqueCd, cooldown);

        ITechnique? techniqueHandler = _techniqueFactory.GetTechnique(castTechnique);

        techniqueHandler?.HandleCastTechnique(monk, castData);
    }

    private static bool TechniqueOnCooldown(NwCreature monk, string cdTag, string techniqueName)
    {
        Effect? techniqueCd = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == cdTag);
        if (techniqueCd == null) return false;

        if (monk.IsPlayerControlled(out NwPlayer? player))
            SpellUtils.SendRemainingCoolDown(player, techniqueName, techniqueCd.DurationRemaining);

        return true;
    }

    private static bool TechniqueRestricted(NwCreature monk, string techniqueName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;

        if (!monk.IsPlayerControlled(out NwPlayer? player))
            return hasArmor || hasShield;

        if (hasArmor || hasShield)
        {
            string reason = hasArmor ? "wearing armor"
                : "wielding a shield";

            player.SendServerMessage($"Cannot use {techniqueName} because you are {reason}.");
            return true;
        }

        return false;
    }

}
