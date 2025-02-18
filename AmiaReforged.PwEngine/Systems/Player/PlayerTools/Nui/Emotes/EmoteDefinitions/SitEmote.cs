using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;

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