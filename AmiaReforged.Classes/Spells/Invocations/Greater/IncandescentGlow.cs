using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

public class IncandescentGlow
{
    private readonly NwPlayer _player;
    private readonly Location _location;

    public IncandescentGlow(NwPlayer player, Location location)
    {
        _player = player;
        _location = location;
    }

    public void CastIncandescentGlow()
    {
        _location.GetObjectsInShape(Shape.Sphere, RADIUS_SIZE_COLOSSAL, false, ObjectTypes.Placeable)
            .Where(p => p.ResRef == "incandescentglow").ToList().ForEach(p => p.Destroy());

        NwPlaceable placeable = NwPlaceable.Create("incandescentglow", _location);
        float duration = RoundsToSeconds(GetCasterLevel(_player.LoginCreature));

        IntPtr glow = EffectAreaOfEffect(52);

        NwEffects.RemoveAoeWithTag(_location, _player.LoginCreature, "VFX_PER_WLK_INCAN", RADIUS_SIZE_COLOSSAL);
        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, glow, _location, duration);
        
        DelayCommand(duration + 1.0f, () => placeable.Destroy());
    }
}