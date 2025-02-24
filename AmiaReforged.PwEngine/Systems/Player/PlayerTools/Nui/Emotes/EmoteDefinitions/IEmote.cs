using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;

public interface IEmote
{
    public string Label { get; }
    string Id { get; }

    public void Perform(NwCreature creature);
}