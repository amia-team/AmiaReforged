using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.Data.SqlClient.DataClassification;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]
public class LabelCommand : IChatCommand
{
    public string Command => "./labelitem";
    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {


        if (args.Length < 1)
        {
            caller.SendServerMessage(UsageMessage);
            return Task.CompletedTask;
        }

        if (args[0] == "color")
        {
            caller.SendServerMessage(ColorMessage);
            return Task.CompletedTask;
        }

        if (!args[0].StartsWith('"'))
        {
            caller.SendServerMessage("Your label must start with a quotation mark");
            return Task.CompletedTask;
        }

        // Parse the color argument
        Color? color = null;
        string lastArg = args[^1];
        string[] labelStrings;

        if (ColorDictionary.TryGetValue(lastArg, out Color foundColor))
        {
            color = foundColor;

            if (args.Length < 2 || !args[^2].EndsWith('"'))
            {
                caller.SendServerMessage("Your label must end with a quotation mark followed by a color.");
                return Task.CompletedTask;
            }

            labelStrings = args[..^2];
        }
        else
        {
            if (!lastArg.EndsWith('"'))
            {
                caller.SendServerMessage("Your label must end with a quotation mark.");
                return Task.CompletedTask;
            }

            labelStrings = args[..^1];
        }

        string rawLabel = string.Join(" ", labelStrings);
        string label = rawLabel.TrimStart('"').TrimEnd('"');

        caller.EnterTargetMode(
            targetingData => LabelItem(targetingData, label, color),
            new TargetModeSettings { ValidTargets = ObjectTypes.Item });

        return Task.CompletedTask;
    }

    private void LabelItem(ModuleEvents.OnPlayerTarget targetingData, string label, Color? color)
    {
        if (targetingData.TargetObject is not NwItem targetItem) return;
        if (targetItem.RootPossessor != targetingData.Player.ControlledCreature)
        {
            targetingData.Player.SendServerMessage("The item must be in your own inventory!");
            return;
        }
        if (targetItem.ResRef.Contains("mythal"))
        {
            targetingData.Player.SendServerMessage("Can't label a mythal!");
            return;
        }

        targetItem.Name = label;
        if (color != null)
            targetItem.Name = targetItem.Name.ColorString(color.Value);
    }

    private const string UsageMessage = "Usage: ./labelitem \"My Snazzy Label\" color:[color]" +
                                        "\nTo view available colors, enter ./labelitem color";

    private const string ColorMessage = "Available colors:" +
                                        "\ncolor:black" +
                                        "\ncolor:blue" +
                                        "\ncolor:brown" +
                                        "\ncolor:cyan" +
                                        "\ncolor:gray" +
                                        "\ncolor:green" +
                                        "\ncolor:lime" +
                                        "\ncolor:magenta" +
                                        "\ncolor:maroon" +
                                        "\ncolor:navy" +
                                        "\ncolor:olive" +
                                        "\ncolor:orange" +
                                        "\ncolor:pink" +
                                        "\ncolor:purple" +
                                        "\ncolor:red" +
                                        "\ncolor:rose" +
                                        "\ncolor:silver" +
                                        "\ncolor:teal" +
                                        "\ncolor:white" +
                                        "\ncolor:yellow";

    private static readonly Dictionary<string, Color> ColorDictionary = new()
    {
        { "color:black", ColorConstants.Black },
        { "color:blue", ColorConstants.Blue },
        { "color:brown", ColorConstants.Brown },
        { "color:cyan", ColorConstants.Cyan },
        { "color:gray", ColorConstants.Gray },
        { "color:green", ColorConstants.Green },
        { "color:lime", ColorConstants.Lime },
        { "color:magenta", ColorConstants.Magenta },
        { "color:maroon", ColorConstants.Maroon },
        { "color:navy", ColorConstants.Navy },
        { "color:olive", ColorConstants.Olive },
        { "color:orange", ColorConstants.Orange },
        { "color:pink", ColorConstants.Pink },
        { "color:purple", ColorConstants.Purple },
        { "color:red", ColorConstants.Red },
        { "color:rose", ColorConstants.Rose },
        { "color:silver", ColorConstants.Silver },
        { "color:teal", ColorConstants.Teal },
        { "color:white", ColorConstants.White },
        { "color:yellow", ColorConstants.Yellow }
    };
}
