namespace AmiaReforged.Classes.Warlock.Types.EssenceEffects;

public static class EssenceEffectFactory
{
    public static EssenceEffectApplier CreateEssenceEffect(EssenceType type, uint target, uint caster)
    {
        return type switch
        {
            EssenceType.RemoveEssence => new RemoveEssenceEffects(target, caster),
            EssenceType.Frightful => new FrightfulEssenceEffects(target, caster),
            EssenceType.Draining => new DrainingEssenceEffects(target, caster),
            EssenceType.Vitriolic => new VitriolicEssenceEffects(target, caster),
            EssenceType.Screaming => new ScreamingEssenceEffects(target, target),
            EssenceType.Hellrime => new HellrimeEssenceEffects(target, caster),
            EssenceType.Utterdark => new UtterdarkEssenceEffects(target, caster),
            EssenceType.Brimstone => new BrimstoneEssenceEffects(target, caster),
            EssenceType.Beshadowed => new BeshadowedEssenceEffects(target, caster),
            EssenceType.Binding => new BindingEssenceEffects(target, caster),
            EssenceType.Bewitching => new BewitchingEssenceEffects(target, caster),
            _ => new RemoveEssenceEffects(target, caster)
        };
    }
}