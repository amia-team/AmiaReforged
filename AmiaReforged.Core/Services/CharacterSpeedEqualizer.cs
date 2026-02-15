using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
/// Static skins might not have their speed set to PC speed, this sets that right
/// </summary>
[ServiceBinding(typeof(CharacterSpeedEqualizer))]
public class CharacterSpeedEqualizer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CharacterSpeedEqualizer(EventService eventService)
    {
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(EqualizeCharacterSpeed,
            EventCallbackType.After);

        Log.Info(message: "Character Speed Equalizer Service initialized.");
    }

    private void EqualizeCharacterSpeed(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.IsDM || eventData.Player.LoginCreature is not { } loginCreature
            || loginCreature.MovementRate == MovementRate.PC) return;

        MovementRate originalMovementRate = loginCreature.MovementRate;

        loginCreature.MovementRate = MovementRate.PC;

        eventData.Player.SendServerMessage($"Incorrect movement speed detected: {originalMovementRate.ToString()}. " +
                                           $"Movement speed set to PC movement.");
    }
}
