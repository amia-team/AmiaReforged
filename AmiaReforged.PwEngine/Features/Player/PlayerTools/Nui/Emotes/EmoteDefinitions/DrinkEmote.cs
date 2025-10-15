using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;

[CreatedAtRuntime]
public class DrinkEmote : IEmote
{
    public string Label => "Drink";
    public string Id => "emote_drink";

    public void Perform(NwCreature creature)
    {
        creature.PlayAnimation(Animation.FireForgetDrink, 1.0f);
    }
}
