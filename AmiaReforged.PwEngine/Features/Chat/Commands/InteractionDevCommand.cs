using System.Text;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

/// <summary>
/// Temporary dev/test command for QA-ing the Interaction Framework.
/// Disabled on the live server via <c>SERVER_MODE</c> environment variable.
/// <list type="bullet">
///   <item><c>./interaction list</c> — lists all registered interaction types</item>
///   <item><c>./interaction &lt;tag&gt;</c> — performs a tick of the named interaction on the caller</item>
/// </list>
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class InteractionDevCommand : IChatCommand
{
    private readonly IInteractionSubsystem _interactions;
    private readonly RuntimeCharacterService _characters;
    private readonly bool _isEnabled;

    public InteractionDevCommand(IInteractionSubsystem interactions, RuntimeCharacterService characters)
    {
        _interactions = interactions;
        _characters = characters;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public string Command => "./interaction";
    public string Description => "Dev tool — list or perform interactions (disabled on live)";
    public string AllowedRoles => "All";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!_isEnabled)
        {
            caller.SendServerMessage("This command is disabled on the live server.", ColorConstants.Red);
            return;
        }

        if (args.Length == 0)
        {
            SendUsage(caller);
            return;
        }

        string subCommand = args[0].ToLowerInvariant();

        if (subCommand == "list")
        {
            ListInteractions(caller);
            return;
        }

        // Treat the first arg as an interaction tag.
        await PerformInteraction(caller, args[0]);
    }

    private void ListInteractions(NwPlayer caller)
    {
        IReadOnlyCollection<string> types = _interactions.GetRegisteredInteractionTypes();

        if (types.Count == 0)
        {
            caller.SendServerMessage("No interaction types are currently registered.", ColorConstants.Orange);
            return;
        }

        StringBuilder sb = new();
        sb.AppendLine($"Registered interaction types ({types.Count}):");
        foreach (string tag in types)
        {
            sb.AppendLine($"  • {tag}");
        }

        caller.SendServerMessage(sb.ToString(), ColorConstants.Lime);
    }

    private async Task PerformInteraction(NwPlayer caller, string tag)
    {
        NwCreature? creature = caller.LoginCreature;
        if (creature is null)
        {
            caller.SendServerMessage("You must be logged in with a character.", ColorConstants.Red);
            return;
        }

        if (!_characters.TryGetPlayerKey(caller, out Guid playerKey))
        {
            caller.SendServerMessage("Could not resolve your character ID.", ColorConstants.Red);
            return;
        }

        CharacterId characterId = CharacterId.From(playerKey);
        Guid targetId = creature.UUID;
        string? areaResRef = creature.Area?.ResRef;

        caller.SendServerMessage($"Performing interaction '{tag}'...", ColorConstants.Cyan);

        CommandResult result = await _interactions.PerformInteractionAsync(
            characterId, tag, targetId, areaResRef);

        if (result.Success)
        {
            caller.SendServerMessage($"Interaction '{tag}': Success", ColorConstants.Lime);
        }
        else
        {
            caller.SendServerMessage($"Interaction '{tag}' failed: {result.ErrorMessage ?? "Unknown error"}", ColorConstants.Red);
        }
    }

    private static void SendUsage(NwPlayer caller)
    {
        caller.SendServerMessage("Usage: ./interaction list | ./interaction <tag>", ColorConstants.White);
    }
}
