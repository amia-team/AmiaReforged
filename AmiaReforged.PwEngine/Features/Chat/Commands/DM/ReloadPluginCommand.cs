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

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!_isEnabled)
        {
            caller.SendServerMessage("This command is disabled on live servers.", ColorConstants.Red);
            return;
        }

        if (caller is { IsDM: false, IsPlayerDM: false }) return;

        await NwTask.SwitchToMainThread();

        PluginManager? pluginManager = AnvilCore.GetService<PluginManager>();

        if (pluginManager == null)
        {
            caller.SendServerMessage("Failed to resolve PluginManager.", ColorConstants.Red);
            return;
        }

        try
        {
            // --- Resolve current plugin references and directories BEFORE unloading ---
            Plugin? classesPlugin = pluginManager.GetPlugin(ClassesPluginName);
            Plugin? pwEnginePlugin = pluginManager.GetPlugin(PwEnginePluginName);

            // Get directory paths as strings — these survive plugin unload
            string? classesDir = classesPlugin != null
                ? pluginManager.GetPluginDirectory(classesPlugin.Assembly)
                : null;
            string? pwEngineDir = pwEnginePlugin != null
                ? pluginManager.GetPluginDirectory(pwEnginePlugin.Assembly)
                : null;

            caller.SendServerMessage("Unloading plugins...", ColorConstants.Orange);
            Log.Info("DM {Dm} initiated plugin reload via ./reloadplugins", caller.PlayerName);

            // --- Unload in reverse dependency order: Classes first, then PwEngine ---
            if (classesPlugin != null)
            {
                Log.Info("Unloading {Plugin}...", ClassesPluginName);
                pluginManager.UnloadPlugin(classesPlugin);
                Log.Info("{Plugin} unloaded.", ClassesPluginName);
            }

            if (pwEnginePlugin != null)
            {
                Log.Info("Unloading {Plugin}...", PwEnginePluginName);
                pluginManager.UnloadPlugin(pwEnginePlugin);
                Log.Info("{Plugin} unloaded.", PwEnginePluginName);
            }

            // --- Load in dependency order: PwEngine first, then Classes ---
            if (pwEngineDir != null)
            {
                Log.Info("Loading {Plugin} from {Dir}...", PwEnginePluginName, pwEngineDir);
                pluginManager.LoadPlugin(pwEngineDir);
                Log.Info("{Plugin} loaded.", PwEnginePluginName);
            }
            else
            {
                Log.Warn("Could not determine directory for {Plugin}, skipping load.", PwEnginePluginName);
            }

            if (classesDir != null)
            {
                Log.Info("Loading {Plugin} from {Dir}...", ClassesPluginName, classesDir);
                pluginManager.LoadPlugin(classesDir);
                Log.Info("{Plugin} loaded.", ClassesPluginName);
            }
            else
            {
                Log.Warn("Could not determine directory for {Plugin}, skipping load.", ClassesPluginName);
            }

            // NwPlayer is an Anvil core object — still valid after plugin reload
            caller.SendServerMessage("PwEngine and Classes plugins reloaded successfully.",
                ColorConstants.Green);

            Log.Info("Plugin reload completed successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Plugin reload failed.");
            caller.SendServerMessage($"Plugin reload failed: {ex.Message}", ColorConstants.Red);
        }
    }
}
