using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils;

/// <summary>
/// Provides a general-purpose and performant cooldown mechanism for abilities and effects, backed by effect handling
/// that doesn't get stuck with player relogs etc. Optional broadcasting of cooldown messages.
/// </summary>
[ServiceBinding(typeof(CooldownService))]
public class CooldownService(ScriptHandleFactory scriptHandleFactory)
{

    /// <summary>
    /// Applies a named cooldown to a game object for the specified duration. Must be used with IsOnCooldown.
    /// </summary>
    /// <param name="target">The object to apply the cooldown to.</param>
    /// <param name="cdTag">Unique identifier and used in the CD message. Spell or feat name preferred.
    /// Must match IsOnCooldown calls.</param>
    /// <param name="duration">CD duration in TimeSpan, prefer NwTimeSpan</param>
    /// <param name="broadcastCd">If true, sends cooldown feedback to the player</param>
    /// <param name="subType">Subtype: defaults to Supernatural. If CD shouldn't be dispellable, use Supernatural;
    /// if CD should be dispellable, use Magical; if CD should persist through rest and death, use Unyielding.
    /// </param>
    public void ApplyCooldown(NwGameObject target, string cdTag, TimeSpan duration, bool broadcastCd = true,
        EffectSubType subType = EffectSubType.Supernatural)
    {
        Effect cooldownEffect = CooldownEffect(cdTag, duration, broadcastCd);
        cooldownEffect.Tag = cdTag;
        cooldownEffect.SubType = subType;
        target.ApplyEffect(EffectDuration.Temporary, cooldownEffect, duration);
    }

    /// <summary>
    /// Returns true if the named cooldown is currently active on the target object.
    /// For frequent calls, set broadcastCd to false. Must be used with ApplyCooldown.
    /// </summary>
    /// <param name="target">The object to check.</param>
    /// <param name="cdTag">Unique identifier and used in the CD message. Spell or feat name preferred.
    /// Must match ApplyCooldown calls.</param>
    /// /// <param name="broadcastCd">If true, sends remaining duration feedback to the player.</param>
    public bool IsOnCooldown(NwGameObject target, string cdTag, bool broadcastCd = true)
    {
        bool isOnCooldown = target.GetObjectVariable<LocalVariableBool>(cdTag).Value;

        if (isOnCooldown && broadcastCd && target.IsPlayerControlled(out NwPlayer? player))
        {
            Effect? effect = target.ActiveEffects.FirstOrDefault(e => e.Tag == cdTag);
            if (effect != null)
                SendRemainingCooldown(player, cdTag, effect.DurationRemaining);
        }

        return isOnCooldown;
    }


    private Effect CooldownEffect(string cdTag, TimeSpan duration, bool broadcastCd = true)
    {
        ScriptCallbackHandle onApply = scriptHandleFactory.CreateUniqueHandler(info =>
        {
            if (info.ObjectSelf is not { } objectSelf || objectSelf is NwItem)
                return ScriptHandleResult.Handled;

            objectSelf.GetObjectVariable<LocalVariableBool>(cdTag).Value = true;

            if (broadcastCd && objectSelf.IsPlayerControlled(out NwPlayer? player))
            {
                string message = $"{cdTag} is on cooldown for {FormatDuration(duration)}.".ColorString(ColorConstants.Orange);
                player.SendServerMessage(message);
            }

            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle onRemove = scriptHandleFactory.CreateUniqueHandler(info =>
        {
            if (info.ObjectSelf is not { } objectSelf || objectSelf is NwItem)
                return ScriptHandleResult.Handled;

            objectSelf.GetObjectVariable<LocalVariableBool>(cdTag).Delete();

            if (broadcastCd && objectSelf.IsPlayerControlled(out NwPlayer? player))
            {
                string floatingText = $"*{cdTag} Available!*".ColorString(ColorConstants.Lime);
                player.FloatingTextString(floatingText, broadcastToParty: false);
            }

            return ScriptHandleResult.Handled;
        });

        return Effect.RunAction(onApply, onRemove);
    }

    private static void SendRemainingCooldown(NwPlayer player, string cdTag, float secondsRemaining)
    {
        TimeSpan remainingCd = TimeSpan.FromSeconds(secondsRemaining);
        string message = $"{cdTag} is available in {FormatDuration(remainingCd)}.".ColorString(ColorConstants.Orange);
        player.SendServerMessage(message);
    }

    private static string FormatDuration(TimeSpan duration) =>
        duration.TotalSeconds < 60 ? $"{duration.Seconds}s"
        : duration.Seconds == 0 ? $"{duration.Minutes}m"
        : $"{duration.Minutes}m {duration.Seconds}s";
}
