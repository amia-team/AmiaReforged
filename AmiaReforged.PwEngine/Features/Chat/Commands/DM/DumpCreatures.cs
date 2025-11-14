using System.Text.Json;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class DumpCreatures : IChatCommand
{
    [Inject] private JsonWritingService? JsonWritingService { get; set; }
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public string Command => "./dumpcreatures";
    public string Description => "Dump creature data to JSON file";
    public string AllowedRoles => "DM";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM || !caller.IsPlayerDM) return Task.CompletedTask;

        // If it's the live server, don't allow this command.
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live")
        {
            caller.SendServerMessage("This command is disabled on the live server due to performance concerns.");
            return Task.CompletedTask;
        }

        if (JsonWritingService is null) return Task.CompletedTask;

        if (args.Length < 1)
        {
            caller.SendServerMessage("Usage: ./dumpcreatures encounters|all|summons|familiars|companions");
            return Task.CompletedTask;
        }



        JsonWritingService.WriteOnlyEncounterCreatureData();

        return Task.CompletedTask;
    }

    private void WriteToJson(List<string> areaNames, string serverPath)
    {
        string finalPath = Path.Combine(serverPath, "development/creatures.json");

        // Just make a file in there that has nothing in it for now.

        using StreamWriter file = File.CreateText(finalPath);
        string serializedData = JsonSerializer.Serialize(areaNames);

        file.WriteLine(serializedData);

        Log.Info("Wrote area names to file.");
    }
}
