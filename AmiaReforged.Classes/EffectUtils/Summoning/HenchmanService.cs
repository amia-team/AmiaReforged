using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.Summoning;

[ServiceBinding(typeof(HenchmanService))]
public class HenchmanService(ScriptHandleFactory scriptHandleFactory)
{
    /// <summary>
    /// Summons a henchman to the location.
    /// </summary>
    /// <param name="summoner">Summoner</param>
    /// <param name="henchmanResRef">Henchman resref</param>
    /// <param name="location">Location where the henchman appears</param>
    /// <param name="henchman">Outputs the created henchman creature, or null if creation failed.</param>
    /// <param name="subType">Supernatural (default): Henchman is removed on summoner death.
    ///     Extraordinary: Henchman is removed on death and rest.
    ///     Magical: Henchman is removed on death, rest, and dispel.
    ///     Unyielding: Henchman is not removed at all.</param>
    /// <param name="duration">The henchman is permanent if this is null</param>
    public void SummonHenchman(NwCreature summoner, string henchmanResRef, Location location, out NwCreature? henchman,
        EffectSubType subType = EffectSubType.Supernatural, TimeSpan duration = default)
    {
        henchman = null;

        // Only players can summon henchmen
        if (!summoner.IsPlayerControlled(out NwPlayer? player))
            return;

        henchman = NwCreature.Create(henchmanResRef, location);
        if (henchman == null)
        {
            player.SendServerMessage($"Failed to summon henchman with henchman resref " +
                                      $"{henchmanResRef.ColorString(ColorConstants.Cyan)}. " +
                                      $"Please make a bug report!", ColorConstants.Red);
            return;
        }

        Effect henchmanEffect = HenchmanEffect(summoner, henchman, player, subType);
        if (duration != default)
            henchman.ApplyEffect(EffectDuration.Temporary, henchmanEffect, duration);
        else
            henchman.ApplyEffect(EffectDuration.Permanent, henchmanEffect);
    }

    private Effect HenchmanEffect(NwCreature summoner, NwCreature henchman, NwPlayer player, EffectSubType subType)
    {
        string summonerName = summoner.Name.ColorString(NwColors.NameCyan);
        string henchmanName = henchman.Name.ColorString(NwColors.NamePurple);
        string summonMessage = summonerName + " summons henchman ".ColorString(NwColors.MagicPurple) + henchmanName + ".";
        string dismissMessage = summonerName + " unsummons henchman ".ColorString(NwColors.MagicPurple) + henchmanName + ".";

        ScriptCallbackHandle onApply = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            player.AddHenchmen(henchman);
            player.SendServerMessage(summonMessage);
            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle onInterval = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            // Readd henchman eg if summoner has crashed or whatever
            if (summoner is { IsValid: true, IsDead: false } && henchman.IsValid && !summoner.Associates.Contains(henchman))
                player.AddHenchmen(henchman);

            // Jump henchman to summoner if they are in a different area
            if (summoner is { IsValid: true, IsDead: false } && summoner.Associates.Contains(henchman) &&
                     summoner.Area != henchman.Area)
                henchman.JumpToObject(summoner);

            // Remove henchman if summoner is dead
            if (summoner.IsDead && henchman.IsValid && subType != EffectSubType.Unyielding)
                henchman.Destroy();

            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle onRemove = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            if (henchman.IsValid)
            {
                henchman.Destroy();
                player.SendServerMessage(dismissMessage);
            }

            return ScriptHandleResult.Handled;
        });


        Effect henchmanEffect = Effect.RunAction(onApply, onRemove, onInterval, TimeSpan.FromMinutes(1));
        henchmanEffect.SubType = subType;
        return henchmanEffect;
    }
}
