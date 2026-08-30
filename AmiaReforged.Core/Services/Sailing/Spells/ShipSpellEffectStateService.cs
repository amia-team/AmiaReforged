using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(ShipSpellEffectStateService))]
public sealed class ShipSpellEffectStateService
{

    private readonly Dictionary<
        string,
        List<ShipSpellEffectInstance>>
        _activeEffects =
            new(StringComparer.Ordinal);

    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    public float GetMovementMultiplier(
        ShipState ship)
    {
        if (!_activeEffects.TryGetValue(
                ship.ShipName,
                out List<ShipSpellEffectInstance>? effects))
        {
            return 1.0f;
        }

        DateTime now = DateTime.UtcNow;

        effects.RemoveAll(
            effect =>
                effect.ExpiresAt <= now);

        if (effects.Count == 0)
        {
            _activeEffects.Remove(
                ship.ShipName);

            return 1.0f;
        }

        float multiplier = 1.0f;

        foreach (ShipSpellEffectInstance effect in effects)
        {
            multiplier *=
                effect.SpeedMultiplier;
        }

        return multiplier;
    }

    public void ApplySpeedBoost(
        ShipState ship,
        string spellName,
        float bonusPercent,
        TimeSpan duration)
    {
        if (!_activeEffects.TryGetValue(
                ship.ShipName,
                out List<ShipSpellEffectInstance>? effects))
        {
            effects =
                new List<ShipSpellEffectInstance>();

            _activeEffects[ship.ShipName] =
                effects;
        }

        effects.RemoveAll(
            effect =>
                string.Equals(
                    effect.SpellName,
                    spellName,
                    StringComparison.OrdinalIgnoreCase));

        effects.Add(
            new ShipSpellEffectInstance
            {
                SpellName = spellName,
                SpeedMultiplier =
                    1.0f + (bonusPercent / 100.0f),
                ExpiresAt =
                    DateTime.UtcNow.Add(duration)
            });

        Log.Info(
            $"Ship spell speed boost applied: " +
            $"Ship={ship.ShipName}, " +
            $"Spell={spellName}, " +
            $"Bonus={bonusPercent:0}%, " +
            $"Duration={duration.TotalSeconds:0.0}s.");
    }

}