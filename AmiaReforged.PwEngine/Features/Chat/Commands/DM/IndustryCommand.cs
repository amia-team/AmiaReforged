using System.Text;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// Dev/test command for managing the industry/workstation system at runtime.
/// Disabled on the live server via <c>SERVER_MODE</c> environment variable.
/// <list type="bullet">
///   <item><c>./industry spawn workstation &lt;tag&gt;</c> — spawns a workstation placeable at a clicked location</item>
///   <item><c>./industry list workstations</c> — lists all registered workstation definitions</item>
///   <item><c>./industry info workstation &lt;tag&gt;</c> — shows details of a workstation definition</item>
/// </list>
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class IndustryCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IWorkstationRepository _workstationRepository;
    private readonly WorkstationBootstrapService _bootstrapService;
    private readonly bool _isEnabled;

    public IndustryCommand(
        IWorkstationRepository workstationRepository,
        WorkstationBootstrapService bootstrapService)
    {
        _workstationRepository = workstationRepository;
        _bootstrapService = bootstrapService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public string Command => "./industry";
    public string Description => "Dev tool — manage industry system (disabled on live)";
    public string AllowedRoles => "All";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!_isEnabled)
        {
            caller.SendServerMessage("This command is disabled on the live server.", ColorConstants.Red);
            return Task.CompletedTask;
        }

        if (args.Length < 2)
        {
            SendUsage(caller);
            return Task.CompletedTask;
        }

        string action = args[0].ToLowerInvariant();
        string subject = args[1].ToLowerInvariant();

        switch (action)
        {
            case "spawn" when subject is "workstation" or "ws":
                HandleSpawnWorkstation(caller, args);
                break;

            case "list" when subject is "workstations" or "ws":
                HandleListWorkstations(caller);
                break;

            case "info" when subject is "workstation" or "ws":
                HandleWorkstationInfo(caller, args);
                break;

            default:
                SendUsage(caller);
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleSpawnWorkstation(NwPlayer caller, string[] args)
    {
        if (args.Length < 3)
        {
            caller.SendServerMessage(
                "Usage: ./industry spawn workstation <tag>",
                ColorConstants.Orange);
            return;
        }

        string tag = args[2];

        Workstation? workstation = _workstationRepository.GetByTag(new WorkstationTag(tag));
        if (workstation == null)
        {
            caller.SendServerMessage(
                $"Workstation definition '{tag}' not found. Use './industry list workstations' to see available definitions.",
                ColorConstants.Red);
            return;
        }

        if (string.IsNullOrWhiteSpace(workstation.PlaceableResRef))
        {
            caller.SendServerMessage(
                $"Workstation '{tag}' has no PlaceableResRef configured. Cannot spawn without a blueprint.",
                ColorConstants.Red);
            return;
        }

        caller.FloatingTextString($"Click to place: {workstation.Name}", false);

        caller.EnterTargetMode(targetData => OnSpawnLocationSelected(targetData, workstation),
            new TargetModeSettings
            {
                ValidTargets = ObjectTypes.Tile | ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Door
            });
    }

    private void OnSpawnLocationSelected(ModuleEvents.OnPlayerTarget targetData, Workstation workstation)
    {
        NwPlayer player = targetData.Player;
        NwCreature? creature = player.ControlledCreature;
        NwArea? area = creature?.Area;

        if (area == null)
        {
            player.SendServerMessage("Could not determine target area.", ColorConstants.Red);
            return;
        }

        Location spawnLocation = Location.Create(area, targetData.TargetPosition, creature!.Rotation);

        NwPlaceable? placeable = NwPlaceable.Create(workstation.PlaceableResRef!, spawnLocation);
        if (placeable == null)
        {
            player.SendServerMessage(
                $"Failed to spawn placeable with ResRef '{workstation.PlaceableResRef}'. Check that the blueprint exists.",
                ColorConstants.Red);
            return;
        }

        placeable.Name = workstation.Name;
        placeable.Tag = workstation.Tag.Value;
        placeable.Description = workstation.Description ?? $"A {workstation.Name} workstation.";
        placeable.Useable = true;
        placeable.PlotFlag = false;
        placeable.HP = 200;
        placeable.MaxHP = 200;

        // Apply appearance override if configured
        if (workstation.AppearanceId.HasValue)
        {
            PlaceableTableEntry row = NwGameTables.PlaceableTable.GetRow(workstation.AppearanceId.Value);
            placeable.Appearance = row;
        }

        bool registered = _bootstrapService.RegisterPlaceable(placeable, workstation);
        if (!registered)
        {
            player.SendServerMessage(
                $"Spawned '{workstation.Name}' but failed to register it with the crafting system.",
                ColorConstants.Orange);
            return;
        }

        player.SendServerMessage(
            $"Spawned workstation '{workstation.Name}' (tag: {workstation.Tag.Value}) at {targetData.TargetPosition}.",
            ColorConstants.Green);

        Log.Info("DM {Dm} spawned workstation '{Tag}' in area {Area} at {Pos}.",
            player.PlayerName, workstation.Tag.Value, area.ResRef, targetData.TargetPosition);
    }

    private void HandleListWorkstations(NwPlayer caller)
    {
        List<Workstation> workstations = _workstationRepository.All();

        if (workstations.Count == 0)
        {
            caller.SendServerMessage("No workstation definitions found.", ColorConstants.Orange);
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"=== Workstation Definitions ({workstations.Count}) ===");

        foreach (Workstation ws in workstations.OrderBy(w => w.Tag.Value))
        {
            string resRef = ws.PlaceableResRef ?? "(none)";
            string industries = ws.SupportedIndustries.Count > 0
                ? string.Join(", ", ws.SupportedIndustries.Select(i => i.Value))
                : "(none)";
            sb.AppendLine($"  {ws.Tag.Value} — {ws.Name} | ResRef: {resRef} | Industries: {industries}");
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Cyan);
    }

    private void HandleWorkstationInfo(NwPlayer caller, string[] args)
    {
        if (args.Length < 3)
        {
            caller.SendServerMessage("Usage: ./industry info workstation <tag>", ColorConstants.Orange);
            return;
        }

        string tag = args[2];

        Workstation? workstation = _workstationRepository.GetByTag(new WorkstationTag(tag));
        if (workstation == null)
        {
            caller.SendServerMessage($"Workstation definition '{tag}' not found.", ColorConstants.Red);
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"=== {workstation.Name} ===");
        sb.AppendLine($"  Tag: {workstation.Tag.Value}");
        sb.AppendLine($"  ResRef: {workstation.PlaceableResRef ?? "(none)"}");
        sb.AppendLine($"  Description: {workstation.Description ?? "(none)"}");

        if (workstation.SupportedIndustries.Count > 0)
        {
            sb.AppendLine("  Supported Industries:");
            foreach (IndustryTag industry in workstation.SupportedIndustries)
                sb.AppendLine($"    - {industry.Value}");
        }
        else
        {
            sb.AppendLine("  Supported Industries: (none)");
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Cyan);
    }

    private void SendUsage(NwPlayer caller)
    {
        caller.SendServerMessage(
            "Usage:\n" +
            "  ./industry spawn workstation <tag>  — spawn a workstation at a clicked location\n" +
            "  ./industry list workstations        — list all workstation definitions\n" +
            "  ./industry info workstation <tag>    — show workstation details",
            ColorConstants.Orange);
    }
}
