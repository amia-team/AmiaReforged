using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;
using Anvil.API;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.Emotes;

public class EmoteModel
{
    private readonly NwPlayer _player;

    public EmoteModel(NwPlayer player)
    {
        _player = player;
    }

    private Dictionary<string, IEmote> Emotes { get; } = new();

    public void InitAllEmotes()
    {
        IEnumerable<Type> emoteTypes =
            typeof(IEmote).Assembly.GetTypes().Where(t =>
                t.GetInterfaces().Contains(typeof(IEmote)) && !t.IsAbstract && typeof(IEmote).IsAssignableFrom(t));

        foreach (Type emoteType in emoteTypes)
        {
            IEmote? emote = (IEmote)Activator.CreateInstance(emoteType);

            if (emote == null) continue;

            Emotes.TryAdd(emote.Id, emote);
        }
    }
}
