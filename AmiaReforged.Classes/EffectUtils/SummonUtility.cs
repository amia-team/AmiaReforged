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

    public static async Task SummonMany(NwCreature summoner, int summonVfx, int unsummonVfx, float summonDuration, 
        int summonCount, string summonResRef, IntPtr location, float minLoc, float maxLoc, float minDelay, float maxDelay)
    {
        // First unsummon previous summons, because we need to make the new summons undestroyable
        foreach (NwCreature associate in summoner.Associates)
        {
            if (associate.AssociateType == AssociateType.Summoned)
                associate.Unsummon();
        }
        
        // Hide the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, summoner);

        float delay = minDelay;
        
        for (int i = 1; i <= summonCount; i++)
        {
            // Set summons undestroyable so they don't get unsummoned
            foreach (NwCreature associate in summoner.Associates)
                if (associate.AssociateType == AssociateType.Summoned)
                    associate.IsDestroyable = false;
            
            if (i > 1)
            {
                minDelay += 0.2f;
                delay = NwEffects.RandomFloat(minDelay, maxDelay);
            }

            IntPtr summonLocation = GetRandomLocationAroundPoint(location, NwEffects.RandomFloat(minLoc, maxLoc));
            
            IntPtr summonCreature = EffectSummonCreature(summonResRef, summonVfx, delay,
                nUnsummonVisualEffectId: unsummonVfx);
            
            ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, summonCreature, summonLocation, summonDuration);
        }
        
        // Wait a bit so we can make summons destroyable again
        await NwTask.Delay(TimeSpan.FromSeconds(maxDelay + 1));
        
        foreach (NwCreature associate in summoner.Associates)
            if (associate.AssociateType == AssociateType.Summoned)
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