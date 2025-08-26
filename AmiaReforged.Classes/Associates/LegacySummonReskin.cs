using Anvil.API;

namespace AmiaReforged.Classes.Associates;

public static class LegacySummonReskin
{
    public static bool ApplySummonReskin(NwCreature associate, AssociateType associateType, NwItem? reskinWidget)
    {
        if (reskinWidget == null)
            return false;

        string summonResRef = associateType switch
        {
            AssociateType.AnimalCompanion => "companion",
            AssociateType.Familiar => "familiar",
            _ => associate.ResRef
        };

        LocalVariableInt widgetVersion = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_adv");

        bool ignoreName = associate.AssociateType switch
        {
            AssociateType.AnimalCompanion or AssociateType.Familiar => false,
            _ => true
        };

        bool reskinApplied = widgetVersion.Value switch
        {
            0 => SetBasicAppearance(associate, summonResRef, reskinWidget, ignoreName),
            1 => SetAdvancedAppearance(associate, summonResRef, reskinWidget, ignoreName),
            _ => false
        };

        return reskinApplied;
    }

    private static bool SetBasicAppearance(NwCreature associate, string summonResRef, NwItem reskinWidget, bool ignoreName)
    {
        int newAppearance = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_a").Value;
        if (newAppearance == 0) return false;

        int newPortrait = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_p").Value;
        int newTail = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_t").Value;
        string? newName = reskinWidget.GetObjectVariable<LocalVariableString>(summonResRef + "_n").Value;

        associate.Appearance = NwGameTables.AppearanceTable[newAppearance];
        associate.PortraitId = NwGameTables.PortraitTable[newPortrait];
        associate.TailType = (CreatureTailType)newTail;
        if (newName != null && ignoreName == false) associate.Name = newName;

        SetVfx(associate, reskinWidget);

        return true;
    }

    private static bool SetAdvancedAppearance(NwCreature associate, string summonResRef, NwItem reskinWidget, bool ignoreName)
    {
        int newAppearance = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_app").Value;
        if (newAppearance == 0) return false;

        int newTail = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_tail").Value;
        int newWing = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_wing").Value;
        float newScale = reskinWidget.GetObjectVariable<LocalVariableFloat>(summonResRef + "_scale").Value;
        string? newPortrait = reskinWidget.GetObjectVariable<LocalVariableString>(summonResRef + "_port").Value;
        string? newDescription = reskinWidget.GetObjectVariable<LocalVariableString>(summonResRef + "_bio").Value;
        string? newName = reskinWidget.GetObjectVariable<LocalVariableString>(summonResRef + "_name").Value;

        associate.Appearance = NwGameTables.AppearanceTable[newAppearance];
        associate.TailType = (CreatureTailType)newTail;
        associate.WingType = (CreatureWingType)newWing;
        if (newScale != 0.0f)  associate.VisualTransform.Scale = newScale;
        if (newPortrait != null) associate.PortraitResRef = newPortrait;
        if (newDescription != null) associate.Description = newDescription;
        if (newName != null && ignoreName == false) associate.Name = newName;

        SetSkinBits(associate, reskinWidget, summonResRef);

        SetColorBits(associate, reskinWidget, summonResRef);

        SetVfx(associate, reskinWidget);

        return true;
    }

    private static void SetVfx(NwCreature associate, NwItem reskinWidget)
    {
        List<string> vfxVariables =
        [
            "summon_vfx",
            "summon_vfx2",
            "summon_vfx3"
        ];

        foreach (string varName in vfxVariables)
        {
            LocalVariableInt vfxVar = reskinWidget.GetObjectVariable<LocalVariableInt>(varName);

            if (vfxVar.HasNothing) continue;

            Effect vfx = Effect.VisualEffect((VfxType)vfxVar.Value);
            vfx.SubType = EffectSubType.Unyielding;
            associate.ApplyEffect(EffectDuration.Permanent, vfx);
        }
    }

    private static void SetColorBits(NwCreature associate, NwItem reskinWidget, string summonResRef)
    {
        SetColorFromWidget(associate, reskinWidget, summonResRef + "_c_skin", ColorChannel.Skin);
        SetColorFromWidget(associate, reskinWidget, summonResRef + "_c_hair", ColorChannel.Hair);
        SetColorFromWidget(associate, reskinWidget, summonResRef + "_c_tat1", ColorChannel.Tattoo1);
        SetColorFromWidget(associate, reskinWidget, summonResRef + "_c_tat2", ColorChannel.Tattoo2);
    }

    private static void SetColorFromWidget(NwCreature associate, NwItem reskinWidget, string variableName, ColorChannel colorChannel)
    {
        LocalVariableInt colorVar = reskinWidget.GetObjectVariable<LocalVariableInt>(variableName);
        if (colorVar.HasNothing) return;

        int colorIndex = colorVar.Value;
        if (colorIndex is >= 0 and < 176)
        {
            associate.SetColor(colorChannel, colorIndex);
        }
    }

    private static void SetSkinBits(NwCreature associate, NwItem reskinWidget, string summonResRef)
    {
        const int maxParts = 21;
        for (int i = 0; i < maxParts; i++)
        {
            int partNumber = reskinWidget.GetObjectVariable<LocalVariableInt>(summonResRef + "_p_" + i).Value;

            if (partNumber > 0 && partNumber != associate.GetCreatureBodyPart((CreaturePart)i))
                associate.SetCreatureBodyPart((CreaturePart)i, partNumber);
        }
    }
}
