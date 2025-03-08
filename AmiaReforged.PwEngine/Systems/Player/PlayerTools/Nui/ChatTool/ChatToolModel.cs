using System.Text.RegularExpressions;
using Anvil.API;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.ChatTool;

public partial class ChatToolModel
{
    private readonly NwPlayer _player;

    public ChatToolModel(NwPlayer player)
    {
        _player = player;
    }

    public string? NextMessage { get; set; }
    public string? ChatHistory { get; set; }

    public NwCreature? Selection { get; set; }

    public void Speak()
    {
        if (Selection == null)
        {
            _player.SendServerMessage(message: "You must select an associate.", ColorConstants.Orange);
            return;
        }

        string pretty = PrettifyMessage(NextMessage!);
        Selection.SpeakString(pretty);
        UpdateChatHistory();
    }

    private string PrettifyMessage(string message)
    {
        // If there are any emotes (denoted by starting with * and ending with *), we want to make them light blue.
        // We should use a regex to get all of the emotes, and then replace them with the same text but wrapped with the color tokens.

        Regex emoteRegex = new(pattern: @"\*(.*?)\*");
        MatchCollection matches = emoteRegex.Matches(message);

        foreach (Match match in matches)
        {
            string emote = match.Groups[1].Value;
            message = message.Replace($"*{emote}*", $"<c{ColorConstants.Purple.ToColorToken()}>*{emote}*</c>");
        }

        // Clean up trailing spaces.
        message = message.Trim();

        // Clean up any words separated by more than one space.
        message = SpaceTrimmer().Replace(message, replacement: " ");


        LogManager.GetCurrentClassLogger().Info($"{message}");
        return message;
    }

    private void UpdateChatHistory()
    {
        if (Selection == null) return;

        ChatHistory += $"{Selection.Name}: {NextMessage}\n";
        SaveToCreature();
    }

    private void SaveToCreature()
    {
        // We convert the list to JSON and save it to the creature as a local variable.
        if (ChatHistory != null) NWScript.SetLocalString(Selection, sVarName: "CHAT_HISTORY", ChatHistory);
    }

    public bool IsAnAssociate(NwCreature creature)
    {
        bool isPlayer = creature == _player.ControlledCreature;
        bool isOwnedByPlayer = _player.LoginCreature != null && _player.LoginCreature.Associates.Contains(creature);

        return isPlayer || isOwnedByPlayer;
    }

    [GeneratedRegex(pattern: "\\s+")]
    private static partial Regex SpaceTrimmer();
}