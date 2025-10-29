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
        new("Hermitic Confusion", AdverseWildMagic.HermiticConfusion),
        new("Trade Places", AdverseWildMagic.TradePlaces),
        new("Heal (Not That One!)", adverseWildMagic.HealNotThatOne),
        new("Restoration (Not That One!)", AdverseWildMagic.RestorationNotThatOne),
        new("Self-Immolation", adverseWildMagic.SelfImmolation),
        new("Death Armor (Not That One!)", adverseWildMagic.DeathArmorNotThatOne),
        new("Self-Inflict Wounds", adverseWildMagic.SelfInflictWounds),
        new("Spontaneous Meditation", AdverseWildMagic.SpontaneousMeditation),
        new("Lightning Rod", AdverseWildMagic.LightningRod)
    ];

    public readonly WildMagicEffect[] WeakEffects =
    [
        new("Magic Missile", weakWildMagic.MagicMissile),
        new("Flare", weakWildMagic.Flare),
        new("Bane", weakWildMagic.Bane),
        new("Doom", weakWildMagic.Doom),
        new("Death Armor", weakWildMagic.DeathArmor),
        new("Electric Jolt", weakWildMagic.ElectricJolt),
        new("Silence", weakWildMagic.Silence),
        new("Combust", weakWildMagic.Combust),
        new("Charm Monster", weakWildMagic.CharmMonster),
        new("Inflict Light Wounds", weakWildMagic.InflictLightWounds),
        new("Cure Light Wounds", weakWildMagic.CureLightWounds),
        new("Lesser Restoration", weakWildMagic.LesserRestoration),
        new("Shelgarn's Persistent Blade", weakWildMagic.ShelgarnsPersistentBlade)
    ];

    public readonly WildMagicEffect[] ModerateEffects =
    [
        new("Isaac's Lesser Missile Storm", moderateWildMagic.IsaacsLesserMissileStorm),
        new("Healing Sting", moderateWildMagic.HealingSting),
        new("Inflict Critical Wounds", moderateWildMagic.InflictCriticalWounds),
        new("Concealment", moderateWildMagic.Concealment),
        new("Balagarn's Iron Horn", moderateWildMagic.BalagarnsIronHorn),
        new("Cure Critical Wounds", moderateWildMagic.CureCriticalWounds),
        new("Restoration", moderateWildMagic.Restoration),
        new("Baleful Polymorph", moderateWildMagic.BalefulPolymorph),
        new("Sound Burst", moderateWildMagic.SoundBurst),
        new("Morndenkainen's Sword", moderateWildMagic.MordenkainensSword),
        new("Gedlee's Electric Loop", moderateWildMagic.GedleesElectricLoop),
        new("Blindness/Deafness", moderateWildMagic.BlindnessDeafness),
        new("Hold Monster", moderateWildMagic.HoldMonster)
    ];

    public readonly WildMagicEffect[] StrongEffects =
    [
        new("Isaac's Greater Missile Storm", strongWildMagic.IsaacsGreaterMissileStorm),
        new("Web", strongWildMagic.Web),
        new("Gust of Wind", strongWildMagic.GustOfWind),
        new("Confusion", strongWildMagic.Confusion),
        new("Negative Energy Burst", strongWildMagic.NegativeEnergyBurst),
        new("Call Lightning", strongWildMagic.CallLightning),
        new("Fireball", strongWildMagic.Fireball),
        new("Slow", strongWildMagic.Slow),
        new("Greater Planar Binding", strongWildMagic.GreaterPlanarBinding),
        new("Bigby's Interposing Hand", strongWildMagic.BigbysInterposingHand),
        new("Mass Blindness/Deafness", strongWildMagic.MassBlindnessDeafness),
        new("Mass Polymorph", strongWildMagic.MassPolymorph)
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
        new("Black Blade of Disaster", epicWildMagic.BlackBladeOfDisaster)
    ];
}
