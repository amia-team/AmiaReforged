using System.Text;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.WildMagic;

[ServiceBinding(typeof(WildMagicService))]
public class WildMagicService(WildMagicEffects wildMagicEffects)
{
    private static readonly (int weak, int moderate, int strong) DefaultChances = (66, 33, 0);

    // chances for weak, moderate, strong wild magic; this should always add up to 98,
    // leaving a 1% chance for adverse wild magic and 1% chance for epic wild magic
    private static readonly Dictionary<KiFocus, (int weak, int moderate, int strong)> KiFocusChances = new()
    {
        { KiFocus.KiFocus1, (56, 33, 9) },
        { KiFocus.KiFocus2, (43, 40, 15) },
        { KiFocus.KiFocus3, (32, 33, 33) },
    };

    private static readonly Color[] RainbowColors =
    [
        ColorConstants.Red, ColorConstants.Orange, ColorConstants.Yellow, ColorConstants.Green, ColorConstants.Teal,
        ColorConstants.Purple
    ];

    public void DoWildMagic(NwCreature monk, NwCreature targetCreature)
    {
        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);

        (int weak, int moderate, int strong) chances = kiFocus == null ? DefaultChances : KiFocusChances[kiFocus.Value];

        int d100Roll = Random.Shared.Roll(100);

        WildMagicEffects.WildMagicEffect[] randomWildMagicEffects = GetRandomWildMagicEffects(d100Roll, chances);

        WildMagicEffects.WildMagicEffect randomWildMagic = randomWildMagicEffects[Random.Shared.Next(randomWildMagicEffects.Length)];

        FloatWildMagicName(randomWildMagic.Name, monk);

        monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpMagblue, fScale: 0.5f));

        int dc = MonkUtils.CalculateMonkDc(monk);
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        randomWildMagic.Effect(monk, targetCreature, dc, monkLevel);
    }

    private WildMagicEffects.WildMagicEffect[] GetRandomWildMagicEffects(int d100Roll, (int weak, int moderate, int strong) chances)
    {
        return
            d100Roll <= chances.weak ? wildMagicEffects.WeakEffects :
            d100Roll <= chances.weak + chances.moderate ? wildMagicEffects.ModerateEffects :
            d100Roll <= chances.weak + chances.moderate + chances.strong ? wildMagicEffects.StrongEffects :
            d100Roll <= chances.weak + chances.moderate + chances.strong + 1 ? wildMagicEffects.EpicEffects :
            wildMagicEffects.AdverseEffects;
    }

    private static void FloatWildMagicName(string magicName, NwCreature monk)
    {
        StringBuilder rainbowName = new();
        int colorIndex = 0;

        foreach (char letter in magicName)
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

        monk.ControllingPlayer?.FloatingTextString($"{rainbowName}", false);
    }

}
