using Anvil.API;
using NWN.Core;
using static AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.ReportTouchAttackAc)]
public class ReportTouchAttackAcHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        NwItem? armor = playerCreature.GetItemInSlot(InventorySlot.Chest);
        NwItem? neck = playerCreature.GetItemInSlot(InventorySlot.Neck);
        NwItem? shield = playerCreature.GetItemInSlot(InventorySlot.LeftHand);

        int armorAc = NWScript.GetItemACValue(armor);
        int neckAc = NWScript.GetItemACValue(neck);
        int shieldAc = NWScript.GetItemACValue(shield);
        int addedAc = armorAc + neckAc + shieldAc;
        int touchAc = NWScript.GetAC(playerCreature) - addedAc;

        playerCreature.SpeakString(
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> My Touch AC is:</c> {touchAc} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}