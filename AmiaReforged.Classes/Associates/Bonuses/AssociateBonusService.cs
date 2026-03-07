using AmiaReforged.Classes.Shadowdancer.Shadow;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Associates.Bonuses;

[ServiceBinding(typeof(AssociateBonusService))]
public class AssociateBonusService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly List<IFamiliarBonusStrategy> _familiarStrategies;
    private readonly List<ICompanionBonusStrategy> _companionStrategies;
    private readonly List<ISummonBonusStrategy> _summonStrategies;

    public AssociateBonusService(
        IEnumerable<IFamiliarBonusStrategy> familiarStrategies,
        IEnumerable<ICompanionBonusStrategy> companionStrategies,
        IEnumerable<ISummonBonusStrategy> summonStrategies)
    {
        _familiarStrategies = familiarStrategies.ToList();
        _companionStrategies = companionStrategies.ToList();
        _summonStrategies = summonStrategies.ToList();

        foreach (IFamiliarBonusStrategy strategy in _familiarStrategies)
            Log.Info($"Registered familiar bonus strategy for resref prefix '{strategy.ResRefPrefix}'.");

        foreach (ICompanionBonusStrategy strategy in _companionStrategies)
            Log.Info($"Registered companion bonus strategy for resref prefix '{strategy.ResRefPrefix}'.");

        foreach (ISummonBonusStrategy strategy in _summonStrategies)
            Log.Info($"Registered summon bonus strategy for resref prefix '{strategy.ResRefPrefix}'.");

        NwModule.Instance.OnAssociateAdd += ApplyAssociateBonus;
        Log.Info(
            $"Associate Bonus Service initialized with {_familiarStrategies.Count} familiar, " +
            $"{_companionStrategies.Count} companion, and {_summonStrategies.Count} summon strategies.");
    }

    private void ApplyAssociateBonus(OnAssociateAdd eventData)
    {
        NwCreature owner = eventData.Owner;
        NwCreature associate = eventData.Associate;
        string resRef = associate.ResRef;



        switch (eventData.AssociateType)
        {
            case AssociateType.AnimalCompanion:
                CompanionBonuses companionBonuses = new(owner, associate);
                companionBonuses.ApplyCompanionBonus();

                ICompanionBonusStrategy? companionStrategy =
                    _companionStrategies.FirstOrDefault(s => resRef.StartsWith(s.ResRefPrefix));
                if (companionStrategy != null)
                {
                    Log.Info($"Applying companion strategy '{companionStrategy.GetType().Name}' for resref '{resRef}'.");
                    companionStrategy.Apply(owner, associate);
                }

                break;
            case AssociateType.Familiar:
                FamiliarBonuses familiarBonuses = new(owner, associate);
                familiarBonuses.ApplyFamiliarBonus();

                IFamiliarBonusStrategy? familiarStrategy =
                    _familiarStrategies.FirstOrDefault(s => resRef.StartsWith(s.ResRefPrefix));
                if (familiarStrategy != null)
                {
                    Log.Info($"Applying familiar strategy '{familiarStrategy.GetType().Name}' for resref '{resRef}'.");
                    familiarStrategy.Apply(owner, associate);
                }

                break;
            case AssociateType.Summoned:
                if (associate.ResRef.StartsWith("sd_shadow_"))
                    ShadowBonuses.ApplyShadowBonuses(owner, associate);

                ISummonBonusStrategy? summonStrategy =
                    _summonStrategies.FirstOrDefault(s => resRef.StartsWith(s.ResRefPrefix));
                if (summonStrategy != null)
                {
                    Log.Info($"Applying summon strategy '{summonStrategy.GetType().Name}' for resref '{resRef}'.");
                    summonStrategy.Apply(owner, associate);
                }

                break;
        }
    }
}
