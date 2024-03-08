using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.ReportFlatFootedAc)]
public class ReportFlatFootedAcHandler : IRollHandler
{
    private const string UncannyDodge = "uncanny dodge";

    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        bool hasUncannyDodge =
            playerCreature.Feats.Any(f => f.Name.ToString().ToLowerInvariant().Contains(UncannyDodge));

        int flatFootedAc = hasUncannyDodge
            ? NWScript.GetAC(playerCreature)
            : NWScript.GetAC(playerCreature) - playerCreature.GetAbilityModifier(Ability.Dexterity);

        playerCreature.SpeakString(
            $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> My Flat-footed AC is:</c> {flatFootedAc} <c{AmiaLime.ToColorToken()}>[?]</c>");
    }
}