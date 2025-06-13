using System.Numerics;
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
        // First unsummon previous summons, because we need to make the new summons undestroyable
        foreach (NwCreature associate in summoner.Associates)
        {
            if (associate.ResRef == summonResRef)
                associate.Unsummon();
        }
        
        // Hide the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, summoner);
        
        // If there's only one summon, summon that at the summon location, skips the extra work for multiple summoning
        if (summonCount == 1)
        {
            float summonDelay = NwEffects.RandomFloat(minDelay, maxDelay);
            
            IntPtr summonCreature = EffectSummonCreature(summonResRef, summonVfx, summonDelay,
                nUnsummonVisualEffectId: unsummonVfx);
            
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, summonLocation, summonDuration);

            return;
        }
        
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
            IntPtr randomSummonLocation = 
                GetRandomLocationAroundPoint(summonLocation, NwEffects.RandomFloat(minDist, maxDist));
            
            IntPtr summonCreature = EffectSummonCreature(summonResRef, summonVfx,
                nUnsummonVisualEffectId: unsummonVfx);
            
            DelayCommand(delayArray[i], () =>
                ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, randomSummonLocation, summonDuration));
        }
        
        // Loop making summons undestroyable; note: NWScript starts indexing at 1
        for (int i = 1; i <= summonCount; i++)
        {
            int nth = i;
            DelayCommand(delayArray[i] + 0.1f, () =>
                SetIsDestroyable(FALSE, oObject: GetAssociate(ASSOCIATE_TYPE_SUMMONED, summoner, nth)));
        }
        
        
        // Wait a bit so we can make summons destroyable again
        await NwTask.Delay(TimeSpan.FromSeconds(maxDelay + 1));
        
        foreach (NwCreature associate in summoner.Associates)
            if (associate.ResRef == summonResRef)
            {
                associate.IsDestroyable = true;

                if (!summoner.IsInCombat) continue;
                
                // Also make sure the summons attack, for some reason multiple summons makes them pretty confused
                NwCreature nearestHostile = associate.GetNearestCreatures().
                    First(creature => creature.IsReactionTypeHostile(associate));
                     
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