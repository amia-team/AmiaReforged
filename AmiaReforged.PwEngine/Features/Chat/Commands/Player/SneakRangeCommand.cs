using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Player;

[ServiceBinding(typeof(IChatCommand))]
public class SneakRangeCommand : IChatCommand
{
    private const VfxType DurAuraGray9MVfx = (VfxType)2541;

    public string Command => "./sneakrange";
    public string Description =>
        "Produces a visual effect to indicate the range of sneak attacks that is only visible to the player.";
    public string AllowedRoles => "All";
    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.LoginCreature is not { } loginCreature) return Task.CompletedTask;
        VisualEffectTableEntry? sneakRangeVfx = DurAuraGray9MVfx;
        if (sneakRangeVfx == null)
        {
            caller.SendServerMessage("Sneak range visual effect not found. Please send a bug report!");
            return Task.CompletedTask;
        }

        List<VisualEffectTableEntry>? loopingVfxList = caller.GetLoopingVisualEffects(loginCreature);

        if (loopingVfxList != null && loopingVfxList.Any(vfx => vfx.RowIndex == sneakRangeVfx.RowIndex))
        {
            loopingVfxList.RemoveAll(vfx => vfx.RowIndex == sneakRangeVfx.RowIndex);
            caller.FloatingTextString("Sneak range visual effect removed", false);

            return Task.CompletedTask;
        }

        caller.AddLoopingVisualEffect(loginCreature, sneakRangeVfx);
        caller.FloatingTextString("Sneak range visual effect added", false);

        return Task.CompletedTask;
    }
}
