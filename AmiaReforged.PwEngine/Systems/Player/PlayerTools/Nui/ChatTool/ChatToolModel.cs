using System.Text.RegularExpressions;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.ChatTool;

public class ChatToolModel
{
    private readonly NwPlayer _player;
    public string? NextMessage { get; set; }
    public string? ChatHistory { get; private set; }

    public NwCreature? Selection { get; set; }

    public ChatToolModel(NwPlayer player)
    {
        _player = player;
    }

    private bool NotAnAssociate(NwCreature creature)
    {
        return false;
    }

    public void Speak()
    {
        if (Selection == null)
        {
            _player.SendServerMessage("You must select an associate.", ColorConstants.Orange);
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
        
        Regex emoteRegex = new Regex(@"\*(.*?)\*");
        MatchCollection matches = emoteRegex.Matches(message);
        
        foreach (Match match in matches)
        {
            string emote = match.Groups[1].Value;
            message = message.Replace($"*{emote}*", $"<c{ColorConstants.Purple.ToColorToken()}>*{emote}*</c>");
        }
        
        // Clean up trailing spaces.
        message = message.Trim();
        
        // Clean up any words separated by more than one space.
        message = Regex.Replace(message, @"\s+", " ");
        
        // Clean up excessive .'s and !'s, but allow for triplicates
        message = Regex.Replace(message, @"[.]{4,}", "...");
        
        LogManager.GetCurrentClassLogger().Info($"{message}");
        return message;
    }

    private void UpdateChatHistory()
    {
        if(Selection == null)
        {
            return;
        }
        
        ChatHistory += $"{Selection.Name}: {NextMessage}\n";
    }

    private void SaveToCreature()
    {
        // We convert the list to JSON and save it to the creature as a local variable.
    }

    public bool IsAnAssociate(NwCreature creature)
    {
        bool isPlayer = creature == _player.ControlledCreature;
        NwPlayer? controller = creature.ControllingPlayer;
        bool isOwnedByPlayer = controller != null && controller == _player;
           
        return isPlayer || isOwnedByPlayer;
    }
}