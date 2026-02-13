using System.Text;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Chat;

/// <summary>
/// Comprehensive chat processing service that handles:
/// - OOC comments (// or (( prefixes) - colored and tagged as [OOC]
/// - Emote symbol colorization - text between emote symbols gets colored
/// - Player shout channel blocking
/// - Custom emote symbol per player (from chat_emote variable)
///
/// Based on the original nwnx_chat handler script by Tarnus (02.09)
/// </summary>
[ServiceBinding(typeof(ChatProcessingService))]
public sealed class ChatProcessingService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    // Color codes matching the original script
    // RGB "377" = cyan/teal for emotes
    // RGB "742" = brownish/orange for OOC
    private const string EmoteColorCode = "377";
    private const string OocColorCode = "742";

    // Default emote symbol if player hasn't set one
    private const string DefaultEmoteSymbol = "*";

    public ChatProcessingService()
    {
        NwModule.Instance.OnPlayerChat += OnPlayerChat;
        Log.Info("Chat Processing Service initialized.");
    }

    private void OnPlayerChat(ModuleEvents.OnPlayerChat eventInfo)
    {
        // Rewrite legacy f_ commands to ./ prefix so ChatCommandService handles them
        if (eventInfo.Message.StartsWith("f_"))
        {
            string rewritten = RewriteLegacyCommand(eventInfo);
            eventInfo.Message = rewritten;
            return; // Let ChatCommandService pick it up
        }

        // Skip our custom chat commands
        if (eventInfo.Message.StartsWith("./")) return;

        NwPlayer player = eventInfo.Sender;
        NwCreature? creature = player.LoginCreature;
        if (creature == null) return;

        string message = eventInfo.Message;
        TalkVolume volume = eventInfo.Volume;

        // Handle different chat volumes
        switch (volume)
        {
            case TalkVolume.Talk:
            case TalkVolume.Whisper:
                // Process OOC and emotes for talk/whisper
                eventInfo.Message = ProcessTalkMessage(message, creature);
                break;

            case TalkVolume.Shout:
                // Block player shout channel (matching original script behavior)
                if(player.IsDM) return;
                eventInfo.Volume = TalkVolume.SilentTalk;
                player.SendServerMessage("Shout channel is disabled.", ColorConstants.Orange);
                break;

            case TalkVolume.Party:
            case TalkVolume.Tell:
                // These pass through with emote processing
                eventInfo.Message = ProcessEmotes(message, GetEmoteSymbol(creature));
                break;
        }
    }

    /// <summary>
    /// Rewrites a legacy f_ command to ./ prefix and handles special cases like f_bio
    /// which routes differently for DMs vs players.
    /// </summary>
    private static string RewriteLegacyCommand(ModuleEvents.OnPlayerChat eventInfo)
    {
        string message = eventInfo.Message;
        // Strip "f_" prefix
        string stripped = message[2..];
        
        // Parse command and args
        int spaceIndex = stripped.IndexOf(' ');
        string command = spaceIndex == -1 ? stripped : stripped[..spaceIndex];
        string args = spaceIndex == -1 ? "" : stripped[(spaceIndex + 1)..];
        command = command.ToLowerInvariant();

        // Special case: f_bio routes to ./bio for players, ./setbio for DMs
        if (command == "bio")
        {
            bool isDm = eventInfo.Sender.IsDM || eventInfo.Sender.IsPlayerDM;
            command = isDm ? "setbio" : "bio";
        }

        return string.IsNullOrEmpty(args) ? $"./{command}" : $"./{command} {args}";
    }

    /// <summary>
    /// Processes a talk/whisper message for OOC and emote formatting.
    /// </summary>
    private static string ProcessTalkMessage(string message, NwCreature creature)
    {
        // Check for OOC prefixes: // or ((
        if (message.StartsWith("//") || message.StartsWith("(("))
        {
            return FormatAsOoc(message);
        }

        // Get player's emote symbol (or default)
        string emoteSymbol = GetEmoteSymbol(creature);

        // Process emotes
        return ProcessEmotes(message, emoteSymbol);
    }

    /// <summary>
    /// Formats a message as OOC (Out of Character).
    /// </summary>
    private static string FormatAsOoc(string message)
    {
        // Remove the prefix (// or (()
        string content = message.Length > 2 ? message[2..] : "";
        string oocMessage = $"[OOC]{content}";

        // Apply OOC color (RGB "742" - brownish/orange)
        return ApplyColorCode(oocMessage, OocColorCode);
    }

    /// <summary>
    /// Processes emotes in a message, colorizing text between emote symbols.
    /// Matches the original script's text_colorizer function.
    /// </summary>
    private static string ProcessEmotes(string message, string emoteSymbol)
    {
        if (string.IsNullOrEmpty(emoteSymbol)) return message;
        if (!message.Contains(emoteSymbol)) return message;

        StringBuilder output = new(message);
        int searchPosition = 0;

        while (true)
        {
            // Find the first emote symbol
            int firstPos = message.IndexOf(emoteSymbol, searchPosition, StringComparison.Ordinal);
            if (firstPos == -1) break;

            // Find the closing emote symbol
            int secondPos = message.IndexOf(emoteSymbol, firstPos + 1, StringComparison.Ordinal);
            if (secondPos == -1) break;

            // Extract the emote text (including the symbols)
            int length = secondPos - firstPos + emoteSymbol.Length;
            string emoteText = message.Substring(firstPos, length);

            // Colorize the emote
            string colorizedEmote = ApplyColorCode(emoteText, EmoteColorCode);

            // Replace in output (only the first occurrence to handle multiple emotes)
            int outputPos = output.ToString().IndexOf(emoteText, StringComparison.Ordinal);
            if (outputPos != -1)
            {
                output.Remove(outputPos, emoteText.Length);
                output.Insert(outputPos, colorizedEmote);
            }

            // Move search position past this emote
            searchPosition = secondPos + emoteSymbol.Length;

            // Check if we've reached the end
            if (searchPosition >= message.Length) break;
        }

        return output.ToString();
    }

    /// <summary>
    /// Gets the player's custom emote symbol, or the default (*) if not set.
    /// </summary>
    private static string GetEmoteSymbol(NwCreature creature)
    {
        string symbol = creature.GetObjectVariable<LocalVariableString>("chat_emote").Value ?? "";
        return string.IsNullOrEmpty(symbol) ? DefaultEmoteSymbol : symbol;
    }

    /// <summary>
    /// Applies an RGB color code to text using NWN's color token format.
    /// Color code is a 3-character string where each char represents R, G, B values.
    /// </summary>
    private static string ApplyColorCode(string text, string colorCode)
    {
        if (colorCode.Length != 3) return text;

        // Convert color code characters to actual color values
        // In the original script, "377" means R=3, G=7, B=7 (scaled)
        // NWN uses a special encoding where the digit maps to brightness
        char r = MapColorDigit(colorCode[0]);
        char g = MapColorDigit(colorCode[1]);
        char b = MapColorDigit(colorCode[2]);

        return $"<c{r}{g}{b}>{text}</c>";
    }

    /// <summary>
    /// Maps a single color digit (0-9) to an NWN color character.
    /// The original StringToRGBString uses digits 0-9 mapped to color intensity.
    /// </summary>
    private static char MapColorDigit(char digit)
    {
        // StringToRGBString maps digits to color values:
        // 0 = very dark, 9 = very bright
        // We scale 0-9 to approximately 0-255 range
        int value = digit switch
        {
            '0' => 16,   // Very dark (avoid 0 which is null terminator issues)
            '1' => 44,
            '2' => 102,
            '3' => 119,
            '4' => 174,
            '5' => 156,
            '6' => 184,
            '7' => 254,
            '8' => 240,
            '9' => 255,  // Full bright
            _ => 128     // Default to mid-range
        };

        // Ensure we don't use 0 (null terminator issues in NWN)
        return (char)Math.Max(1, value);
    }
}
