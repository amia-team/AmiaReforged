namespace AmiaReforged.Classes.Types;

public class EssenceVisuals
{
    public EssenceVisuals(IntPtr beamVfx, IntPtr impactVfx, IntPtr doomVfx, int beamVfxConst)
    {
        BeamVfxConst = beamVfxConst;
        BeamVfx = beamVfx;
        ImpactVfx = impactVfx;
        DoomVfx = doomVfx;
    }


    public IntPtr BeamVfx { get; }
    public IntPtr ImpactVfx { get; }
    public IntPtr DoomVfx { get; }
    public int BeamVfxConst { get; }
}