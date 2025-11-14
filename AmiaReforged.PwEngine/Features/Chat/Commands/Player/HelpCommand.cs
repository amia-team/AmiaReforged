using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Player;

/// <summary>
/// Help command that lists all available commands to the user.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class HelpCommand : IChatCommand
{
    private readonly IEnumerable<IChatCommand> _commands;

    public HelpCommand(IEnumerable<IChatCommand> commands)
    {
        _commands = commands;
    }

    public string Command => "./help";
    public string Description => "Lists all available commands";
    public string AllowedRoles => "All";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        bool isDm = caller.IsDM;

        // Get all commands available to this user
        List<IChatCommand> availableCommands = _commands
            .Where(cmd => cmd.AllowedRoles == "All" ||
                         (isDm && cmd.AllowedRoles == "DM") ||
                         (!isDm && cmd.AllowedRoles == "Player"))
            .OrderBy(cmd => cmd.Command)
            .ToList();

        caller.SendServerMessage("=== Available Commands ===", ColorConstants.Cyan);

        if (isDm)
        {
            caller.SendServerMessage("DM Commands:", ColorConstants.Yellow);
            foreach (IChatCommand cmd in availableCommands.Where(c => c.AllowedRoles == "DM"))
            {
                caller.SendServerMessage($"  {cmd.Command} - {cmd.Description}", ColorConstants.White);
            }
            caller.SendServerMessage("", ColorConstants.White);
        }

        caller.SendServerMessage("Player Commands:", ColorConstants.Yellow);
        foreach (IChatCommand cmd in availableCommands.Where(c => c.AllowedRoles == "Player" || c.AllowedRoles == "All"))
        {
            caller.SendServerMessage($"  {cmd.Command} - {cmd.Description}", ColorConstants.White);
        }

        caller.SendServerMessage("", ColorConstants.White);
        caller.SendServerMessage($"Total: {availableCommands.Count} commands available", ColorConstants.Gray);
    }
}

