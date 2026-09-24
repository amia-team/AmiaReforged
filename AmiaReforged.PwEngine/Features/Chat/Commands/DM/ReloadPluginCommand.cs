using Anvil;
using Anvil.API;
using Anvil.Plugins;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// Reloads the PwEngine and Classes plugins using Anvil's PluginManager.
/// Both plugins must be marked [PluginInfo(Isolated = true)] to support individual reload.
/// Disabled on live servers.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class ReloadPluginCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string PwEnginePluginName = "AmiaReforged.PwEngine";
    private const string ClassesPluginName = "AmiaReforged.Classes";

    private readonly bool _isEnabled;

    public ReloadPluginCommand()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public string Command => "./reloadplugins";
    public string Description => "Unloads and reloads PwEngine and Classes plugins. Usage: ./reloadplugins";
    public string AllowedRoles => "DM";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        try
        {
            if (!_isEnabled)
            {
                caller.SendServerMessage("This command is disabled.", ColorConstants.Red);
            }

            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
