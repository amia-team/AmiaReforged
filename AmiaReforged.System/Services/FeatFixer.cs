using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

/// <summary>
/// If we recently modify a feat in a way that changes the uses we auto remove/add the feat and then boot them,
/// so it saves it to the character's BIC and they relogin and it is back to normal. The BIC isn't automatically updated.
/// </summary>
[ServiceBinding(typeof(FeatFixer))]
public class FeatFixer
{
    private static readonly Feat[] FeatsToFix =
    {
        Feat.EpicBlindingSpeed,
        Feat.DivineWrath
        // Add other feats as needed
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public FeatFixer()
    {
        NwModule.Instance.OnClientEnter += OnEnterFixFeat;

        Log.Info("Feat Fixer initialized.");
    }

    private void OnEnterFixFeat(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.LoginCreature is not { } playerCharacter) return;

        bool featFixed = false;

        foreach (Feat feat in FeatsToFix)
        {
            if (TryFixFeat(playerCharacter, feat))
                featFixed = true;
        }

        if (featFixed)
            eventData.Player.BootPlayer();
    }

    private static bool TryFixFeat(NwCreature playerCharacter, Feat featType)
    {
        NwFeat? feat = playerCharacter.Feats.FirstOrDefault(f => f.FeatType == featType);

        if (feat == null || playerCharacter.GetFeatRemainingUses(feat) != 0) return false;

        playerCharacter.RemoveFeat(feat);
        playerCharacter.AddFeat(feat);

        return true;
    }
}
