using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes.EmoteDefinitions;
using Anvil.API;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes;

public class EmoteModel
{
    private readonly NwPlayer _player;
    public Dictionary<string, IEmote> Emotes { get; } = new();

    public EmoteModel(NwPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// Uses reflection to fetch all registered emotes and then creates a row with a button for each emote.
    /// Emotes are defined in <see cref="AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.Emotes.EmoteDefinitions"/>
    /// </summary>
    public void InitAllEmotes()
    {
        IEnumerable<Type> emoteTypes =
            typeof(IEmote).Assembly.GetTypes().Where(t =>
                t.GetInterfaces().Contains(typeof(IEmote)) && !t.IsAbstract && typeof(IEmote).IsAssignableFrom(t));

        foreach (Type emoteType in emoteTypes)
        {
            // Create an instance of the emote
            IEmote? emote = (IEmote)Activator.CreateInstance(emoteType);
            
            if (emote == null) continue;
            
            Emotes.TryAdd(emote.Id, emote);
        }
    }
}