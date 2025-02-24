using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;

[CreatedAtRuntime]
public class CheerEmote : IEmote
{
    public string Label => "Cheer";
    public string Id => "emote_cheer";

    public void Perform(NwCreature creature)
    {
        // Random int between 109 and 111, inclusive.
        int randomCheer = Random.Shared.Next(109, 112);

        creature.PlayAnimation((Animation)randomCheer, 1.0f);
    }
}