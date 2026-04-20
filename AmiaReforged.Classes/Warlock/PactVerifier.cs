using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

/// <summary>
/// Doesn't allow levelup if the warlock hasn't chosen a pact yet. Adds missing pact feats to the warlock on login.
/// </summary>
[ServiceBinding(typeof(PactVerifier))]
public class PactVerifier
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PactVerifier()
    {
        NwModule.Instance.OnClientLevelUpBegin += EnforcePact;
        NwModule.Instance.OnClientEnter += AddMissingPactFeats;
        Log.Info(message: "Pact Verifier initialized.");
    }

    private void EnforcePact(OnClientLevelUpBegin eventData)
    {
        if (eventData.Player.ControlledCreature is not { } warlock
            || warlock.WarlockLevel() < 1)
            return;

        bool hasPact = Enum.GetValues<PactType>()
            .Any(pact => warlock.Feats.Any(f => f.FeatType == (Feat)pact));

        if (hasPact) return;

        eventData.PreventLevelUp = true;
        eventData.Player.SendServerMessage("You must choose a pact before you can level up!");
    }

    private void AddMissingPactFeats(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.ControlledCreature is not { } warlock
            || warlock.WarlockLevel() < 1) return;

        PactType? pact = warlock.GetPact();
        if (pact == null) return;

        Feat[] pactFeats = PactFeatMap.GetFeats(pact.Value);
        bool knowsAllPactFeats = pactFeats.All(feat => warlock.KnowsFeat(feat!));
        if (knowsAllPactFeats) return;

        foreach (Feat pactFeat in pactFeats)
        {
            NwFeat? featToAdd = NwFeat.FromFeatType(pactFeat);
            if (featToAdd == null)
            {
                eventData.Player.SendServerMessage($"Tried to add {nameof(pactFeat)} but could not find it!");
                continue;
            }
            if (warlock.KnowsFeat(featToAdd)) continue;

            warlock.AddFeat(featToAdd, warlock.GetFirstWarlockLevel());
            eventData.Player.SendServerMessage($"Pact feat {featToAdd.Name} added");
        }
    }
}
