using Anvil.API;

namespace AmiaReforged.Classes.Associates;

public static class EpicCompanionAppearance
{
    private const string EpicCompanionAppearanceVar = "epic_companion_appearance";
    private static readonly NwFeat? EpicCompanionFeat = NwFeat.FromFeatId(1240);

    // Wyvern (Badger)
    private const string WyvernDescription = "Wyverns are vicious and deadly predators. Only an exceptional druid or ranger would be able to befriend one.";
    private const int WyvernAppearance = 458;

    // Displacer Beast (Wolf)
    private const string DisplacerBeastDescription =
    "Displacer Beasts are evil creatures that most don't live long enough to tell about. It would take a terrifyingly skilled individual to claim one as their companion.";
    private const int DisplacerBeastAppearance = 925;

    // Dire Bear (Bear)
    private const string DireBearDescription = "Dire Bears are larger, more vicious versions of their common cousins. They are often found in the company of powerful rangers or druids.";
    private const int DireBearAppearance = 1174;

    // Raptor (Boar)
    private const string RaptorDescription = "Quick, vicious and smart, Raptors make excellent companions as they do enjoy to hunt in company.";
    private const int RaptorAppearance = 1439;

    // Giant Eagle (Hawk)
    private const string GiantEagleDescription = "Giant Eagles are terrifying raptors that can lift a full grown deer off the ground with ease. They are smart, fast, and have talons the size of human forearms.";
    private const int GiantEagleAppearance = 914;

    // Male Lion (Panther)
    private const string MaleLionDescription = "Lions are one of the few social cats. Once their trust is earned they make excellent partners in hunts, and other coordinated activities.";
    private const int MaleLionAppearance = 967;

    // Giant Spider (Spider)
    private const string GiantSpiderDescription = "Large, hungry, and venomous, beware the Gargantuan Spider and the one skilled enough to tame it.";
    private const int GiantSpiderAppearance = 905;

    // Guardian Wolf (DireWolf)
    private const string GuardianWolfDescription = "Guardian Wolves are rare, massive, and intelligent super predators. They make indispensable lifelong companions.";
    private const int GuardianWolfAppearance = 1140;

    // Giant Scorpion (DireRat)
    private const string GiantScorpionDescription = "Giant Scorpions are armored beasts that kill, eat and do whatever they please. Stay away unless you are prepared to fight an almost unstoppable mass of armor, pincers, and stinger.";
    private const int GiantScorpionAppearance = 338;

    private record CompanionAppearanceData(string Description, int Appearance);

    public static bool ApplyEpicCompanionAppearance(NwCreature associate, AssociateType associateType, NwCreature owner, NwItem? pcKey)
    {
        if (associateType != AssociateType.AnimalCompanion) return false;
        if (pcKey is null) return false;
        if (EpicCompanionFeat == null || !owner.KnowsFeat(EpicCompanionFeat)) return false;

        LocalVariableInt epicCompanionAppearanceVar = pcKey.GetObjectVariable<LocalVariableInt>(EpicCompanionAppearanceVar);

        if (epicCompanionAppearanceVar.Value == 0) return false;

        CompanionAppearanceData appearanceData = owner.AnimalCompanionType switch
        {
            AnimalCompanionCreatureType.Badger => new CompanionAppearanceData(WyvernDescription, WyvernAppearance),
            AnimalCompanionCreatureType.Wolf => new CompanionAppearanceData(DisplacerBeastDescription, DisplacerBeastAppearance),
            AnimalCompanionCreatureType.Bear => new CompanionAppearanceData(DireBearDescription, DireBearAppearance),
            AnimalCompanionCreatureType.Boar => new CompanionAppearanceData(RaptorDescription, RaptorAppearance),
            AnimalCompanionCreatureType.Hawk => new CompanionAppearanceData(GiantEagleDescription, GiantEagleAppearance),
            AnimalCompanionCreatureType.Panther => new CompanionAppearanceData(MaleLionDescription, MaleLionAppearance),
            AnimalCompanionCreatureType.Spider => new CompanionAppearanceData(GiantSpiderDescription, GiantSpiderAppearance),
            AnimalCompanionCreatureType.DireWolf => new CompanionAppearanceData(GuardianWolfDescription, GuardianWolfAppearance),
            AnimalCompanionCreatureType.DireRat => new CompanionAppearanceData(GiantScorpionDescription, GiantScorpionAppearance),
            _ => new CompanionAppearanceData(associate.Description, 0)
        };

        switch (owner.AnimalCompanionType)
        {
            case AnimalCompanionCreatureType.Wolf: // Displacer Beast
                associate.ApplyEffect(EffectDuration.Permanent, Effect.VisualEffect(VfxType.DurProtShadowArmor));
                break;

            case AnimalCompanionCreatureType.Boar: // Raptor
                associate.VisualTransform.Scale = 0.6f;
                break;
        }

        AppearanceTableEntry appearanceEntry = NwGameTables.AppearanceTable.GetRow(appearanceData.Appearance);
        associate.Appearance = appearanceEntry;
        associate.Description = appearanceData.Description;
        associate.PortraitResRef = !string.IsNullOrEmpty(appearanceEntry.Portrait) ? appearanceEntry.Portrait : "po_clsranger_";

        return true;
    }
}


