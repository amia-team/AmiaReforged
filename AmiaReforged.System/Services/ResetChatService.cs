using AmiaReforged.System.Commands;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(ResetChatService))]
public class ResetChatService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly List<IChatCommand> _commands;

    public ResetChatService(IEnumerable<IChatCommand> commands)
    {
        _commands = commands.ToList();
        NwModule.Instance.OnPlayerChat += HandleChatCommand;
        Log.Info("Reset Control Panel Service initialized.");
    }

    private void HandleChatCommand(ModuleEvents.OnPlayerChat eventInfo)
    {
        string message = eventInfo.Message;
        if (!message.StartsWith("./")) return;

        eventInfo.Volume = TalkVolume.SilentShout;
        
        ResolveCommandFromChatMessage(eventInfo, message);
    }

    private void ResolveCommandFromChatMessage(ModuleEvents.OnPlayerChat eventInfo, string message)
    {
        foreach (IChatCommand command in _commands.Where(command => command.Command.Equals(message.Split(' ')[0])))
        {
            command.ExecuteCommand(eventInfo.Sender, message);
            return;
        }
    }
}