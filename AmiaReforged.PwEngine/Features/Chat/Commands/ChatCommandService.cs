using System.Text.RegularExpressions;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

[ServiceBinding(typeof(ChatCommandService))]
public class ChatCommandService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex CommandRegex = new(@"^\./(\w+)(?:\s+(.*))?$", RegexOptions.Compiled);
    private readonly List<IChatCommand> _commands;

    public ChatCommandService(IEnumerable<IChatCommand> commands)
    {
        _commands = commands.ToList();
        NwModule.Instance.OnPlayerChat += HandleChatCommand;
        Log.Info(message: "Chat Command Service initialized.");
    }

    private void HandleChatCommand(ModuleEvents.OnPlayerChat eventInfo)
    {
        string message = eventInfo.Message;
        if (!message.StartsWith(value: "./")) return;

        eventInfo.Volume = TalkVolume.SilentTalk;

        ResolveCommandFromChatMessage(eventInfo, message);
    }

    private async void ResolveCommandFromChatMessage(ModuleEvents.OnPlayerChat eventInfo, string message)
    {
        try
        {
            (string Command, string[] Args)? parsedCommand = ParseCommand(message);
            if (parsedCommand == null)
            {
                Log.Info("Command not found.");
                await NwTask.SwitchToMainThread();
                return;
            }

            (string command, string[] args) = parsedCommand.Value;

            Log.Info($"{command}");
            foreach (IChatCommand c in _commands.Where(registered =>
                         registered.Command.Replace("./", "").Equals(command)))
            {
                await c.ExecuteCommand(eventInfo.Sender, args);
                return;
            }

            await NwTask.SwitchToMainThread();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    private (string Command, string[] Args)? ParseCommand(string message)
    {
        Match match = CommandRegex.Match(message);
        if (!match.Success)
        {
            return null;
        }

        string command = match.Groups[1].Value;
        string argsString = match.Groups[2].Value;
        string[] args = string.IsNullOrEmpty(argsString) ? [] : argsString.Split(' ');

        return (command, args);
    }
}
