using AmiaReforged.Classes.Monk.WildMagic.EffectLists;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic;

[ServiceBinding(typeof(WildMagicEffects))]
public class WildMagicEffects(
    AdverseWildMagic adverseWildMagic,
    WeakWildMagic weakWildMagic,
    ModerateWildMagic moderateWildMagic,
    StrongWildMagic strongWildMagic,
    EpicWildMagic epicWildMagic)
{
    public readonly struct WildMagicEffect(string name, Action<NwCreature, NwCreature, int, byte> effect)
    {
        public readonly string Name = name;
        public readonly Action<NwCreature, NwCreature, int, byte> Effect = effect;
    }

    public readonly WildMagicEffect[] AdverseEffects =
    [
        new("Polymorph", adverseWildMagic.Polymorph),
        new("Internal Confusion", adverseWildMagic.InternalConfusion),
        new("Trade Places", adverseWildMagic.TradePlaces),
        new("Heal (Not That One!)", adverseWildMagic.HealNotThatOne),
        new("Restoration (Not That One!)", adverseWildMagic.RestorationNotThatOne),
        new("Self-Immolation", adverseWildMagic.SelfImmolation),
        new("Death Armor (Not That One!)", adverseWildMagic.DeathArmorNotThatOne),
        new("Self-Inflict Wounds", adverseWildMagic.SelfInflictWounds),
        new("Stasis", adverseWildMagic.Stasis)
    ];

    public readonly WildMagicEffect[] WeakEffects =
    [
        new("Flare", weakWildMagic.Flare),
        new("Bane", weakWildMagic.Bane),
        new("Doom", weakWildMagic.Doom),
        new("Death Armor", weakWildMagic.DeathArmor),
        new("Electric Jolt", weakWildMagic.ElectricJolt),
        new("Sanctuary", weakWildMagic.Sanctuary),
        new("Silence", weakWildMagic.Silence),
        new("Invisibility", weakWildMagic.Invisibility),
        new("Combust", weakWildMagic.Combust),
        new("Charm Monster", weakWildMagic.CharmMonster),
        new("Inflict Light Wounds", weakWildMagic.InflictLightWounds),
        new("Cure Light Wounds", weakWildMagic.CureLightWounds),
        new("Lesser Restoration", weakWildMagic.LesserRestoration),
        new("Shelgarn's Persistent Blade", weakWildMagic.ShelgarnsPersistentBlade)
    ];

    public readonly WildMagicEffect[] ModerateEffects =
    [
        new("Magic Missile", moderateWildMagic.MagicMissile),
        new("Healing Sting", moderateWildMagic.HealingSting),
        new("Inflict Critical Wounds", moderateWildMagic.InflictCriticalWounds),
        new("Invisibility Sphere", moderateWildMagic.InvisibilitySphere),
        new("Circle of Death", moderateWildMagic.CircleOfDeath),
        new("Cure Critical Wounds", moderateWildMagic.CureCriticalWounds),
        new("Restoration", moderateWildMagic.Restoration),
        new("Polymorph Foe", moderateWildMagic.PolymorphFoe),
        new("Sound Burst", moderateWildMagic.SoundBurst),
        new("Morndenkainen's Sword", moderateWildMagic.MordenkainensSword),
        new("Gedlee's Electric Loop", moderateWildMagic.GedleesElectricLoop),
        new("Blindness/Deafness", moderateWildMagic.BlindnessDeafness),
        new("Scare", moderateWildMagic.Scare),
        new("Hold Monster", moderateWildMagic.HoldMonster)
    ];

    public readonly WildMagicEffect[] StrongEffects =
    [
        new("Web", strongWildMagic.Web),
        new("Gust of Wind", strongWildMagic.GustOfWind),
        new("Confusion", strongWildMagic.Confusion),
        new("Negative Energy Burst", strongWildMagic.NegativeEnergyBurst),
        new("Call Lightning", strongWildMagic.CallLightning),
        new("Scintillating Sphere", strongWildMagic.ScintillatingSphere),
        new("Fireball", strongWildMagic.Fireball),
        new("Slow", strongWildMagic.Slow),
        new("Greater Planar Binding", strongWildMagic.GreaterPlanarBinding),
        new("Bigby's Interposing Hand", strongWildMagic.BigbysInterposingHand),
        new("Mass Blindness/Deafness", strongWildMagic.MassBlindnessDeafness),
        new("Mass Polymorph", strongWildMagic.MassPolymorph),
        new("Isaac's Lesser Missile Storm", strongWildMagic.IsaacsLesserMissileStorm)
    ];

    public readonly WildMagicEffect[] EpicEffects =
    [
        new("Time Stop", epicWildMagic.TimeStop),
        new("Great Thunderclap", epicWildMagic.GreatThunderclap),
        new("Hammer of the Gods", epicWildMagic.HammerOfTheGods),
        new("Firestorm", epicWildMagic.Firestorm),
        new("Earthquake", epicWildMagic.Earthquake),
        new("Sunburst", epicWildMagic.Sunburst),
        new("Meteor Storm", epicWildMagic.MeteorStorm),
        new("Black Blade of Disaster", epicWildMagic.BlackBladeOfDisaster),
        new("Isaac's Greater Missile Storm", epicWildMagic.IsaacsGreaterMissileStorm)
    ];
}
