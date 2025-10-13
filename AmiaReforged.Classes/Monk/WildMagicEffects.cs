using System.Text;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WildMagicEffects
{
    private static readonly (int weak, int moderate, int strong) DefaultChances = (66, 33, 0);

    // chances for weak, moderate, strong spells; this should always add up to 99, leaving a 1% chance for epic spells
    private static readonly Dictionary<KiFocus, (int weak, int moderate, int strong)> KiFocusChances = new()
    {
        { KiFocus.KiFocus1, (57, 33, 9) },
        { KiFocus.KiFocus2, (44, 40, 15) },
        { KiFocus.KiFocus3, (33, 33, 33) },
    };

    private static readonly Color[] RainbowColors =
    [
        ColorConstants.Red, ColorConstants.Orange, ColorConstants.Yellow, ColorConstants.Green, ColorConstants.Navy,
        ColorConstants.Purple
    ];

    public static void DoWildMagic(NwCreature monk, NwCreature targetCreature)
    {
        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);

        (int weak, int moderate, int strong) chances = kiFocus == null ? DefaultChances : KiFocusChances[kiFocus.Value];

        int d100Roll = Random.Shared.Roll(100);

        Spell[] randomSpellList = GetRandomSpellArray(d100Roll, chances);

        NwSpell? randomSpell = NwSpell.FromSpellType(randomSpellList[Random.Shared.Next(randomSpellList.Length)]);
        if (randomSpell == null)
        {
            monk.ControllingPlayer?.SendServerMessage("Wild Magic spell not found!");
            return;
        }

        string spellName = randomSpell.Name.ToString();

        StringBuilder rainbowName = new();
        int colorIndex = 0;

        foreach (char letter in spellName)
        {
            if (char.IsWhiteSpace(letter))
            {
                rainbowName.Append(letter);
            }
            else
            {
                Color color = RainbowColors[colorIndex % RainbowColors.Length];

                rainbowName.Append(letter.ToString().ColorString(color));
                colorIndex++;
            }
        }

        monk.ControllingPlayer?.FloatingTextString($"{rainbowName}", false, false);

        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagblue, fScale: 0.5f));

        monk.ActionCastSpellAt(randomSpell, targetCreature, MetaMagic.None, true, instant: true);
    }

    private static Spell[] GetRandomSpellArray(int d100Roll, (int weak, int moderate, int strong) chances)
    {
        return
            d100Roll <= chances.weak ? WeakSpells :
            d100Roll <= chances.weak + chances.moderate ? ModerateSpells :
            d100Roll <= chances.weak + chances.moderate + chances.strong ? StrongSpells :
            EpicSpellArray;
    }

    private static readonly Spell[] WeakSpells =
    [
        Spell.Flare,
        Spell.Bane,
        Spell.GhoulTouch,
        Spell.Quillfire,
        Spell.MagicMissile,
        Spell.InfestationOfMaggots,
        Spell.Poison,
        Spell.Enervation
    ];

    private static readonly Spell[] ModerateSpells =
    [
        Spell.Doom,
        Spell.ColorSpray,
        Spell.RayOfEnfeeblement,
        Spell.TashasHideousLaughter,
        Spell.GedleesElectricLoop,
        Spell.GustOfWind,
        Spell.MindFog,
        Spell.EnergyDrain,
        Spell.HealingSting
    ];

    private static readonly Spell[] StrongSpells =
    [
        Spell.Web,
        Spell.Grease,
        Spell.Balagarnsironhorn,
        Spell.HoldMonster,
        Spell.NegativeEnergyBurst,
        Spell.Slow,
        Spell.BlindnessAndDeafness,
        Spell.PrismaticSpray,
        Spell.PowerWordStun,
        Spell.HammerOfTheGods,
        Spell.VampiricTouch
    ];

    private static readonly Spell[] EpicSpellArray =
    [
        Spell.TimeStop,
        Spell.MeteorSwarm,
        Spell.GreatThunderclap
    ];
}
