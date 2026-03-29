using System.Numerics;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.EffectUtils;

public static class SummoningService
{
    public static void SummonMany(this Location location, NwCreature caster, int count,
        float radius, float delayMin, float delayMax, Effect summonEffect, TimeSpan summonDuration)
    {
        // Before moving on with the multi summon logic, make sure previous summons are removed
        foreach (NwCreature summon in caster.Associates.Where(a => a.AssociateType == AssociateType.Summoned))
        {
            summon.Unsummon();
        }

        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, caster);

        for (int i = 0; i < count; i++)
        {
            TimeSpan randomDelay = SpellUtils.GetRandomDelay(delayMin, delayMax);
            Location randomLocation = location.GenerateRandomLocationWithinRadius(radius);

            _ = randomLocation.SummonCreature(caster, summonEffect, randomDelay, summonDuration);
        }

        _ = SetSummonFeedbackVisible(caster, delayMax);
        _ = SetSummonsDestroyable(caster, delayMax);
    }

    private static async Task SetSummonFeedbackVisible(NwCreature caster, float delayMax)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(delayMax + 0.1f));
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, caster);
    }

    private static async Task SetSummonsDestroyable(NwCreature caster, float delayMax)
    {
        await NwTask.Delay(TimeSpan.FromSeconds(delayMax + 0.1f));
        foreach (NwCreature associate in caster.Associates)
            associate.IsDestroyable = true;
    }

    private static async Task SummonCreature(this Location location, NwCreature caster, Effect summonEffect,
        TimeSpan delay, TimeSpan summonDuration)
    {
        await NwTask.Delay(delay);
        await caster.WaitForObjectContext();

        location.ApplyEffect(EffectDuration.Temporary, summonEffect, summonDuration);
        foreach (NwCreature associate in caster.Associates)
            associate.IsDestroyable = false;
    }

    private static Location GenerateRandomLocationWithinRadius(this Location location, float radius)
    {
        double randomRadiusFactor = Random.Shared.NextDouble();
        double randomAngleFactor = Random.Shared.NextDouble();

        // Calculate the random distance from the center
        float randomDistance = radius * (float)Math.Sqrt(randomRadiusFactor);

        // Calculate the random angle in which direction the distance will be
        float randomAngle = (float)(randomAngleFactor * 2 * Math.PI);

        // Calculate X and Y offsets
        float xAxisOffset = randomDistance * (float)Math.Cos(randomAngle);
        float yAxisOffset = randomDistance * (float)Math.Sin(randomAngle);

        // Apply offsets to the original location's position, preserving the Z (height) axis
        Vector3 newVector = new
        (
            location.Position.X + xAxisOffset,
            location.Position.Y + yAxisOffset,
            location.Position.Z
        );

        // Give a random orientation (0 to 360 degrees)
        float randomOrientation = (float)(Random.Shared.NextDouble() * 360.0);

        return Location.Create(location.Area, newVector, randomOrientation);
    }
}
