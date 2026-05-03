using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.EffectUtils.ChangeAppearance;

public static class ChangeAppearanceUtils
{
    private const string PcKeyResRef = "ds_pckey";
    private const string OriginalAppearancePrefix = "appearance_original_";
    private const string ChangeAppearanceActiveVariable = "effect_change_appearance_active";
    private const string OriginalAppearanceBool = OriginalAppearancePrefix + "bool";

    public static bool HasActiveChangeAppearance(NwCreature creature)
        => creature.GetObjectVariable<LocalVariableBool>(ChangeAppearanceActiveVariable).Value;

    public static void SetChangeAppearanceActive(NwCreature creature)
        => creature.GetObjectVariable<LocalVariableBool>(ChangeAppearanceActiveVariable).Value = true;

    public static void SetChangeAppearanceInactive(NwCreature creature)
        => creature.GetObjectVariable<LocalVariableBool>(ChangeAppearanceActiveVariable).Delete();

    private static NwGameObject? GetOriginalAppearanceStorageObject(NwCreature creature, bool warnPlayer = false)
    {
        if (!creature.IsPlayerControlled)
            return creature;

        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == PcKeyResRef);
        if (pcKey != null)
            return pcKey;

        if (warnPlayer)
            creature.ControllingPlayer?.SendServerMessage("You need to have a PC key to store original appearance data.");

        return null;
    }

    /// <summary>
    /// Stores the original appearance data of the creature.
    /// For player-controlled creatures, data is stored on the PC key.
    /// For non-player creatures, data is stored on the creature itself.
    /// </summary>
    /// <returns>True if stored, otherwise false.</returns>
    public static bool StoreOriginalAppearance(NwCreature creature)
    {
        if (HasActiveChangeAppearance(creature))
            return true;

        NwGameObject? objectToStore = GetOriginalAppearanceStorageObject(creature, warnPlayer: true);
        if (objectToStore == null) return false;

        SetInt(nameof(ChangeAppearanceData.GenderId), (int)creature.Gender);
        SetInt(nameof(ChangeAppearanceData.AppearanceId), creature.Appearance.RowIndex);
        SetInt(nameof(ChangeAppearanceData.PhenotypeId), (int)creature.Phenotype);
        SetInt(nameof(ChangeAppearanceData.HeadId), creature.GetCreatureBodyPart(CreaturePart.Head));
        SetInt(nameof(ChangeAppearanceData.NeckId), creature.GetCreatureBodyPart(CreaturePart.Neck));
        SetInt(nameof(ChangeAppearanceData.TorsoId), creature.GetCreatureBodyPart(CreaturePart.Torso));
        SetInt(nameof(ChangeAppearanceData.LeftBicepId), creature.GetCreatureBodyPart(CreaturePart.LeftBicep));
        SetInt(nameof(ChangeAppearanceData.RightBicepId), creature.GetCreatureBodyPart(CreaturePart.RightBicep));
        SetInt(nameof(ChangeAppearanceData.LeftForearmId), creature.GetCreatureBodyPart(CreaturePart.LeftForearm));
        SetInt(nameof(ChangeAppearanceData.RightForearmId), creature.GetCreatureBodyPart(CreaturePart.RightForearm));
        SetInt(nameof(ChangeAppearanceData.LeftHandId), creature.GetCreatureBodyPart(CreaturePart.LeftHand));
        SetInt(nameof(ChangeAppearanceData.RightHandId), creature.GetCreatureBodyPart(CreaturePart.RightHand));
        SetInt(nameof(ChangeAppearanceData.LeftThighId), creature.GetCreatureBodyPart(CreaturePart.LeftThigh));
        SetInt(nameof(ChangeAppearanceData.RightThighId), creature.GetCreatureBodyPart(CreaturePart.RightThigh));
        SetInt(nameof(ChangeAppearanceData.LeftShinId), creature.GetCreatureBodyPart(CreaturePart.LeftShin));
        SetInt(nameof(ChangeAppearanceData.RightShinId), creature.GetCreatureBodyPart(CreaturePart.RightShin));
        SetInt(nameof(ChangeAppearanceData.LeftFootId), creature.GetCreatureBodyPart(CreaturePart.LeftFoot));
        SetInt(nameof(ChangeAppearanceData.RightFootId), creature.GetCreatureBodyPart(CreaturePart.RightFoot));
        SetInt(nameof(ChangeAppearanceData.WingsId), (int)creature.WingType);
        SetInt(nameof(ChangeAppearanceData.TailId), (int)creature.TailType);
        SetInt(nameof(ChangeAppearanceData.SkinColorId), creature.GetColor(ColorChannel.Skin));
        SetInt(nameof(ChangeAppearanceData.HairColorId), creature.GetColor(ColorChannel.Hair));
        SetInt(nameof(ChangeAppearanceData.TattooColorOneId), creature.GetColor(ColorChannel.Tattoo1));
        SetInt(nameof(ChangeAppearanceData.TattooColorTwoId), creature.GetColor(ColorChannel.Tattoo2));
        SetFloat(nameof(ChangeAppearanceData.Scale), creature.VisualTransform.Scale);
        SetFloat(nameof(ChangeAppearanceData.WingsScale),
            creature.GetVisualTransform(ObjectVisualTransformDataScope.CreatureWings).Scale);
        SetFloat(nameof(ChangeAppearanceData.TailScale),
            creature.GetVisualTransform(ObjectVisualTransformDataScope.CreatureTail).Scale);

        objectToStore.GetObjectVariable<LocalVariableBool>(OriginalAppearanceBool).Value = true;

        return true;

        void SetFloat(string name, float value)
        {
            objectToStore.GetObjectVariable<LocalVariableFloat>(OriginalAppearancePrefix + name).Value = value;
        }

        void SetInt(string name, int value)
        {
            objectToStore.GetObjectVariable<LocalVariableInt>(OriginalAppearancePrefix + name).Value = value + 1;
        }
    }

    /// <summary>
    /// Gets the original appearance of the creature before the new appearance is set.
    /// For player-controlled creatures, data is read from the PC key.
    /// For non-player creatures, data is read from the creature itself.
    /// </summary>
    /// <returns>The original appearance data, or null if not found.</returns>
    public static ChangeAppearanceData? GetOriginalAppearance(NwCreature creature)
    {
        NwGameObject? objectToRead = GetOriginalAppearanceStorageObject(creature);
        if (objectToRead == null) return null;

        // If the original appearance hasn't been stored, return null.
        if (!objectToRead.GetObjectVariable<LocalVariableBool>(OriginalAppearanceBool).Value)
            return null;

        return new ChangeAppearanceData(
            GenderId: GetInt(nameof(ChangeAppearanceData.GenderId)),
            AppearanceId: GetInt(nameof(ChangeAppearanceData.AppearanceId)),
            PhenotypeId: GetInt(nameof(ChangeAppearanceData.PhenotypeId)),
            HeadId: GetInt(nameof(ChangeAppearanceData.HeadId)),
            NeckId: GetInt(nameof(ChangeAppearanceData.NeckId)),
            TorsoId: GetInt(nameof(ChangeAppearanceData.TorsoId)),
            LeftBicepId: GetInt(nameof(ChangeAppearanceData.LeftBicepId)),
            RightBicepId: GetInt(nameof(ChangeAppearanceData.RightBicepId)),
            LeftForearmId: GetInt(nameof(ChangeAppearanceData.LeftForearmId)),
            RightForearmId: GetInt(nameof(ChangeAppearanceData.RightForearmId)),
            LeftHandId: GetInt(nameof(ChangeAppearanceData.LeftHandId)),
            RightHandId: GetInt(nameof(ChangeAppearanceData.RightHandId)),
            LeftThighId: GetInt(nameof(ChangeAppearanceData.LeftThighId)),
            RightThighId: GetInt(nameof(ChangeAppearanceData.RightThighId)),
            LeftShinId: GetInt(nameof(ChangeAppearanceData.LeftShinId)),
            RightShinId: GetInt(nameof(ChangeAppearanceData.RightShinId)),
            LeftFootId: GetInt(nameof(ChangeAppearanceData.LeftFootId)),
            RightFootId: GetInt(nameof(ChangeAppearanceData.RightFootId)),
            WingsId: GetInt(nameof(ChangeAppearanceData.WingsId)),
            TailId: GetInt(nameof(ChangeAppearanceData.TailId)),
            SkinColorId: GetInt(nameof(ChangeAppearanceData.SkinColorId)),
            HairColorId: GetInt(nameof(ChangeAppearanceData.HairColorId)),
            TattooColorOneId: GetInt(nameof(ChangeAppearanceData.TattooColorOneId)),
            TattooColorTwoId: GetInt(nameof(ChangeAppearanceData.TattooColorTwoId)),
            Scale: GetFloat(nameof(ChangeAppearanceData.Scale)),
            WingsScale: GetFloat(nameof(ChangeAppearanceData.WingsScale)),
            TailScale: GetFloat(nameof(ChangeAppearanceData.TailScale))
        );

        int? GetInt(string name)
        {
            LocalVariableInt variable = objectToRead.GetObjectVariable<LocalVariableInt>(OriginalAppearancePrefix + name);
            return variable.HasValue ? variable.Value - 1 : null;
        }

        float? GetFloat(string name)
        {
            LocalVariableFloat variable = objectToRead.GetObjectVariable<LocalVariableFloat>(OriginalAppearancePrefix + name);
            return variable.HasValue ? variable.Value : null;
        }
    }

    /// <summary>
    /// Sets the creature's appearance to the specified appearance data.
    /// </summary>
    /// <param name="creature">Creature whose appearance to set</param>
    /// <param name="appearance">The appearance data, you need some way of getting this</param>
    public static void SetAppearance(NwCreature creature, ChangeAppearanceData appearance)
    {
        if (appearance.GenderId is { } genderId)
            creature.Gender = (Gender)genderId;

        if (appearance.AppearanceId is { } appearanceId)
            creature.Appearance = NwGameTables.AppearanceTable.GetRow(appearanceId);

        if (appearance.PhenotypeId is { } phenotypeId)
            creature.Phenotype = (Phenotype)phenotypeId;

        if (appearance.HeadId is { } headId)
            creature.SetCreatureBodyPart(CreaturePart.Head, headId);

        if (appearance.NeckId is { } neckId)
            creature.SetCreatureBodyPart(CreaturePart.Neck, neckId);

        if (appearance.TorsoId is { } chestId)
            creature.SetCreatureBodyPart(CreaturePart.Torso, chestId);

        if (appearance.LeftBicepId is { } leftBicepId)
            creature.SetCreatureBodyPart(CreaturePart.LeftBicep, leftBicepId);

        if (appearance.RightBicepId is { } rightBicepId)
            creature.SetCreatureBodyPart(CreaturePart.RightBicep, rightBicepId);

        if (appearance.LeftForearmId is { } leftForearmId)
            creature.SetCreatureBodyPart(CreaturePart.LeftForearm, leftForearmId);

        if (appearance.RightForearmId is { } rightForearmId)
            creature.SetCreatureBodyPart(CreaturePart.RightForearm, rightForearmId);

        if (appearance.LeftHandId is { } leftHandId)
            creature.SetCreatureBodyPart(CreaturePart.LeftHand, leftHandId);

        if (appearance.RightHandId is { } rightHandId)
            creature.SetCreatureBodyPart(CreaturePart.RightHand, rightHandId);

        if (appearance.LeftThighId is { } leftThighId)
            creature.SetCreatureBodyPart(CreaturePart.LeftThigh, leftThighId);

        if (appearance.RightThighId is { } rightThighId)
            creature.SetCreatureBodyPart(CreaturePart.RightThigh, rightThighId);

        if (appearance.LeftShinId is { } leftShinId)
            creature.SetCreatureBodyPart(CreaturePart.LeftShin, leftShinId);

        if (appearance.RightShinId is { } rightShinId)
            creature.SetCreatureBodyPart(CreaturePart.RightShin, rightShinId);

        if (appearance.LeftFootId is { } leftFootId)
            creature.SetCreatureBodyPart(CreaturePart.LeftFoot, leftFootId);

        if (appearance.RightFootId is { } rightFootId)
            creature.SetCreatureBodyPart(CreaturePart.RightFoot, rightFootId);

        if (appearance.WingsId is { } wingsId)
            creature.WingType = (CreatureWingType)wingsId;

        if (appearance.TailId is { } tailId)
            creature.TailType = (CreatureTailType)tailId;

        if (appearance.Scale is { } scale)
            creature.VisualTransform.Scale = scale;

        if (appearance.WingsScale is { } wingsScale)
            NWScript.SetObjectVisualTransform(creature,
                nScope: NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_WINGS,
                nTransform: NWScript.OBJECT_VISUAL_TRANSFORM_SCALE,
                fValue: wingsScale);

        if (appearance.TailScale is { } tailScale)
            NWScript.SetObjectVisualTransform(creature,
                nScope: NWScript.OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_CREATURE_TAIL,
                nTransform: NWScript.OBJECT_VISUAL_TRANSFORM_SCALE,
                fValue: tailScale);

        // Sooo, when body part changes are involved, changes must be made with 130 millisecond delay cos NWN dunno
        TimeSpan delay = TimeSpan.FromMilliseconds(130);
        _ = ApplyColor();

        return;

        async Task ApplyColor()
        {
            await NwTask.Delay(delay);

            if (!creature.IsValid) return;

            if (appearance.SkinColorId is { } skinColorId)
                creature.SetColor(ColorChannel.Skin, skinColorId);

            if (appearance.HairColorId is { } hairColorId)
                creature.SetColor(ColorChannel.Hair, hairColorId);

            if (appearance.TattooColorOneId is { } tattooColorOneId)
                creature.SetColor(ColorChannel.Tattoo1, tattooColorOneId);

            if (appearance.TattooColorTwoId is { } tattooColorTwoId)
                creature.SetColor(ColorChannel.Tattoo2, tattooColorTwoId);
        }
    }

    /// <summary>
    /// Clears the original appearance data of the creature, this is to prevent stale data overwriting intended
    /// appearance changes.
    /// </summary>
    public static void ClearOriginalAppearance(NwCreature creature)
    {
        NwGameObject? objectToClear = GetOriginalAppearanceStorageObject(creature);
        if (objectToClear == null) return;

        DeleteInt(nameof(ChangeAppearanceData.GenderId));
        DeleteInt(nameof(ChangeAppearanceData.AppearanceId));
        DeleteInt(nameof(ChangeAppearanceData.PhenotypeId));
        DeleteInt(nameof(ChangeAppearanceData.HeadId));
        DeleteInt(nameof(ChangeAppearanceData.NeckId));
        DeleteInt(nameof(ChangeAppearanceData.TorsoId));
        DeleteInt(nameof(ChangeAppearanceData.LeftBicepId));
        DeleteInt(nameof(ChangeAppearanceData.RightBicepId));
        DeleteInt(nameof(ChangeAppearanceData.LeftForearmId));
        DeleteInt(nameof(ChangeAppearanceData.RightForearmId));
        DeleteInt(nameof(ChangeAppearanceData.LeftHandId));
        DeleteInt(nameof(ChangeAppearanceData.RightHandId));
        DeleteInt(nameof(ChangeAppearanceData.LeftThighId));
        DeleteInt(nameof(ChangeAppearanceData.RightThighId));
        DeleteInt(nameof(ChangeAppearanceData.LeftShinId));
        DeleteInt(nameof(ChangeAppearanceData.RightShinId));
        DeleteInt(nameof(ChangeAppearanceData.LeftFootId));
        DeleteInt(nameof(ChangeAppearanceData.RightFootId));
        DeleteInt(nameof(ChangeAppearanceData.WingsId));
        DeleteInt(nameof(ChangeAppearanceData.TailId));
        DeleteInt(nameof(ChangeAppearanceData.SkinColorId));
        DeleteInt(nameof(ChangeAppearanceData.HairColorId));
        DeleteInt(nameof(ChangeAppearanceData.TattooColorOneId));
        DeleteInt(nameof(ChangeAppearanceData.TattooColorTwoId));
        DeleteFloat(nameof(ChangeAppearanceData.Scale));
        DeleteFloat(nameof(ChangeAppearanceData.WingsScale));
        DeleteFloat(nameof(ChangeAppearanceData.TailScale));

        objectToClear.GetObjectVariable<LocalVariableBool>(OriginalAppearanceBool).Delete();
        return;

        void DeleteInt(string name)
        {
            objectToClear.GetObjectVariable<LocalVariableInt>(OriginalAppearancePrefix + name).Delete();
        }

        void DeleteFloat(string name)
        {
            objectToClear.GetObjectVariable<LocalVariableFloat>(OriginalAppearancePrefix + name).Delete();
        }
    }

    public static void PrintOriginalAppearanceData(NwCreature creature)
    {
        NwPlayer? player = creature.ControllingPlayer;
        if (player == null) return;

        ChangeAppearanceData? appearanceData = GetOriginalAppearance(creature);
        if (appearanceData == null)
        {
            player.SendServerMessage("No stored original pact appearance data found.");
            return;
        }

        player.SendServerMessage("Stored original pact appearance data:");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.GenderId)}: {appearanceData.GenderId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.AppearanceId)}: {appearanceData.AppearanceId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.PhenotypeId)}: {appearanceData.PhenotypeId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.HeadId)}: {appearanceData.HeadId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.NeckId)}: {appearanceData.NeckId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.TorsoId)}: {appearanceData.TorsoId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.LeftBicepId)}: {appearanceData.LeftBicepId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.RightBicepId)}: {appearanceData.RightBicepId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.LeftForearmId)}: {appearanceData.LeftForearmId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.RightForearmId)}: {appearanceData.RightForearmId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.LeftHandId)}: {appearanceData.LeftHandId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.RightHandId)}: {appearanceData.RightHandId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.LeftThighId)}: {appearanceData.LeftThighId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.RightThighId)}: {appearanceData.RightThighId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.LeftShinId)}: {appearanceData.LeftShinId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.RightShinId)}: {appearanceData.RightShinId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.WingsId)}: {appearanceData.WingsId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.TailId)}: {appearanceData.TailId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.SkinColorId)}: {appearanceData.SkinColorId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.HairColorId)}: {appearanceData.HairColorId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.TattooColorOneId)}: {appearanceData.TattooColorOneId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.TattooColorTwoId)}: {appearanceData.TattooColorTwoId}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.Scale)}: {appearanceData.Scale}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.WingsScale)}: {appearanceData.WingsScale}");
        player.SendServerMessage($"{nameof(ChangeAppearanceData.TailScale)}: {appearanceData.TailScale}");
    }
}
