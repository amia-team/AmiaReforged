using Anvil.API;

namespace AmiaReforged.Classes.Associates;

public static class CustomAppearance
{
    /// <summary>
    ///     When the associate is summoned, gets the copied appearance for the associate and customizes it.
    /// </summary>
    public static bool ApplyCustomAppearance(NwCreature associate, NwItem? associateCustomizer)
    {
        if (associateCustomizer == null) return false;
        if (associate.AssociateType == AssociateType.Dominated) return false;

        string associateResRef = associate.ResRef;

        // Animal companion and familiar has a unique resref each level so just nab the first chars
        if (associate.AssociateType is AssociateType.AnimalCompanion or AssociateType.Familiar)
            associateResRef = associate.ResRef[..8];

        NwCreature? creatureCopy = GetCreatureCopy(associateCustomizer, associateResRef);
        if (creatureCopy == null) return false;

        ApplyCreatureAppearanceFromCopy(associate, creatureCopy);

        CreatureEquipment equipmentCopies = GetItemCopies(associateCustomizer, associateResRef);

        // Hide helmet, cloak, and shield if the copied creature has none but the associate does
        HideEquipment(associate, equipmentCopies);

        if (equipmentCopies.Armor != null)
            ApplyArmorAppearanceFromCopy(associate, equipmentCopies.Armor);

        if (equipmentCopies.Helmet != null)
            ApplyHelmetAppearanceFromCopy(associate, equipmentCopies.Helmet);

        if (equipmentCopies.Cloak != null)
            ApplyCloakAppearanceFromCopy(associate, equipmentCopies.Cloak);

        if (equipmentCopies.MainHand != null)
            ApplyMainHandAppearanceFromCopy(associate, equipmentCopies.MainHand);

        if (equipmentCopies.OffHand != null)
            ApplyOffHandAppearanceFromCopy(associate, equipmentCopies.OffHand);

        ApplyVfxFromCopy(associateCustomizer, associateResRef, associate);

        associateCustomizer.Description =
            associate.AssociateType is AssociateType.AnimalCompanion or AssociateType.Familiar ?
            UpdateCompanionName(associateCustomizer.Description, associate, creatureCopy.Name)
            : UpdateAssociateName(associateCustomizer.Description, associate, creatureCopy.Name);

        return true;
    }

    /// <summary>
    /// Updates the description to include information about the customized associate
    /// </summary>
    private static string UpdateAssociateName(string customizerDescription, NwCreature associate, string newName)
    {
        if (customizerDescription.Contains(associate.OriginalName)) return customizerDescription;

        string nameUpdate = $"{associate.OriginalName}is {newName}".ColorString(ColorConstants.Green);

        return $"{nameUpdate}\n\n{customizerDescription}";
    }

    /// <summary>
    /// Updates the description to include information about the customized animal companion or familiar
    /// </summary>
    private static string UpdateCompanionName(string customizerDescription, NwCreature associate, string newName)
    {
        string companionType = associate.AssociateType switch
        {
            AssociateType.AnimalCompanion => associate.AnimalCompanionType switch
            {
                AnimalCompanionCreatureType.Badger => "badger",
                AnimalCompanionCreatureType.Bear => "bear",
                AnimalCompanionCreatureType.Boar => "boar",
                AnimalCompanionCreatureType.DireRat => "dire rat",
                AnimalCompanionCreatureType.DireWolf => "dire wolf",
                AnimalCompanionCreatureType.Hawk => "hawk",
                AnimalCompanionCreatureType.Panther => "panther",
                AnimalCompanionCreatureType.Spider => "spider",
                AnimalCompanionCreatureType.Wolf => "wolf",
                _ => "unknown"
            },
            AssociateType.Familiar => associate.FamiliarType switch
            {
                FamiliarCreatureType.Bat => "bat",
                FamiliarCreatureType.Eyeball => "eyeball",
                FamiliarCreatureType.CragCat => "panther",
                FamiliarCreatureType.FairyDragon => "faerie dragon",
                FamiliarCreatureType.FireMephit => "fire mephit",
                FamiliarCreatureType.HellHound => "hell hound",
                FamiliarCreatureType.IceMephit => "ice mephit",
                FamiliarCreatureType.Imp => "imp",
                FamiliarCreatureType.Pixie => "pixie",
                FamiliarCreatureType.PseudoDragon => "pseudodragon",
                FamiliarCreatureType.Raven => "raven",
                _ when associate.ResRef.Contains("const") => "construct",
                _ when associate.ResRef.Contains("phase") => "phase spider",
                _ when associate.ResRef.Contains("skele") => "skeleton",
                _ => "unknown"
            },
            _ => "unknown"
        };

        if (customizerDescription.Contains(companionType) &&
            customizerDescription.Contains(newName)) return customizerDescription;

        string prefix = associate.AssociateType == AssociateType.AnimalCompanion ? "Companion" : "Familiar";
        string nameUpdate = $"{prefix} {companionType} is {newName}".ColorString(ColorConstants.Green);

        return $"{nameUpdate}\n\n{customizerDescription}";
    }

    /// <summary>
    /// Applies the visuals for the creature copy
    /// </summary>
    private static void ApplyVfxFromCopy(NwItem associateCustomizer, string associateResRef, NwCreature associate)
    {
        int vfxCount = associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount"+associateResRef).Value;

        if (vfxCount == 0) return;

        for (int i = 0; i < vfxCount; i++)
        {
            int vfxId = associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i+associateResRef).Value;
            Effect vfx = Effect.VisualEffect((VfxType)vfxId);
            vfx.SubType = EffectSubType.Supernatural;
            associate.ApplyEffect(EffectDuration.Permanent, vfx);
        }
    }

    /// <summary>
    /// Applies the off hand item appearance stored for the creature copy
    /// </summary>
    private static void ApplyOffHandAppearanceFromCopy(NwCreature associate, NwItem offHandCopy)
    {
        NwItem? associateOffHand = associate.GetItemInSlot(InventorySlot.LeftHand);

        if (associateOffHand == null)
        {
            offHandCopy.Clone(associate, "dummy_offhand");
            NwItem dummyOffhand = associate.Inventory.Items.First(item => item.Tag == "dummy_offhand");
            dummyOffhand.RemoveItemProperties();
            dummyOffhand.Droppable = false;
            associate.RunEquip(dummyOffhand, InventorySlot.LeftHand);
        }

        if (associateOffHand == null) return;

        if (associateOffHand.BaseItem.Category == BaseItemCategory.Shield)
        {
            associateOffHand.Appearance.SetSimpleModel(offHandCopy.Appearance.GetSimpleModel());
        }
        else
        {
            associateOffHand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Bottom, offHandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Bottom));
            associateOffHand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Middle, offHandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Middle));
            associateOffHand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Top, offHandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Top));
            associateOffHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, offHandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom));
            associateOffHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, offHandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle));
            associateOffHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, offHandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top));

            ItemProperty? offHandCopyVfx = offHandCopy.ItemProperties.FirstOrDefault(itemVisual =>
                itemVisual.Property.PropertyType == ItemPropertyType.VisualEffect);

            if (offHandCopyVfx != null)
                associateOffHand.AddItemProperty(offHandCopyVfx, EffectDuration.Permanent);

            if (offHandCopyVfx == null)
                associateOffHand.RemoveItemProperties(ItemPropertyType.VisualEffect);
        }
    }

    /// <summary>
    /// Applies the main hand item appearance stored for the creature copy
    /// </summary>
    private static void ApplyMainHandAppearanceFromCopy(NwCreature associate, NwItem mainHandCopy)
    {
        NwItem? associateMainHand = associate.GetItemInSlot(InventorySlot.RightHand);
        if (associateMainHand == null) return;

        associateMainHand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Bottom, mainHandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Bottom));
        associateMainHand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Middle, mainHandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Middle));
        associateMainHand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Top, mainHandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Top));
        associateMainHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, mainHandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom));
        associateMainHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, mainHandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle));
        associateMainHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, mainHandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top));

        ItemProperty? mainHandCopyVfx = mainHandCopy.ItemProperties.FirstOrDefault(ip =>
            ip.Property.PropertyType == ItemPropertyType.VisualEffect);

        if (mainHandCopyVfx != null)
            associateMainHand.AddItemProperty(mainHandCopyVfx, EffectDuration.Permanent);

        if (mainHandCopyVfx == null)
            associateMainHand.RemoveItemProperties(ItemPropertyType.VisualEffect);
    }

    /// <summary>
    /// Applies the cloak appearance stored for the creature copy
    /// </summary>
    private static void ApplyCloakAppearanceFromCopy(NwCreature associate, NwItem cloakCopy)
    {
        NwItem? associateCloak = associate.GetItemInSlot(InventorySlot.Cloak);

        if (associateCloak != null)
        {

            associateCloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
            associateCloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
            associateCloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
            associateCloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
            associateCloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
            associateCloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
            associateCloak.Appearance.SetSimpleModel(cloakCopy.Appearance.GetSimpleModel());
        }

        if (associateCloak != null) return;

        NwItem dummyCloak = cloakCopy.Clone(associate, "dummy_cloak");
        dummyCloak.RemoveItemProperties();
        dummyCloak.Droppable = false;
        associate.RunEquip(dummyCloak, InventorySlot.Cloak);
    }

    /// <summary>
    /// Applies the helmet appearance stored for the creature copy
    /// </summary>
    private static void ApplyHelmetAppearanceFromCopy(NwCreature associate, NwItem helmetCopy)
    {
        NwItem? associateHelmet = associate.GetItemInSlot(InventorySlot.Head);

        if (associateHelmet != null)
        {

            associateHelmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
            associateHelmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
            associateHelmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
            associateHelmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
            associateHelmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
            associateHelmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
            associateHelmet.Appearance.SetSimpleModel(helmetCopy.Appearance.GetSimpleModel());
        }

        if (associate.GetItemInSlot(InventorySlot.Head) != null) return;

        NwItem dummyHelmet = helmetCopy.Clone(associate, "dummy_helmet");
        dummyHelmet.RemoveItemProperties();
        dummyHelmet.Droppable = false;
        associate.RunEquip(dummyHelmet, InventorySlot.Head);
    }

    /// <summary>
    /// Applies the armor appearance stored for the creature copy
    /// </summary>
    private static void ApplyArmorAppearanceFromCopy(NwCreature associate, NwItem armorCopy)
    {
        NwItem? associateArmor = associate.GetItemInSlot(InventorySlot.Chest);

        if (associateArmor != null)
        {
            associateArmor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
            associateArmor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
            associateArmor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
            associateArmor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
            associateArmor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
            associateArmor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
            associateArmor.Appearance.SetArmorModel(CreaturePart.Belt, armorCopy.Appearance.GetArmorModel(CreaturePart.Belt));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftBicep, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftBicep));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftFoot, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftFoot));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftForearm, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftForearm));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftHand, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftHand));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftShin, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftShin));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftShoulder, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftShoulder));
            associateArmor.Appearance.SetArmorModel(CreaturePart.LeftThigh, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftThigh));
            associateArmor.Appearance.SetArmorModel(CreaturePart.Neck, armorCopy.Appearance.GetArmorModel(CreaturePart.Neck));
            associateArmor.Appearance.SetArmorModel(CreaturePart.Pelvis, armorCopy.Appearance.GetArmorModel(CreaturePart.Pelvis));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightBicep, armorCopy.Appearance.GetArmorModel(CreaturePart.RightBicep));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightFoot, armorCopy.Appearance.GetArmorModel(CreaturePart.RightFoot));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightForearm, armorCopy.Appearance.GetArmorModel(CreaturePart.RightForearm));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightHand, armorCopy.Appearance.GetArmorModel(CreaturePart.RightHand));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightShin, armorCopy.Appearance.GetArmorModel(CreaturePart.RightShin));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightShoulder, armorCopy.Appearance.GetArmorModel(CreaturePart.RightShoulder));
            associateArmor.Appearance.SetArmorModel(CreaturePart.RightThigh, armorCopy.Appearance.GetArmorModel(CreaturePart.RightThigh));
            associateArmor.Appearance.SetArmorModel(CreaturePart.Robe, armorCopy.Appearance.GetArmorModel(CreaturePart.Robe));
            associateArmor.Appearance.SetArmorModel(CreaturePart.Torso, armorCopy.Appearance.GetArmorModel(CreaturePart.Torso));
        }

        if (associate.GetItemInSlot(InventorySlot.Chest) != null) return;

        NwItem dummyArmor = armorCopy.Clone(associate, "dummy_armor");
        dummyArmor.RemoveItemProperties();
        dummyArmor.Droppable = false;
        associate.RunEquip(dummyArmor, InventorySlot.Chest);
    }

    /// <summary>
    /// Hides helmet, cloak, and offhand item if the associate has them but the copied creature doesn't
    /// </summary>
    private static void HideEquipment(NwCreature associate, CreatureEquipment equipmentCopies)
    {
        NwItem? associateHelm = associate.GetItemInSlot(InventorySlot.Head);
        NwItem? associateCloak = associate.GetItemInSlot(InventorySlot.Cloak);
        NwItem? associateOffHand = associate.GetItemInSlot(InventorySlot.LeftHand);

        if (equipmentCopies.Helmet == null && associateHelm != null)
            associateHelm.HiddenWhenEquipped = 1;

        if (equipmentCopies.Cloak == null && associateCloak != null)
            associateCloak.HiddenWhenEquipped = 1;

        if (equipmentCopies.OffHand == null && associateOffHand != null)
            associateOffHand.HiddenWhenEquipped = 1;
    }

    private class CreatureEquipment
    {
        public NwItem? Armor { get; init; }
        public NwItem? Helmet { get; init; }
        public NwItem? Cloak { get; init; }
        public NwItem? MainHand { get; init; }
        public NwItem? OffHand { get; init; }
    }

    private static NwItem? GetItemFromVariable(NwItem associateCustomizer, string variableName)
    {
        string? itemData = associateCustomizer.GetObjectVariable<LocalVariableString>(variableName).Value;

        if (itemData == null) return null;

        byte[] convertedData = Convert.FromBase64String(itemData);

        return NwItem.Deserialize(convertedData);
    }

    /// <summary>
    /// Gets the item copies stored in the Associate Customizer and assigned to the associate
    /// </summary>
    /// <returns>Copies of the items; null if no item data stored by that var name is found</returns>
    private static CreatureEquipment GetItemCopies(NwItem associateCustomizer, string associateResRef)
    {
        return new CreatureEquipment
        {
            Armor = GetItemFromVariable(associateCustomizer, "armor" + associateResRef),
            Helmet = GetItemFromVariable(associateCustomizer, "helmet" + associateResRef),
            Cloak = GetItemFromVariable(associateCustomizer, "cloak" + associateResRef),
            MainHand = GetItemFromVariable(associateCustomizer, "mainhand" + associateResRef),
            OffHand = GetItemFromVariable(associateCustomizer, "offhand" + associateResRef)
        };
    }

    /// <summary>
    /// Applies appearance, soundset, footstep etc. appearance-related variables while retaining original associate's
    /// size and movement
    /// </summary>
    private static void ApplyCreatureAppearanceFromCopy(NwCreature associate, NwCreature creatureCopy)
    {
        MovementRate originalMovement = associate.MovementRate;
        CreatureSize originalSize = associate.Size;
        associate.Appearance = creatureCopy.Appearance;
        associate.MovementRate = originalMovement;
        associate.Name = creatureCopy.Name;
        associate.Size = originalSize;
        associate.PortraitResRef = creatureCopy.PortraitResRef;
        associate.SoundSet = creatureCopy.SoundSet;
        associate.FootstepType = creatureCopy.FootstepType;
        associate.Gender = creatureCopy.Gender;
        associate.Phenotype = creatureCopy.Phenotype;
        associate.Description = creatureCopy.Description;
        associate.VisualTransform.Rotation = creatureCopy.VisualTransform.Rotation;
        associate.VisualTransform.Scale = creatureCopy.VisualTransform.Scale;
        associate.VisualTransform.Translation = creatureCopy.VisualTransform.Translation;
        associate.VisualTransform.AnimSpeed = creatureCopy.VisualTransform.AnimSpeed;
        associate.TailType = creatureCopy.TailType;
        associate.WingType = creatureCopy.WingType;
        associate.SetCreatureBodyPart(CreaturePart.Belt, creatureCopy.GetCreatureBodyPart(CreaturePart.Belt));
        associate.SetCreatureBodyPart(CreaturePart.Head, creatureCopy.GetCreatureBodyPart(CreaturePart.Head));
        associate.SetCreatureBodyPart(CreaturePart.LeftBicep, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftBicep));
        associate.SetCreatureBodyPart(CreaturePart.LeftFoot, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftFoot));
        associate.SetCreatureBodyPart(CreaturePart.LeftForearm, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftForearm));
        associate.SetCreatureBodyPart(CreaturePart.LeftHand, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftHand));
        associate.SetCreatureBodyPart(CreaturePart.LeftShin, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftShin));
        associate.SetCreatureBodyPart(CreaturePart.LeftShoulder, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftShoulder));
        associate.SetCreatureBodyPart(CreaturePart.LeftThigh, creatureCopy.GetCreatureBodyPart(CreaturePart.LeftThigh));
        associate.SetCreatureBodyPart(CreaturePart.Neck, creatureCopy.GetCreatureBodyPart(CreaturePart.Neck));
        associate.SetCreatureBodyPart(CreaturePart.Pelvis, creatureCopy.GetCreatureBodyPart(CreaturePart.Pelvis));
        associate.SetCreatureBodyPart(CreaturePart.RightBicep, creatureCopy.GetCreatureBodyPart(CreaturePart.RightBicep));
        associate.SetCreatureBodyPart(CreaturePart.RightFoot, creatureCopy.GetCreatureBodyPart(CreaturePart.RightFoot));
        associate.SetCreatureBodyPart(CreaturePart.RightForearm, creatureCopy.GetCreatureBodyPart(CreaturePart.RightForearm));
        associate.SetCreatureBodyPart(CreaturePart.RightHand, creatureCopy.GetCreatureBodyPart(CreaturePart.RightHand));
        associate.SetCreatureBodyPart(CreaturePart.RightShin, creatureCopy.GetCreatureBodyPart(CreaturePart.RightShin));
        associate.SetCreatureBodyPart(CreaturePart.RightShoulder, creatureCopy.GetCreatureBodyPart(CreaturePart.RightShoulder));
        associate.SetCreatureBodyPart(CreaturePart.RightThigh, creatureCopy.GetCreatureBodyPart(CreaturePart.RightThigh));
        associate.SetCreatureBodyPart(CreaturePart.Robe, creatureCopy.GetCreatureBodyPart(CreaturePart.Robe));
        associate.SetCreatureBodyPart(CreaturePart.Torso, creatureCopy.GetCreatureBodyPart(CreaturePart.Torso));
        associate.SetColor(ColorChannel.Hair, creatureCopy.GetColor(ColorChannel.Hair));
        associate.SetColor(ColorChannel.Skin, creatureCopy.GetColor(ColorChannel.Skin));
        associate.SetColor(ColorChannel.Tattoo1, creatureCopy.GetColor(ColorChannel.Tattoo1));
        associate.SetColor(ColorChannel.Tattoo2, creatureCopy.GetColor(ColorChannel.Tattoo2));
    }

    /// <summary>
    /// Gets the creature copy stored in the Associate Customizer and assigned to the associate
    /// </summary>
    /// <returns>A copy of the creature; null if no creature data stored by that var name is found</returns>
    private static NwCreature? GetCreatureCopy(NwItem associateCustomizer, string associateResRef)
    {
        string? creatureData = associateCustomizer.GetObjectVariable<LocalVariableString>("creature"+associateResRef).Value;
        if (creatureData == null) return null;

        byte[] convertedCreatureData = Convert.FromBase64String(creatureData);

        NwCreature? creatureCopy = NwCreature.Deserialize(convertedCreatureData);

        return creatureCopy == null ? null : creatureCopy;
    }
}
