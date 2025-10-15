using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;

[CreatedAtRuntime]
public class SitEmote : IEmote
{
    public string Label => "Sit";
    public string Id => "emote_sit";

    public void Perform(NwCreature creature)
    {
        creature.PlayAnimation(Animation.LoopingSitCross, 1.0f, true, TimeSpan.FromMinutes(60));
    }
}
