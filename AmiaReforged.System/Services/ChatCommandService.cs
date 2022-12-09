using AmiaReforged.System.Commands;
using AmiaReforged.System.Helpers;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(ChatCommandService))]
public class ChatCommandService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly List<IChatCommand> _commands;

    public ChatCommandService(IEnumerable<IChatCommand> commands)
    {
        _commands = commands.ToList();
        NwModule.Instance.OnPlayerChat += HandleChatCommand;
        Log.Info("Chat Command Service initialized.");
    }

    private void HandleChatCommand(ModuleEvents.OnPlayerChat eventInfo)
    {
        string message = eventInfo.Message;
        if (!message.StartsWith("./")) return;

        eventInfo.Volume = TalkVolume.SilentShout;
        
        ResolveCommandFromChatMessage(eventInfo, message);
    }

    private async void ResolveCommandFromChatMessage(ModuleEvents.OnPlayerChat eventInfo, string message)
    {
        foreach (IChatCommand command in _commands.Where(command => command.Command.Equals(message.Split(' ')[0])))
        {
            await command.ExecuteCommand(eventInfo.Sender, message);
            return;
        }

        await new NwTaskHelper().TrySwitchToMainThread();
    }
}