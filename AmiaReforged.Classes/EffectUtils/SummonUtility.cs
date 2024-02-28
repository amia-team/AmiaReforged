using System.Numerics;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;
namespace AmiaReforged.Classes.EffectUtils;

public static class SummonUtility
{
    // This returns the change in X coordinate that should be made to
    // cause an object to be fDistance away at fAngle.
    public static float GetChangeInX(float fDistance, float fAngle)
    {
        return fDistance * cos(fAngle);
    }

    // This returns the change in Y coordinate that should be made to
    // cause an object to be fDistance away at fAngle.
    public static float GetChangeInY(float fDistance, float fAngle)
    {
        return fDistance * sin(fAngle);
    }

    public static Vector3 GetChangedPosition(Vector3 vOriginal, float fDistance, float fAngle)
    {
        Vector3 vChanged = default;
        vChanged.Z = vOriginal.Z;
        vChanged.X = vOriginal.X + GetChangeInX(fDistance, fAngle);
        if (vChanged.X < 0.0)
            vChanged.X = - vChanged.X;
        vChanged.Y = vOriginal.Y + GetChangeInY(fDistance, fAngle);
        if (vChanged.Y < 0.0)
            vChanged.Y = - vChanged.Y;

        return vChanged;
    }

    public static IntPtr GenerateNewLocationFromLocation(IntPtr lTarget, float fDistance, float fAngle, float fOrientation)
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
        if (summonCount == 1)
        {
            uint summon = GetAssociate(ASSOCIATE_TYPE_SUMMONED, OBJECT_SELF);
            AssignCommand(summon, () => SetFacingPoint(GetPositionFromLocation(location)));
        }
        if (summonCount > 1)
        {
            for (int i = 1; i <= summonCount; i++)
            {
                uint summon = GetAssociate(ASSOCIATE_TYPE_HENCHMAN, OBJECT_SELF, i);
                AssignCommand(summon, () => SetFacingPoint(GetPositionFromLocation(location)));
            }
        }
    }

    public static void SummonMany(uint caster, float summonDuration, int summonCount, string summonResRef, IntPtr location,
        float minLoc, float maxLoc, float minDelay, float maxDelay, int summonVfx = 0, float summonVfxFloat = 1)
    {
        for (int i = 1; i <= summonCount; i++)
        {
            float delay = NwEffects.RandomFloat(minDelay, maxDelay);
            IntPtr summonLocation = GetRandomLocationAroundPoint(location, NwEffects.RandomFloat(minLoc, maxLoc));
            float newDelay = delay + 0.1f;
            string newTag = summonResRef + IntToString(i) + GetSubString(GetName(caster), 0, 2);
            DelayCommand(delay, () => CreateObject(OBJECT_TYPE_CREATURE, summonResRef, summonLocation, 0, newTag));
            DelayCommand(delay, () => ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(summonVfx, 0, summonVfxFloat), summonLocation));
            DelayCommand(newDelay, () => AddHenchman(caster, GetObjectByTag(newTag)));
            DelayCommand(newDelay, () => DelayCommand(summonDuration, () => RemoveHenchman(caster, GetObjectByTag(newTag))));
        }
    }

    public static bool IsPactSummon(uint target)
    {
        return GetResRef(target) is "wlkfiend" or "wlkfey" or "wlkslaadred" or
            "wlkslaadblue" or "wlkslaadgreen" or "wlkslaadgray" or
            "wlkcelestial" or "wlkaberrant" or "wlkelemental";
    }

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
        return 5 + warlockLevels/2;
    }
}