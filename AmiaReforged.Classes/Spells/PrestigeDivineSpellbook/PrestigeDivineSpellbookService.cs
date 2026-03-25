using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// Service for managing the Prestige Divine Spellbook UI window.
/// Provides the interface for Rangers/Paladins to memorize spells they have access to
/// through prestige class caster level boosts.
/// </summary>
[ServiceBinding(typeof(PrestigeDivineSpellbookService))]
public class PrestigeDivineSpellbookService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<(Guid playerId, ClassType classType), PrestigeDivineSpellbookView> _openWindows = new();

    /// <summary>
    /// Opens the Prestige Divine Spellbook window for a player to memorize spells.
    /// </summary>
    public void OpenSpellbook(NwPlayer player, ClassType classType)
    {
        if (player.LoginCreature == null)
        {
            Log.Warn($"Cannot open spellbook: player has no login creature");
            return;
        }

        if (classType is not (ClassType.Ranger or ClassType.Paladin))
        {
            Log.Warn($"Cannot open spellbook for {classType}: only Rangers and Paladins supported");
            return;
        }

        var key = (player.LoginCreature.UUID, classType);

        // Close existing window if open
        if (_openWindows.TryGetValue(key, out var existingView))
        {
            _openWindows.Remove(key);
        }

        try
        {
            var view = new PrestigeDivineSpellbookView(player, classType, player.LoginCreature);
            _openWindows[key] = view;

            Log.Info($"Opened Prestige Divine Spellbook for {player.LoginCreature.Name} ({classType})");
            player.SendServerMessage($"Prestige Divine Spellbook - {classType}", ColorConstants.Cyan);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open Prestige Divine Spellbook: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            player.SendServerMessage($"Error opening spellbook: {ex.Message}", ColorConstants.Red);
        }
    }

    /// <summary>
    /// Closes the spellbook window for a player.
    /// </summary>
    public void CloseSpellbook(NwPlayer player, ClassType classType)
    {
        var key = (player.LoginCreature?.UUID ?? Guid.Empty, classType);

        if (_openWindows.TryGetValue(key, out var view))
        {
            _openWindows.Remove(key);
            Log.Info($"Closed Prestige Divine Spellbook for {player.LoginCreature?.Name ?? "unknown"} ({classType})");
        }
    }

    /// <summary>
    /// Closes all open spellbook windows for a player.
    /// </summary>
    public void CloseAllSpellbooks(NwPlayer player)
    {
        if (player.LoginCreature == null) return;

        var playerId = player.LoginCreature.UUID;
        var keysToRemove = _openWindows.Keys
            .Where(k => k.playerId == playerId)
            .ToList();

        foreach (var key in keysToRemove)
        {
            if (_openWindows.TryGetValue(key, out var view))
            {
                _openWindows.Remove(key);
            }
        }

        if (keysToRemove.Count > 0)
        {
            Log.Info($"Closed {keysToRemove.Count} Prestige Divine Spellbook window(s) for {player.LoginCreature.Name}");
        }
    }
}





