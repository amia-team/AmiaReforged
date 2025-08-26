using Anvil.API;

namespace AmiaReforged.Classes.Associates;

public static class CustomAppearance
{
    /// <summary>
    ///     When the associate is summoned, gets the copied appearance for the associate and customizes it.
    /// </summary>
    public static bool ApplyCustomAppearance(NwCreature associate, AssociateType associateType,
        NwItem? associateCustomizer)
    {
        if (associateCustomizer == null) return false;
        if (associateType == AssociateType.Dominated) return false;

        string associateResRef = associate.ResRef;

        // Animal companion and familiar has a unique resref each level so just nab the first chars
        if (associateType is AssociateType.AnimalCompanion or AssociateType.Familiar)
            associateResRef = associate.ResRef[..8];

        NwCreature? creatureCopy = GetCreatureCopy(associateCustomizer, associateResRef);
        if (creatureCopy == null) return false;

        ApplyCreatureAppearanceFromCopy(associate, creatureCopy);

        CreatureEquipment equipmentCopies = GetItemCopies(creatureCopy);

        ApplyEquipmentAppearance(associate, equipmentCopies);

        HideEquipment(associate, equipmentCopies);

        ApplyVfxFromCopy(associateCustomizer, associateResRef, associate);

        associateCustomizer.Description =
            associateType is AssociateType.AnimalCompanion or AssociateType.Familiar ?
            UpdateCompanionName(associateCustomizer.Description, associate, associateType, creatureCopy.Name)
            : UpdateAssociateName(associateCustomizer.Description, associate, creatureCopy.Name);

        return true;
    }

    private static void ApplyEquipmentAppearance(NwCreature associate, CreatureEquipment equipmentCopies)
    {
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
    private static string UpdateCompanionName(string customizerDescription, NwCreature associate,
        AssociateType associateType, string newName)
    {
        string companionType = associateType switch
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

        string prefix = associateType == AssociateType.AnimalCompanion ? "Companion" : "Familiar";
        string nameUpdate = $"{prefix} {companionType} is{newName}".ColorString(ColorConstants.Green);

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
            NwItem dummyOffhand = offHandCopy.Clone(associate, "dummy_offhand");
            dummyOffhand.RemoveItemProperties();
            dummyOffhand.Droppable = false;
            associate.RunEquip(dummyOffhand, InventorySlot.LeftHand);

            return;
        }

        if (associateOffHand.BaseItem.Category == BaseItemCategory.Shield)
        {
            associateOffHand.Appearance.SetSimpleModel(offHandCopy.Appearance.GetSimpleModel());
        }
        else
        {
            SetWeaponAppearance(associateOffHand, offHandCopy);
        }
    }

    /// <summary>
    /// Applies the main hand item appearance stored for the creature copy
    /// </summary>
    private static void ApplyMainHandAppearanceFromCopy(NwCreature associate, NwItem mainHandCopy)
    {
        NwItem? associateMainHand = associate.GetItemInSlot(InventorySlot.RightHand);
        if (associateMainHand == null) return;

        SetWeaponAppearance(associateMainHand, mainHandCopy);
    }

    private static void SetWeaponAppearance(NwItem associateWeapon, NwItem weaponCopy)
    {
        foreach (ItemAppearanceWeaponColor colorChannel in Enum.GetValues<ItemAppearanceWeaponColor>())
        {
            associateWeapon.Appearance.SetWeaponColor(colorChannel, weaponCopy.Appearance.GetWeaponColor(colorChannel));
        }

        foreach (ItemAppearanceWeaponModel modelPart in Enum.GetValues<ItemAppearanceWeaponModel>())
        {
            associateWeapon.Appearance.SetWeaponModel(modelPart, weaponCopy.Appearance.GetWeaponModel(modelPart));
        }

        ItemProperty? offHandCopyVfx = weaponCopy.ItemProperties.FirstOrDefault(itemVisual =>
            itemVisual.Property.PropertyType == ItemPropertyType.VisualEffect);

        if (offHandCopyVfx != null)
            associateWeapon.AddItemProperty(offHandCopyVfx, EffectDuration.Permanent);

        if (offHandCopyVfx == null)
            associateWeapon.RemoveItemProperties(ItemPropertyType.VisualEffect);

        associateWeapon.VisualTransform.Scale = weaponCopy.VisualTransform.Scale;
        associateWeapon.VisualTransform.Rotation = weaponCopy.VisualTransform.Rotation;
        associateWeapon.VisualTransform.AnimSpeed = weaponCopy.VisualTransform.AnimSpeed;
        associateWeapon.VisualTransform.Translation = weaponCopy.VisualTransform.Translation;
    }

    /// <summary>
    /// Applies the cloak appearance stored for the creature copy
    /// </summary>
    private static void ApplyCloakAppearanceFromCopy(NwCreature associate, NwItem cloakCopy)
    {
        NwItem? associateCloak = associate.GetItemInSlot(InventorySlot.Cloak);

        if (associateCloak != null)
        {
            foreach (ItemAppearanceArmorColor colorChannel in Enum.GetValues<ItemAppearanceArmorColor>())
            {
                associateCloak.Appearance.SetArmorColor(colorChannel, cloakCopy.Appearance.GetArmorColor(colorChannel));
            }

            associateCloak.Appearance.SetSimpleModel(cloakCopy.Appearance.GetSimpleModel());

            associateCloak.VisualTransform.Scale = cloakCopy.VisualTransform.Scale;
            associateCloak.VisualTransform.Rotation = cloakCopy.VisualTransform.Rotation;
            associateCloak.VisualTransform.AnimSpeed = cloakCopy.VisualTransform.AnimSpeed;
            associateCloak.VisualTransform.Translation = cloakCopy.VisualTransform.Translation;
        }
        else
        {
            NwItem dummyCloak = cloakCopy.Clone(associate, "dummy_cloak");
            dummyCloak.RemoveItemProperties();
            dummyCloak.Droppable = false;
            associate.RunEquip(dummyCloak, InventorySlot.Cloak);
        }
    }

    /// <summary>
    /// Applies the helmet appearance stored for the creature copy
    /// </summary>
    private static void ApplyHelmetAppearanceFromCopy(NwCreature associate, NwItem helmetCopy)
    {
        NwItem? associateHelmet = associate.GetItemInSlot(InventorySlot.Head);

        if (associateHelmet != null)
        {
            foreach (ItemAppearanceArmorColor colorChannel in Enum.GetValues<ItemAppearanceArmorColor>())
            {
                associateHelmet.Appearance.SetArmorColor(colorChannel, helmetCopy.Appearance.GetArmorColor(colorChannel));
            }

            associateHelmet.Appearance.SetSimpleModel(helmetCopy.Appearance.GetSimpleModel());

            associateHelmet.VisualTransform.Scale = helmetCopy.VisualTransform.Scale;
            associateHelmet.VisualTransform.Rotation = helmetCopy.VisualTransform.Rotation;
            associateHelmet.VisualTransform.AnimSpeed = helmetCopy.VisualTransform.AnimSpeed;
            associateHelmet.VisualTransform.Translation = helmetCopy.VisualTransform.Translation;
        }
        else
        {
            NwItem dummyHelmet = helmetCopy.Clone(associate, "dummy_helmet");
            dummyHelmet.RemoveItemProperties();
            dummyHelmet.Droppable = false;
            associate.RunEquip(dummyHelmet, InventorySlot.Head);
        }
    }

    /// <summary>
    /// Applies the armor appearance stored for the creature copy
    /// </summary>
    private static void ApplyArmorAppearanceFromCopy(NwCreature associate, NwItem armorCopy)
    {
        NwItem? associateArmor = associate.GetItemInSlot(InventorySlot.Chest);

        if (associateArmor != null)
        {
            // Apply colors
            foreach (ItemAppearanceArmorColor colorChannel in Enum.GetValues<ItemAppearanceArmorColor>())
            {
                associateArmor.Appearance.SetArmorColor(colorChannel, armorCopy.Appearance.GetArmorColor(colorChannel));
            }

            // Apply models
            foreach (CreaturePart modelPart in Enum.GetValues<CreaturePart>())
            {
                associateArmor.Appearance.SetArmorModel(modelPart, armorCopy.Appearance.GetArmorModel(modelPart));
            }

            associateArmor.VisualTransform.Scale = armorCopy.VisualTransform.Scale;
            associateArmor.VisualTransform.Rotation = armorCopy.VisualTransform.Rotation;
            associateArmor.VisualTransform.AnimSpeed = armorCopy.VisualTransform.AnimSpeed;
            associateArmor.VisualTransform.Translation = armorCopy.VisualTransform.Translation;
        }
        else
        {
            NwItem dummyArmor = armorCopy.Clone(associate, "dummy_armor");
            dummyArmor.RemoveItemProperties();
            dummyArmor.Droppable = false;
            associate.RunEquip(dummyArmor, InventorySlot.Chest);
        }
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

    /// <summary>
    /// Gets the item copies stored in the Associate Customizer and assigned to the associate
    /// </summary>
    /// <returns>Copies of the items; null if no item data stored by that var name is found</returns>
    private static CreatureEquipment GetItemCopies(NwCreature creatureCopy)
    {
        return new CreatureEquipment
        {
            Armor = creatureCopy.GetItemInSlot(InventorySlot.Chest),
            Helmet = creatureCopy.GetItemInSlot(InventorySlot.Head),
            Cloak = creatureCopy.GetItemInSlot(InventorySlot.Cloak),
            MainHand = creatureCopy.GetItemInSlot(InventorySlot.RightHand),
            OffHand = creatureCopy.GetItemInSlot(InventorySlot.LeftHand)
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

        // Apply body parts
        foreach (CreaturePart creaturePart in Enum.GetValues<CreaturePart>())
        {
            associate.SetCreatureBodyPart(creaturePart, creatureCopy.GetCreatureBodyPart(creaturePart));
        }

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
