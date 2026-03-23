using Anvil;
using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class ReloadAnvilCommand : IChatCommand
{
    private readonly bool _isEnabled;


    public ReloadAnvilCommand()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public string Command => "./reloadanvil";
    public string Description => "Reloads the Anvil engine assemblies. Usage: ./reloadanvil";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!_isEnabled)
        {
            caller.SendServerMessage("This command is disabled on live servers.", ColorConstants.Red);
            return;
        }

        if (caller is { IsDM: false, IsPlayerDM: false }) return;
        await NwTask.SwitchToMainThread();

        AnvilCore.Reload();
        caller.SendServerMessage("Anvil assemblies reloaded.", ColorConstants.Green);
    }
}
