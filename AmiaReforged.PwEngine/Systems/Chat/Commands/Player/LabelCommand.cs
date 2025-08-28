using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

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
            caller.SendServerMessage("Your label must start with a quotation mark.");
            return Task.CompletedTask;
        }

        // Parse the color argument
        Color? color = null;
        string lastArg = args[^1];
        string[] labelStrings;

        if (ColorDictionary.TryGetValue(lastArg.ToLower(), out Color foundColor))
        {
            color = foundColor;

            if (args.Length < 2 || !args[^2].EndsWith('"'))
            {
                caller.SendServerMessage(
                    $"Your label must end with a quotation mark followed by a valid color.\n{UsageMessage}");
                return Task.CompletedTask;
            }

            labelStrings = args[..^1];
        }
        else
        {
            if (!lastArg.EndsWith('"'))
            {
                caller.SendServerMessage(
                    $"Your label must end with a quotation mark followed by a valid color.\n{UsageMessage}");
                return Task.CompletedTask;
            }

            labelStrings = args[..];
        }

        string rawLabel = string.Join(" ", labelStrings);
        string label = rawLabel.TrimStart('"').TrimEnd('"');

        caller.EnterTargetMode(
            targetingData => LabelItem(targetingData, label, color),
            new TargetModeSettings { ValidTargets = ObjectTypes.Item });

        caller.FloatingTextString(
            color != null ?
            $"Labelling item: {label.ColorString(foundColor)}" :
            $"Labelling item: {label}", false);

        return Task.CompletedTask;
    }

    private void LabelItem(ModuleEvents.OnPlayerTarget targetingData, string label, Color? color)
    {
        if (targetingData.TargetObject is not NwItem targetItem) return;
        if (targetItem.RootPossessor != targetingData.Player.ControlledCreature)
        {
            targetingData.Player.SendServerMessage("Can't label an item that's not in your own inventory!");
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

    private const string UsageMessage = "Usage example: ./labelitem \"My Snazzy Label\" green" +
                                        "\nTo view available colors, enter ./labelitem color";

    private const string ColorMessage = "Available colors:" +
                                        "\nblack" +
                                        "\nblue" +
                                        "\nbrown" +
                                        "\ncyan" +
                                        "\ngray" +
                                        "\ngreen" +
                                        "\nlime" +
                                        "\nmagenta" +
                                        "\nmaroon" +
                                        "\nnavy" +
                                        "\nolive" +
                                        "\norange" +
                                        "\npink" +
                                        "\npurple" +
                                        "\nred" +
                                        "\nrose" +
                                        "\nsilver" +
                                        "\nteal" +
                                        "\nwhite" +
                                        "\nyellow";

    private static readonly Dictionary<string, Color> ColorDictionary = new()
    {
        { "black", ColorConstants.Black },
        { "blue", ColorConstants.Blue },
        { "brown", ColorConstants.Brown },
        { "cyan", ColorConstants.Cyan },
        { "gray", ColorConstants.Gray },
        { "green", ColorConstants.Green },
        { "lime", ColorConstants.Lime },
        { "magenta", ColorConstants.Magenta },
        { "maroon", ColorConstants.Maroon },
        { "navy", ColorConstants.Navy },
        { "olive", ColorConstants.Olive },
        { "orange", ColorConstants.Orange },
        { "pink", ColorConstants.Pink },
        { "purple", ColorConstants.Purple },
        { "red", ColorConstants.Red },
        { "rose", ColorConstants.Rose },
        { "silver", ColorConstants.Silver },
        { "teal", ColorConstants.Teal },
        { "white", ColorConstants.White },
        { "yellow", ColorConstants.Yellow }
    };
}
