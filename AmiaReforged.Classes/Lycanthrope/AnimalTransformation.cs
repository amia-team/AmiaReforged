using AmiaReforged.Classes.EffectUtils.Polymorph;
using Anvil.API;

namespace AmiaReforged.Classes.Lycanthrope;

public static class AnimalTransformation
{
    private record AnimalForm(int Appearance, int Portrait);

    private static readonly Dictionary<int, AnimalForm> FeatToAnimalForm = new()
    {
        { PolymorphMapping.LycanShape.Feats.Wolf,    new AnimalForm(1616, 319) },
        { PolymorphMapping.LycanShape.Feats.Bear,    new AnimalForm(1502, 148) },
        { PolymorphMapping.LycanShape.Feats.Boar,    new AnimalForm(1814, 152) },
        { PolymorphMapping.LycanShape.Feats.Rat,     new AnimalForm(1257, 602) },
        { PolymorphMapping.LycanShape.Feats.Cat,     new AnimalForm(1504, 165) },
        { PolymorphMapping.LycanShape.Feats.Bat,     new AnimalForm(1087, 145) },
        { PolymorphMapping.LycanShape.Feats.Chicken, new AnimalForm(1580, 168) },
        { PolymorphMapping.LycanShape.Feats.Owl,     new AnimalForm(1569, 1448) },
        { PolymorphMapping.LycanShape.Feats.Croc,    new AnimalForm(1280, 1394) },
        { PolymorphMapping.LycanShape.Feats.Shark,   new AnimalForm(1871, 731) },
        { PolymorphMapping.LycanShape.Feats.Fox,     new AnimalForm(1844, 1473) },
        { PolymorphMapping.LycanShape.Feats.Raccoon, new AnimalForm(1064, 1402) }
    };

    public static void DoAnimalTransformation(NwCreature creature, int formFeat)
    {
        if (!FeatToAnimalForm.TryGetValue(formFeat, out AnimalForm? formData))
            return;

        CreatureSize originalSize = creature.Size;
        MovementRate originalMovement = creature.MovementRate;

        creature.Appearance = NwGameTables.AppearanceTable[formData.Appearance];
        creature.PortraitId = NwGameTables.PortraitTable[formData.Portrait];
        creature.Size = originalSize;
        creature.MovementRate = originalMovement;
    }
}
