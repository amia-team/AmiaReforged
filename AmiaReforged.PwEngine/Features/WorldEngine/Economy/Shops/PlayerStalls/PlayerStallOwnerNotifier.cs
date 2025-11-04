using System;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

public interface IPlayerStallOwnerNotifier
{
    Task NotifyAsync(Guid? ownerCharacterId, string message, Color color);
}

[ServiceBinding(typeof(IPlayerStallOwnerNotifier))]
public sealed class RuntimePlayerStallOwnerNotifier : IPlayerStallOwnerNotifier
{
    private readonly RuntimeCharacterService _characters;

    public RuntimePlayerStallOwnerNotifier(RuntimeCharacterService characters)
    {
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
    }

    public async Task NotifyAsync(Guid? ownerCharacterId, string message, Color color)
    {
        if (ownerCharacterId is null)
        {
            return;
        }

        if (!_characters.TryGetPlayer(ownerCharacterId.Value, out NwPlayer? player) || player is null)
        {
            return;
        }

        await NwTask.SwitchToMainThread();

        if (player.IsValid)
        {
            player.SendServerMessage(message, color);
        }
    }
}
