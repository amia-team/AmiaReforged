﻿using System.Numerics;
using Anvil.API;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.EffectUtils;

public static class SummonUtility
{
    // This returns the change in X coordinate that should be made to
    // cause an object to be fDistance away at fAngle.
    public static float GetChangeInX(float fDistance, float fAngle) => fDistance * cos(fAngle);

    // This returns the change in Y coordinate that should be made to
    // cause an object to be fDistance away at fAngle.
    public static float GetChangeInY(float fDistance, float fAngle) => fDistance * sin(fAngle);

    public static Vector3 GetChangedPosition(Vector3 vOriginal, float fDistance, float fAngle)
    {
        Vector3 vChanged = default;
        vChanged.Z = vOriginal.Z;
        vChanged.X = vOriginal.X + GetChangeInX(fDistance, fAngle);
        if (vChanged.X < 0.0)
            vChanged.X = -vChanged.X;
        vChanged.Y = vOriginal.Y + GetChangeInY(fDistance, fAngle);
        if (vChanged.Y < 0.0)
            vChanged.Y = -vChanged.Y;

        return vChanged;
    }

    public static IntPtr GenerateNewLocationFromLocation(IntPtr lTarget, float fDistance, float fAngle,
        float fOrientation)
    {
        uint oArea = GetAreaFromLocation(lTarget);
        Vector3 vNewPos = GetChangedPosition(GetPositionFromLocation(lTarget), fDistance, fAngle);
        return Location(oArea, vNewPos, fOrientation);
    }

    public static IntPtr GetRandomLocationAroundPoint(IntPtr locPoint, float fDistance)
    {
        float fAngle = IntToFloat(Random(361));
        float fOrient = IntToFloat(Random(361));

        return GenerateNewLocationFromLocation(locPoint, fDistance, fAngle, fOrient);
    }

    public static void SetSummonsFacing(int summonCount, IntPtr location)
    {
        if (summonCount > 1)
            for (int i = 1; i <= summonCount; i++)
            {
                uint summon = GetAssociate(ASSOCIATE_TYPE_SUMMONED, OBJECT_SELF, i);
                AssignCommand(summon, () => SetFacingPoint(GetPositionFromLocation(location)));
            }
    }

    /// <summary>
    /// Use this for when you want your spell to summon multiple creatures of the same creature resref.
    /// </summary>
    /// <param name="summoner">The creature doing the summoning</param>
    /// <param name="summonVfx">Vfx played when the summon appears, use one-shot vfxs</param>
    /// <param name="unsummonVfx">Vfx played when the summon disappears, use one-shot vfxs</param>
    /// <param name="summonDuration">The summons' duration</param>
    /// <param name="summonCount">How many summons you want to have</param>
    /// <param name="summonResRef">The resref of the creature you want to summon</param>
    /// <param name="summonLocation">The summon location, usually GetSpellTargetLocation</param>
    /// /// <param name="minDelay"></param>
    /// <param name="maxDelay"></param>
    /// <param name="minDist">The minimum distance from the summon location; this varies the summoning location across
    /// the multiple summons</param>
    /// <param name="maxDist">The maximum distance from the summon location; this varies the summoning location across
    /// the multiple summons</param>
    public static async Task SummonMany(NwCreature summoner, int summonVfx, int unsummonVfx, float summonDuration, int summonCount, 
        string summonResRef, IntPtr summonLocation, float minDelay, float maxDelay, float minDist, float maxDist)
    {
        // If there's only one summon, summon that at the summon location, skips the extra work for multiple summoning
        if (summonCount == 1)
        {
            float summonDelay = NwEffects.RandomFloat(minDelay, maxDelay);
            
            IntPtr summonCreature = EffectSummonCreature(summonResRef, summonVfx, summonDelay,
                nUnsummonVisualEffectId: unsummonVfx);
            
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, summonLocation, summonDuration);

            return;
        }
        
        // For multi summoning, hide the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, summoner);
        
        // If there are more summons, do the loopy loop for multiple summons
        
        // First populate an array with the delays for the summons
        float[] delayArray = new float[summonCount];
        
        for (int i = 0; i < summonCount; i++)
        {
            float summonDelay = NwEffects.RandomFloat(minDelay, maxDelay);
            delayArray[i] = summonDelay;
        }
        
        // Sort from lowest to highest
        Array.Sort(delayArray);
        
        // Loop summoning
        for (int i = 0; i < summonCount; i++)
        {
            float delay;
            if (i == 0)
                delay = delayArray[i];
            else
                delay = delayArray[i] - delayArray[i - 1];

            await NwTask.Delay(TimeSpan.FromSeconds(delay));
            
            IntPtr randomSummonLocation = 
                GetRandomLocationAroundPoint(summonLocation, NwEffects.RandomFloat(minDist, maxDist));

            await summoner.WaitForObjectContext();
            
            IntPtr summonCreature = EffectSummonCreature(summonResRef, summonVfx,
                nUnsummonVisualEffectId: unsummonVfx);
            
            HashSet<NwCreature> associatesBeforeSummon = new (summoner.Associates);
            
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, randomSummonLocation, summonDuration);
            
            await NwTask.Delay(TimeSpan.FromSeconds(0.01f));
            
            foreach (NwCreature currentAssociate in summoner.Associates)
                if (!associatesBeforeSummon.Contains(currentAssociate))
                    currentAssociate.IsDestroyable = false;
        }

        foreach (NwCreature associate in summoner.Associates)
        {
            if (associate.ResRef != summonResRef) continue;
            
            associate.IsDestroyable = true;
            
            // Also make sure the summons attack, for some reason multiple summons makes them pretty confused
            NwCreature? nearestHostile = associate.GetNearestCreatures().
                FirstOrDefault(creature => creature.IsReactionTypeHostile(associate));

            if (nearestHostile == null) continue;
                 
            _ = associate.ActionAttackTarget(nearestHostile);
        }
        
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 0, summoner);
    }
    
    /// <summary>
    /// Use this for when you want your spell to summon multiple creatures of different creature resrefs.
    /// </summary>
    /// <param name="summoner">The creature doing the summoning</param>
    /// <param name="summonVfx">Vfx played when the summon appears, use one-shot vfxs</param>
    /// <param name="unsummonVfx">Vfx played when the summon disappears, use one-shot vfxs</param>
    /// <param name="summonDuration">The summons' duration</param>
    /// <param name="summonCount">How many summons you want to have</param>
    /// <param name="summonResRefs">String array containing the resrefs you want to summon</param>
    /// <param name="summonLocation">The summon location, usually GetSpellTargetLocation</param>
    /// /// <param name="minDelay"></param>
    /// <param name="maxDelay"></param>
    /// <param name="minDist">The minimum distance from the summon location; this varies the summoning location across
    /// the multiple summons</param>
    /// <param name="maxDist">The maximum distance from the summon location; this varies the summoning location across
    /// the multiple summons</param>
    public static async Task SummonManyDifferent(NwCreature summoner, int summonVfx, int unsummonVfx, float summonDuration, 
        int summonCount, string[] summonResRefs, IntPtr summonLocation, float minDelay, float maxDelay, float minDist, float maxDist)
    {
        // If there's only one summon, summon that at the summon location, skips the extra work for multiple summoning
        if (summonCount == 1)
        {
            float summonDelay = NwEffects.RandomFloat(minDelay, maxDelay);
            
            IntPtr summonCreature = EffectSummonCreature(summonResRefs[0], summonVfx, summonDelay,
                nUnsummonVisualEffectId: unsummonVfx);
            
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, summonLocation, summonDuration);

            return;
        }
        
        // For multi summoning, hide the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, summoner);
        
        // If there are more summons, do the loopy loop for multiple summons
        
        // First populate an array with the delays for the summons
        float[] delayArray = new float[summonCount];
        
        for (int i = 0; i < summonCount; i++)
        {
            float summonDelay = NwEffects.RandomFloat(minDelay, maxDelay);
            delayArray[i] = summonDelay;
        }
        
        // Sort from lowest to highest
        Array.Sort(delayArray);
        
        // Loop summoning
        for (int i = 0; i < summonCount; i++)
        {
            float delay;
            if (i == 0)
                delay = delayArray[i];
            else
                delay = delayArray[i] - delayArray[i - 1];

            await NwTask.Delay(TimeSpan.FromSeconds(delay));
            
            IntPtr randomSummonLocation = 
                GetRandomLocationAroundPoint(summonLocation, NwEffects.RandomFloat(minDist, maxDist));

            await summoner.WaitForObjectContext();
            
            IntPtr summonCreature = EffectSummonCreature(summonResRefs[i], summonVfx,
                nUnsummonVisualEffectId: unsummonVfx);
            
            HashSet<NwCreature> associatesBeforeSummon = new (summoner.Associates);
            
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, randomSummonLocation, summonDuration);
            
            await NwTask.Delay(TimeSpan.FromSeconds(0.01f));
            
            foreach (NwCreature currentAssociate in summoner.Associates)
                if (!associatesBeforeSummon.Contains(currentAssociate))
                    currentAssociate.IsDestroyable = false;
        }

        foreach (NwCreature associate in summoner.Associates)
        {
            if (!summonResRefs.Contains(associate.ResRef)) continue;
            
            associate.IsDestroyable = true;
            
            // Also make sure the summons attack, for some reason multiple summons makes them pretty confused
            NwCreature? nearestHostile = associate.GetNearestCreatures().
                FirstOrDefault(creature => creature.IsReactionTypeHostile(associate));

            if (nearestHostile == null) continue;
                 
            _ = associate.ActionAttackTarget(nearestHostile);
        }
        
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 0, summoner);
    }

    public static bool IsPactSummon(uint target) =>
        GetResRef(target) is "wlkfiend" or "wlkfey" or "wlkslaadred" or
            "wlkslaadblue" or "wlkslaadgreen" or "wlkslaadgray" or
            "wlkcelestial" or "wlkaberrant" or "wlkelemental";

    public static int GetSummonTier(uint caster)
    {
        int warlockLevels = GetLevelByClass(57, caster);
        int summonTier = warlockLevels switch
        {
            >= 1 and < 5 => 1,
            >= 5 and < 10 => 2,
            >= 10 and < 15 => 3,
            >= 15 and < 20 => 4,
            >= 20 and < 25 => 5,
            >= 25 and < 30 => 6,
            >= 30 => 7,
            _ => 0
        };
        return summonTier;
    }

    // a helper function to help tuning summon duration across the board
    public static int PactSummonDuration(uint caster)
    {
        int warlockLevels = GetLevelByClass(57, caster);
        return 5 + warlockLevels / 2;
    }
}