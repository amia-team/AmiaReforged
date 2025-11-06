using Anvil.API;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.CharacterTools.TemporaryNameChanger;

public sealed class TemporaryNameChangerModel(NwPlayer player)
{
    public void SetTemporaryName(string tempName)
    {
        if (string.IsNullOrWhiteSpace(tempName))
        {
            player.SendServerMessage("Please enter a temporary name.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null)
        {
            player.SendServerMessage("Error: Could not get controlled creature.", ColorConstants.Red);
            return;
        }

        // Use RenamePlugin to set the player name override
        // This changes the player name shown in chat, etc., but NOT the character name
        // DMs will still see the original name
        RenamePlugin.SetPCNameOverride(player.LoginCreature, tempName);
        player.SendServerMessage($"Temporary name set to: {tempName}", ColorConstants.Green);
    }

    public void RestoreOriginalName()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null)
        {
            player.SendServerMessage("Error: Could not get controlled creature.", ColorConstants.Red);
            return;
        }

        // Use RenamePlugin to clear the player name override
        RenamePlugin.ClearPCNameOverride(player.LoginCreature);
        player.SendServerMessage("Original name restored.", ColorConstants.Green);
    }
}

