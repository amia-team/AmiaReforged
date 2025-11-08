using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
/// This just gives the player a floaty text when the cooldown is over
/// </summary>
[ServiceBinding(typeof(CooldownNotifier))]
public partial class CooldownNotifier
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

        // Split by underscores first
        string[] words = tag.Split('_');

        // Process each word to handle camel casing and capitalization
        IEnumerable<string> formattedWords = words.SelectMany(word =>
            // Match capitalized segments within the word to handle camel casing
            MyRegex().Matches(word)
                .Select(match => match.Value)
        ).Select(part =>
        {
            // Capitalize the first letter of the word, and make the rest lowercase
            string formattedWord = char.ToUpper(part[0]) + part[1..].ToLower();

            if (formattedWord == "Of")
            {
                formattedWord = "of";
            }

            return formattedWord;
        });

        // Join words with spaces and return
        return string.Join(" ", formattedWords);
    }

    [System.Text.RegularExpressions.GeneratedRegex("([A-Z][a-z]*)|([a-z]+)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}

