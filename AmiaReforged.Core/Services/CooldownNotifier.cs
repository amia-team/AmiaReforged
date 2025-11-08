using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
/// This just gives the player a floaty text when the cooldown is over
/// </summary>
[ServiceBinding(typeof(CooldownNotifier))]
public class CooldownNotifier
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CooldownNotifier()
    {
        NwModule.Instance.OnEffectRemove += OnCooldownComplete;

        Log.Info("Cooldown Notifier initialized.");
    }

    private void OnCooldownComplete(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (!creature.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not { } tag || !tag.EndsWith("_cd")) return;

        string effectName = eventData.Effect.Spell?.Name.ToString() ?? ParseEffectNameFromTag(tag);

        string abilityAvailableMessage = $"{effectName} is available!".ColorString(ColorConstants.Lime);

        player.FloatingTextString(abilityAvailableMessage, false);
    }

    /// <summary>
    /// Parses the tag string into a user-readable effect name.
    /// </summary>
    /// <param name="tag">The input effect tag string.</param>
    /// <returns>A formatted effect name.</returns>
    private static string ParseEffectNameFromTag(string tag)
    {
        // Remove the "_cd" suffix
        tag = tag[..^3];

        // split by underscores
        string[] words = tag.Split('_');
        // capitalize each word and lowercase the rest of the word
        IEnumerable<string> formattedWords = words.Select(word => char.ToUpper(word[0]) + word[1..].ToLower());
        // join words with spaces
        return string.Join(" ", formattedWords);
    }

}

