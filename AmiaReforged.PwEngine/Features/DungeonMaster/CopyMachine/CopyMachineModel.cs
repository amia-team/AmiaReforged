using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.CopyMachine;

internal sealed class CopyMachineModel
{
    private readonly NwPlayer _player;

    public NwObject? Source { get; private set; }
    private bool _copyEquipmentEnabled;

    public delegate void SelectionChangedHandler();

    public event SelectionChangedHandler? OnSelectionChanged;

    public CopyMachineModel(NwPlayer player)
    {
        _player = player;
    }

    public string GetStatusText()
    {
        if (Source == null)
            return "No source selected.";

        var type = GetTypeString();

        return $"Source: {Source.Name} ({type})";
    }

    public string GetTypeString()
    {
        string type = Source switch
        {
            NwCreature => "Creature",
            NwPlaceable => "Placeable",
            NwItem => "Item",
            _ => "Object"
        };
        return type;
    }

    public void EnterSourceTargetingMode()
    {
        _player.EnterTargetMode(OnSourceSelected, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable | ObjectTypes.Item
        });
    }

    private void OnSourceSelected(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject == null)
        {
            _player.SendServerMessage("No valid object selected.", ColorConstants.Red);
            return;
        }

        if (obj.TargetObject is not NwCreature and not NwPlaceable and not NwItem)
        {
            _player.SendServerMessage("You can only select a creature, placeable, or item.", ColorConstants.Red);
            return;
        }

        Source = obj.TargetObject;
        _player.SendServerMessage($"Source set to: {Source.Name}", ColorConstants.Green);
        OnSelectionChanged?.Invoke();
    }

    public void SetCopyEquipmentFlag(bool enabled)
    {
        _copyEquipmentEnabled = enabled;
    }

    public void EnterCopyTargetingMode()
    {
        if (Source == null)
        {
            _player.SendServerMessage("Select a source object first.", ColorConstants.Red);
            return;
        }

        if (Source is NwItem)
        {
            _player.SendServerMessage(
                "Select an item to overwrite its appearance, or select yourself to clone the item into your inventory.",
                ColorConstants.Green);
            _player.EnterTargetMode(OnCopyTargetSelected, new TargetModeSettings
            {
                ValidTargets = ObjectTypes.Item | ObjectTypes.Creature
            });
        }
        else
        {
            _player.EnterTargetMode(OnCopyTargetSelected, new TargetModeSettings
            {
                ValidTargets = ObjectTypes.Creature | ObjectTypes.Placeable
            });
        }
    }

    private void OnCopyTargetSelected(ModuleEvents.OnPlayerTarget obj)
    {
        if (Source == null)
        {
            _player.SendServerMessage("Source is no longer valid. Select a new source.", ColorConstants.Red);
            return;
        }

        if (obj.TargetObject == null)
        {
            _player.SendServerMessage("No valid target selected.", ColorConstants.Red);
            return;
        }

        // Item source
        if (Source is NwItem sourceItem)
        {
            // Item → Creature: clone into inventory (DM's own creature only)
            if (obj.TargetObject is NwCreature targetCreature)
            {
                NwCreature? dmCreature = _player.LoginCreature ?? _player.ControlledCreature;
                if (dmCreature == null || targetCreature != dmCreature)
                {
                    _player.SendServerMessage("You can only clone items into your own inventory.", ColorConstants.Red);
                    return;
                }

                NwItem? copy = sourceItem.Clone(targetCreature);
                if (copy == null)
                {
                    _player.SendServerMessage("Failed to copy the item.", ColorConstants.Red);
                    return;
                }

                _player.SendServerMessage(
                    $"Copied '{sourceItem.Name}' into your inventory.", ColorConstants.Green);
                return;
            }

            // Item → Item: copy appearance
            if (obj.TargetObject is NwItem targetItem)
            {
                if (sourceItem == targetItem)
                {
                    _player.SendServerMessage("Source and target are the same item.", ColorConstants.Red);
                    return;
                }

                if (sourceItem.BaseItem.ItemType != targetItem.BaseItem.ItemType)
                {
                    _player.SendServerMessage(
                        $"Base item type mismatch: source is {sourceItem.BaseItem.Name} but target is {targetItem.BaseItem.Name}.",
                        ColorConstants.Red);
                    return;
                }

                CopyItemAppearance(sourceItem, targetItem);
                _player.SendServerMessage(
                    $"Appearance copied from '{sourceItem.Name}' to '{targetItem.Name}'.", ColorConstants.Green);
                return;
            }

            _player.SendServerMessage("You must select an item or a creature.", ColorConstants.Red);
            return;
        }

        if (obj.TargetObject is not NwCreature and not NwPlaceable)
        {
            _player.SendServerMessage("You can only target a creature or a placeable.", ColorConstants.Red);
            return;
        }

        // Check type match
        if (Source is NwCreature && obj.TargetObject is not NwCreature)
        {
            _player.SendServerMessage("Source is a creature but target is not. Both must be the same type.",
                ColorConstants.Red);
            return;
        }

        if (Source is NwPlaceable && obj.TargetObject is not NwPlaceable)
        {
            _player.SendServerMessage("Source is a placeable but target is not. Both must be the same type.",
                ColorConstants.Red);
            return;
        }

        // Can't copy to itself
        if (Source == obj.TargetObject)
        {
            _player.SendServerMessage("Source and target are the same object.", ColorConstants.Red);
            return;
        }

        NwObject target = obj.TargetObject;
        string targetName = target.Name;

        if (Source is NwCreature sourceCreature && target is NwCreature targetCr)
        {
            CopyCreatureAppearance(sourceCreature, targetCr);

            // If equipment copying is enabled AND source is a creature, copy equipment after appearance
            if (_copyEquipmentEnabled && Source is NwCreature)
            {
                CopyCreatureEquipment(sourceCreature, targetCr);
            }
        }
        else if (Source is NwPlaceable sourcePlc && target is NwPlaceable targetPlc)
        {
            CopyPlaceableAppearance(sourcePlc, targetPlc);
        }

        _player.SendServerMessage($"Appearance copied from {Source.Name} to {targetName}.", ColorConstants.Green);

        // Reset equipment flag after copying
        _copyEquipmentEnabled = false;
    }

    // ───────────────────────────── Creature Copying ─────────────────────────────

    private void CopyCreatureAppearance(NwCreature source, NwCreature target)
    {
        // Name & Description
        if (target.Name != source.Name)
            target.Name = source.Name;

        if (target.Description != source.Description)
            target.Description = source.Description;

        // Gender
        int srcGender = NWScript.GetGender(source);
        if (NWScript.GetGender(target) != srcGender)
            NWScript.SetGender(target, srcGender);

        // Appearance type
        int srcAppearance = NWScript.GetAppearanceType(source);
        if (NWScript.GetAppearanceType(target) != srcAppearance)
            NWScript.SetCreatureAppearanceType(target, srcAppearance);

        // Soundset
        int srcSoundset = NWScript.GetSoundset(source);
        if (NWScript.GetSoundset(target) != srcSoundset)
            NWScript.SetSoundset(target, srcSoundset);

        // Portrait
        if (target.PortraitResRef != source.PortraitResRef)
            target.PortraitResRef = source.PortraitResRef;

        // Wings
        int srcWings = NWScript.GetCreatureWingType(source);
        if (NWScript.GetCreatureWingType(target) != srcWings)
            NWScript.SetCreatureWingType(srcWings, target);

        // Tail
        int srcTail = NWScript.GetCreatureTailType(source);
        if (NWScript.GetCreatureTailType(target) != srcTail)
            NWScript.SetCreatureTailType(srcTail, target);

        // All body parts
        CopyBodyParts(source, target);

        // Colors: Skin, Hair, Tattoo1, Tattoo2
        CopyColorChannel(source, target, NWScript.COLOR_CHANNEL_SKIN);
        CopyColorChannel(source, target, NWScript.COLOR_CHANNEL_HAIR);
        CopyColorChannel(source, target, NWScript.COLOR_CHANNEL_TATTOO_1);
        CopyColorChannel(source, target, NWScript.COLOR_CHANNEL_TATTOO_2);

        // Visual Transform — Scale and Z translation only
        if (Math.Abs(target.VisualTransform.Scale - source.VisualTransform.Scale) > 0.001f)
            target.VisualTransform.Scale = source.VisualTransform.Scale;

        if (Math.Abs(target.VisualTransform.Translation.Z - source.VisualTransform.Translation.Z) > 0.001f)
        {
            System.Numerics.Vector3 t = target.VisualTransform.Translation;
            target.VisualTransform.Translation = new System.Numerics.Vector3(t.X, t.Y, source.VisualTransform.Translation.Z);
        }

        // Equipment appearance for visible slots
        CopyEquipmentSlot(source, target, InventorySlot.Chest);
        CopyEquipmentSlot(source, target, InventorySlot.Head);
        CopyEquipmentSlot(source, target, InventorySlot.Cloak);
        CopyEquipmentSlot(source, target, InventorySlot.RightHand);
        CopyEquipmentSlot(source, target, InventorySlot.LeftHand);
    }

    private static void CopyBodyParts(NwCreature source, NwCreature target)
    {
        int[] bodyParts =
        [
            NWScript.CREATURE_PART_HEAD,
            NWScript.CREATURE_PART_RIGHT_FOOT,
            NWScript.CREATURE_PART_LEFT_FOOT,
            NWScript.CREATURE_PART_RIGHT_SHIN,
            NWScript.CREATURE_PART_LEFT_SHIN,
            NWScript.CREATURE_PART_RIGHT_THIGH,
            NWScript.CREATURE_PART_LEFT_THIGH,
            NWScript.CREATURE_PART_PELVIS,
            NWScript.CREATURE_PART_TORSO,
            NWScript.CREATURE_PART_BELT,
            NWScript.CREATURE_PART_NECK,
            NWScript.CREATURE_PART_RIGHT_FOREARM,
            NWScript.CREATURE_PART_LEFT_FOREARM,
            NWScript.CREATURE_PART_RIGHT_BICEP,
            NWScript.CREATURE_PART_LEFT_BICEP,
            NWScript.CREATURE_PART_RIGHT_SHOULDER,
            NWScript.CREATURE_PART_LEFT_SHOULDER,
            NWScript.CREATURE_PART_RIGHT_HAND,
            NWScript.CREATURE_PART_LEFT_HAND
        ];

        foreach (int part in bodyParts)
        {
            int srcVal = NWScript.GetCreatureBodyPart(part, source);
            if (NWScript.GetCreatureBodyPart(part, target) != srcVal)
                NWScript.SetCreatureBodyPart(part, srcVal, target);
        }
    }

    private static void CopyColorChannel(NwCreature source, NwCreature target, int channel)
    {
        int srcColor = NWScript.GetColor(source, channel);
        if (NWScript.GetColor(target, channel) != srcColor)
            NWScript.SetColor(target, channel, srcColor);
    }

    // ───────────────────────────── Equipment Copying ─────────────────────────────

    private static void CopyEquipmentSlot(NwCreature source, NwCreature target, InventorySlot slot)
    {
        NwItem? srcItem = source.GetItemInSlot(slot);
        NwItem? tgtItem = target.GetItemInSlot(slot);

        // Both must have an item and same base item type
        if (srcItem == null || tgtItem == null) return;
        if (srcItem.BaseItem.ItemType != tgtItem.BaseItem.ItemType) return;

        // Copy item name and description
        if (tgtItem.Name != srcItem.Name)
            tgtItem.Name = srcItem.Name;
        if (tgtItem.Description != srcItem.Description)
            tgtItem.Description = srcItem.Description;

        if (slot == InventorySlot.Chest)
        {
            CopyArmorAppearance(srcItem, tgtItem);
        }
        else if (slot == InventorySlot.Head || slot == InventorySlot.Cloak)
        {
            CopySimpleItemAppearance(srcItem, tgtItem);
        }
        else
        {
            // Weapons: RightHand / LeftHand
            CopyWeaponAppearance(srcItem, tgtItem);
        }
    }

    /// <summary>
    /// Copies armor appearance: all armor model parts and all color channels.
    /// </summary>
    private static void CopyArmorAppearance(NwItem source, NwItem target)
    {
        // Copy armor models for every creature part
        foreach (CreaturePart part in Enum.GetValues<CreaturePart>())
        {
            ushort srcModel = source.Appearance.GetArmorModel(part);
            if (target.Appearance.GetArmorModel(part) != srcModel)
                target.Appearance.SetArmorModel(part, (byte)srcModel);
        }

        // Copy armor colors for every color channel
        foreach (ItemAppearanceArmorColor color in Enum.GetValues<ItemAppearanceArmorColor>())
        {
            byte srcColor = source.Appearance.GetArmorColor(color);
            if (target.Appearance.GetArmorColor(color) != srcColor)
                target.Appearance.SetArmorColor(color, srcColor);
        }
    }

    /// <summary>
    /// Copies appearance for simple-model items (helmets, cloaks): simple model number and colors.
    /// </summary>
    private static void CopySimpleItemAppearance(NwItem source, NwItem target)
    {
        ushort srcModel = source.Appearance.GetSimpleModel();
        if (target.Appearance.GetSimpleModel() != srcModel)
            target.Appearance.SetSimpleModel(srcModel);

        // Copy all 6 armor color channels (used for tinting simple items too)
        foreach (ItemAppearanceArmorColor color in Enum.GetValues<ItemAppearanceArmorColor>())
        {
            byte srcColor = source.Appearance.GetArmorColor(color);
            if (target.Appearance.GetArmorColor(color) != srcColor)
                target.Appearance.SetArmorColor(color, srcColor);
        }
    }

    /// <summary>
    /// Copies weapon appearance. For complex (parts-based) weapons: top, middle, bottom models.
    /// For simple-model weapons: the simple model number. Also copies colors.
    /// </summary>
    private static void CopyWeaponAppearance(NwItem source, NwItem target)
    {
        bool isSimple = source.BaseItem.ModelType == BaseItemModelType.Simple;

        if (isSimple)
        {
            ushort srcModel = source.Appearance.GetSimpleModel();
            if (target.Appearance.GetSimpleModel() != srcModel)
                target.Appearance.SetSimpleModel(srcModel);
        }
        else
        {
            // Complex weapon — top, middle, bottom
            ushort srcTop = source.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
            if (target.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top) != srcTop)
                target.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)srcTop);

            ushort srcMid = source.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            if (target.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle) != srcMid)
                target.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)srcMid);

            ushort srcBot = source.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
            if (target.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom) != srcBot)
                target.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)srcBot);
        }

        // Copy weapon color channels
        foreach (ItemAppearanceArmorColor color in Enum.GetValues<ItemAppearanceArmorColor>())
        {
            byte srcColor = source.Appearance.GetArmorColor(color);
            if (target.Appearance.GetArmorColor(color) != srcColor)
                target.Appearance.SetArmorColor(color, srcColor);
        }
    }

    // ───────────────────────────── Item-to-Item Copying ─────────────────────────────

    /// <summary>
    /// Copies appearance from one item to another of the same base item type.
    /// Handles armor (model parts + colors), simple-model items (icon + colors),
    /// and complex weapons (top/mid/bottom + colors). Also copies name and description.
    /// </summary>
    private static void CopyItemAppearance(NwItem source, NwItem target)
    {
        // Name & Description
        if (target.Name != source.Name)
            target.Name = source.Name;
        if (target.Description != source.Description)
            target.Description = source.Description;

        // Determine item category based on base item type
        BaseItemType baseType = source.BaseItem.ItemType;

        if (baseType == BaseItemType.Armor)
        {
            CopyArmorAppearance(source, target);
        }
        else if (source.BaseItem.ModelType == BaseItemModelType.Simple)
        {
            CopySimpleItemAppearance(source, target);
        }
        else
        {
            // Complex (parts-based) weapons and shields
            CopyWeaponAppearance(source, target);
        }
    }

    // ───────────────────────────── Placeable Copying ─────────────────────────────

    private void CopyPlaceableAppearance(NwPlaceable source, NwPlaceable target)
    {
        if (target.Name != source.Name)
            target.Name = source.Name;

        if (target.Description != source.Description)
            target.Description = source.Description;

        if (target.Appearance.RowIndex != source.Appearance.RowIndex)
            target.Appearance = source.Appearance;

        if (target.PortraitResRef != source.PortraitResRef)
            target.PortraitResRef = source.PortraitResRef;

        if (Math.Abs(target.VisualTransform.Scale - source.VisualTransform.Scale) > 0.001f)
            target.VisualTransform.Scale = source.VisualTransform.Scale;

        if (Math.Abs(target.VisualTransform.Translation.Z - source.VisualTransform.Translation.Z) > 0.001f)
        {
            System.Numerics.Vector3 t = target.VisualTransform.Translation;
            target.VisualTransform.Translation = new System.Numerics.Vector3(t.X, t.Y, source.VisualTransform.Translation.Z);
        }
    }

    /// <summary>
    /// Copies creature equipment appearance (Armor, Cloak, Helmet, Main-Hand, Off-Hand) using CopyItemAndModify.
    /// For armor, skips the chest part if the base armor AC doesn't match (Option A).
    /// Unequips items first, copies appearance via CopyItemAndModify, then re-equips to ensure changes are applied correctly.
    /// </summary>
    public void CopyCreatureEquipment(NwCreature source, NwCreature target)
    {
        if (source == null || target == null)
        {
            _player.SendServerMessage("Invalid source or target creature.", ColorConstants.Red);
            return;
        }

        // Dictionary to store items and their original slots
        Dictionary<InventorySlot, NwItem> itemsToReequip = new();

        // PHASE 1: Unequip all items first
        InventorySlot[] equipmentSlots = [InventorySlot.Chest, InventorySlot.Cloak, InventorySlot.Head, InventorySlot.RightHand, InventorySlot.LeftHand];

        foreach (InventorySlot slot in equipmentSlots)
        {
            NwItem? item = target.GetItemInSlot(slot);
            if (item != null && item.IsValid)
            {
                itemsToReequip[slot] = item;
                NWScript.AssignCommand(target, () => NWScript.ActionUnequipItem(item));
            }
        }

        // Add a delay to ensure unequip completes before we copy appearance
        NWScript.DelayCommand(0.3f, async () => await CopyEquipmentAppearancePhase2(source, target, itemsToReequip));

        _player.SendServerMessage("Copying equipment appearance...", ColorConstants.Cyan);
    }

    /// <summary>
    /// Phase 2 of equipment copying: copy appearance of unequipped items using CopyItemAndModify, then re-equip them.
    /// </summary>
    private async Task CopyEquipmentAppearancePhase2(NwCreature source, NwCreature target, Dictionary<InventorySlot, NwItem> itemsToReequip)
    {
        if (source == null || target == null || !target.IsValid)
            return;

        int copiedCount = 0;
        var slotResults = new Dictionary<InventorySlot, string>();

        // PHASE 2: Copy appearance of unequipped items

        // Copy Armor (with AC-aware copying)
        NwItem? srcArmor = source.GetItemInSlot(InventorySlot.Chest);
        if (itemsToReequip.TryGetValue(InventorySlot.Chest, out NwItem? tgtArmor) && srcArmor != null &&
            tgtArmor.IsValid && srcArmor.BaseItem.ItemType == tgtArmor.BaseItem.ItemType)
        {
            _player.SendServerMessage($"[DEBUG-Armor] Starting armor copy", ColorConstants.Yellow);
            _player.SendServerMessage($"[DEBUG-Armor] Source name: {srcArmor.Name}, Target name: {tgtArmor.Name}", ColorConstants.Yellow);

            // Get AC values upfront before any modifications
            ushort srcTorsoModel = srcArmor.Appearance.GetArmorModel(CreaturePart.Torso);
            ushort tgtTorsoModel = tgtArmor.Appearance.GetArmorModel(CreaturePart.Torso);

            _player.SendServerMessage($"[DEBUG-Armor] Source torso model: {srcTorsoModel}, Target torso model: {tgtTorsoModel}", ColorConstants.Yellow);

            int? srcAc = GetArmorAcFromModel(srcTorsoModel);
            int? tgtAc = GetArmorAcFromModel(tgtTorsoModel);

            _player.SendServerMessage($"[DEBUG-Armor] Source AC: {(srcAc.HasValue ? srcAc.Value.ToString() : "UNKNOWN")}, Target AC: {(tgtAc.HasValue ? tgtAc.Value.ToString() : "UNKNOWN")}", ColorConstants.Yellow);

            // Copy item name and description from source to target
            // TODO: Uncomment when bugs are fixed
            // if (tgtArmor.Name != srcArmor.Name)
            //     tgtArmor.Name = srcArmor.Name;
            // if (tgtArmor.Description != srcArmor.Description)
            //     tgtArmor.Description = srcArmor.Description;

            bool armorCopied = false;

            // Only copy if AC values match, otherwise skip chest part (Option A)
            if (srcAc.HasValue && tgtAc.HasValue && srcAc.Value == tgtAc.Value)
            {
                // AC matches - copy all armor parts via CopyItemAndModify
                _player.SendServerMessage($"[DEBUG-Armor] AC MATCHES ({srcAc} == {tgtAc}), copying all parts including torso", ColorConstants.Yellow);
                armorCopied = CopyArmorAppearanceViaModify(srcArmor, tgtArmor, true);
                if (armorCopied)
                {
                    itemsToReequip[InventorySlot.Chest] = tgtArmor;
                    slotResults[InventorySlot.Chest] = "Armor copied (full appearance)";
                    copiedCount++;
                }
            }
            else if (srcAc.HasValue && tgtAc.HasValue)
            {
                // AC mismatch - copy everything except chest (Option A)
                _player.SendServerMessage($"[DEBUG-Armor] AC MISMATCH ({srcAc} → {tgtAc}), copying non-torso only", ColorConstants.Yellow);
                armorCopied = CopyArmorAppearanceViaModify(srcArmor, tgtArmor, false);
                if (armorCopied)
                {
                    itemsToReequip[InventorySlot.Chest] = tgtArmor;
                    slotResults[InventorySlot.Chest] = $"Armor copied (non-torso only, AC mismatch: {srcAc} → {tgtAc})";
                    copiedCount++;
                }
            }
            else
            {
                // Can't determine AC - copy non-torso parts only (Option A)
                _player.SendServerMessage($"[DEBUG-Armor] AC UNKNOWN, copying non-torso only (safeguard)", ColorConstants.Yellow);
                armorCopied = CopyArmorAppearanceViaModify(srcArmor, tgtArmor, false);
                if (armorCopied)
                {
                    itemsToReequip[InventorySlot.Chest] = tgtArmor;
                    slotResults[InventorySlot.Chest] = "Armor copied (non-torso only, AC unknown)";
                    copiedCount++;
                }
            }

            if (!armorCopied)
            {
                slotResults[InventorySlot.Chest] = "Armor copy failed";
            }
        }

        // Copy Cloak
        NwItem? srcCloak = source.GetItemInSlot(InventorySlot.Cloak);
        if (itemsToReequip.TryGetValue(InventorySlot.Cloak, out NwItem? tgtCloak) && srcCloak != null &&
            tgtCloak.IsValid && srcCloak.BaseItem.ItemType == tgtCloak.BaseItem.ItemType)
        {
            // Copy item name and description from source to target
            // TODO: Uncomment when bugs are fixed
            // if (tgtCloak.Name != srcCloak.Name)
            //     tgtCloak.Name = srcCloak.Name;
            // if (tgtCloak.Description != srcCloak.Description)
            //     tgtCloak.Description = srcCloak.Description;

            if (CopySimpleItemAppearanceViaModify(srcCloak, tgtCloak, target))
            {
                itemsToReequip[InventorySlot.Cloak] = tgtCloak;
                slotResults[InventorySlot.Cloak] = "Cloak copied";
                copiedCount++;
            }
            else
            {
                slotResults[InventorySlot.Cloak] = "Cloak copy failed";
            }
        }

        // Copy Helmet
        NwItem? srcHelmet = source.GetItemInSlot(InventorySlot.Head);
        if (itemsToReequip.TryGetValue(InventorySlot.Head, out NwItem? tgtHelmet) && srcHelmet != null &&
            tgtHelmet.IsValid && srcHelmet.BaseItem.ItemType == tgtHelmet.BaseItem.ItemType)
        {
            // Copy item name and description from source to target
            // TODO: Uncomment when bugs are fixed
            // if (tgtHelmet.Name != srcHelmet.Name)
            //     tgtHelmet.Name = srcHelmet.Name;
            // if (tgtHelmet.Description != srcHelmet.Description)
            //     tgtHelmet.Description = srcHelmet.Description;

            if (CopySimpleItemAppearanceViaModify(srcHelmet, tgtHelmet, target))
            {
                itemsToReequip[InventorySlot.Head] = tgtHelmet;
                slotResults[InventorySlot.Head] = "Helmet copied";
                copiedCount++;
            }
            else
            {
                slotResults[InventorySlot.Head] = "Helmet copy failed";
            }
        }

        // Copy Main-Hand (Right Hand)
        NwItem? srcMainHand = source.GetItemInSlot(InventorySlot.RightHand);
        if (itemsToReequip.TryGetValue(InventorySlot.RightHand, out NwItem? tgtMainHand) && srcMainHand != null &&
            tgtMainHand.IsValid)
        {
            // Copy item name and description from source to target
            // TODO: Uncomment when bugs are fixed
            // if (tgtMainHand.Name != srcMainHand.Name)
            //     tgtMainHand.Name = srcMainHand.Name;
            // if (tgtMainHand.Description != srcMainHand.Description)
            //     tgtMainHand.Description = srcMainHand.Description;

            // Handle weapon copying with potential base-type conversion
            string mainHandResult = await CopyWeaponWithBaseTypeConversion(srcMainHand, tgtMainHand, target, InventorySlot.RightHand);
            slotResults[InventorySlot.RightHand] = mainHandResult;
            if (!mainHandResult.Contains("failed"))
            {
                // Update the reference if it changed
                NwItem? updated = target.GetItemInSlot(InventorySlot.RightHand);
                if (updated != null && updated.IsValid)
                    itemsToReequip[InventorySlot.RightHand] = updated;
                copiedCount++;
            }
        }

        // Copy Off-Hand (Left Hand)
        NwItem? srcOffHand = source.GetItemInSlot(InventorySlot.LeftHand);
        if (itemsToReequip.TryGetValue(InventorySlot.LeftHand, out NwItem? tgtOffHand) && srcOffHand != null &&
            tgtOffHand.IsValid)
        {
            // Copy item name and description from source to target
            // TODO: Uncomment when bugs are fixed
            // if (tgtOffHand.Name != srcOffHand.Name)
            //     tgtOffHand.Name = srcOffHand.Name;
            // if (tgtOffHand.Description != srcOffHand.Description)
            //     tgtOffHand.Description = srcOffHand.Description;

            // Handle weapon copying with potential base-type conversion
            string offHandResult = await CopyWeaponWithBaseTypeConversion(srcOffHand, tgtOffHand, target, InventorySlot.LeftHand);
            slotResults[InventorySlot.LeftHand] = offHandResult;
            if (!offHandResult.Contains("failed"))
            {
                // Update the reference if it changed
                NwItem? updated = target.GetItemInSlot(InventorySlot.LeftHand);
                if (updated != null && updated.IsValid)
                    itemsToReequip[InventorySlot.LeftHand] = updated;
                copiedCount++;
            }
        }

        // PHASE 3: Re-equip all items
        foreach (var kvp in itemsToReequip)
        {
            InventorySlot slot = kvp.Key;
            NwItem item = kvp.Value;

            if (item.IsValid)
            {
                NWScript.AssignCommand(target, () => NWScript.ActionEquipItem(item, (int)slot));
            }
        }

        // Log all slot results
        foreach (var kvp in slotResults)
        {
            InventorySlot slot = kvp.Key;
            string result = kvp.Value;
            _player.SendServerMessage($"[DEBUG-{slot}] {result}", ColorConstants.Yellow);
        }

        if (copiedCount > 0)
        {
            _player.SendServerMessage($"Copied appearance of {copiedCount} equipment slot(s).", ColorConstants.Green);
        }
        else
        {
            _player.SendServerMessage("No compatible equipment to copy.", ColorConstants.Orange);
        }
    }

    /// <summary>
    /// Copies armor appearance using CopyItemAndModify for each part and color.
    /// If includeTorso is false, skips torso part (used when AC doesn't match).
    /// </summary>
    private bool CopyArmorAppearanceViaModify(NwItem source, NwItem target, bool includeTorso)
    {
        _player.SendServerMessage($"[DEBUG-Armor] CopyItemAndModify starting (includeTorso={includeTorso})", ColorConstants.Yellow);

        // Parts to copy (excluding torso if includeTorso is false)
        CreaturePart[] partsToCopy = includeTorso
            ? [CreaturePart.Head, CreaturePart.Neck, CreaturePart.LeftShoulder, CreaturePart.RightShoulder,
               CreaturePart.LeftBicep, CreaturePart.RightBicep, CreaturePart.LeftForearm, CreaturePart.RightForearm,
               CreaturePart.LeftHand, CreaturePart.RightHand, CreaturePart.Torso, CreaturePart.Belt,
               CreaturePart.LeftThigh, CreaturePart.RightThigh, CreaturePart.LeftShin, CreaturePart.RightShin,
               CreaturePart.LeftFoot, CreaturePart.RightFoot, CreaturePart.Pelvis]
            : [CreaturePart.Head, CreaturePart.Neck, CreaturePart.LeftShoulder, CreaturePart.RightShoulder,
               CreaturePart.LeftBicep, CreaturePart.RightBicep, CreaturePart.LeftForearm, CreaturePart.RightForearm,
               CreaturePart.LeftHand, CreaturePart.RightHand, CreaturePart.Belt,
               CreaturePart.LeftThigh, CreaturePart.RightThigh, CreaturePart.LeftShin, CreaturePart.RightShin,
               CreaturePart.LeftFoot, CreaturePart.RightFoot];

        // Copy each armor model part via CopyItemAndModify
        NwItem currentArmor = target;
        foreach (CreaturePart part in partsToCopy)
        {
            ushort srcModel = source.Appearance.GetArmorModel(part);
            ushort currentModel = currentArmor.Appearance.GetArmorModel(part);

            if (srcModel != currentModel)
            {
                uint copy = NWScript.CopyItemAndModify(currentArmor, NWScript.ITEM_APPR_TYPE_ARMOR_MODEL, (int)part, srcModel, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NWScript.DestroyObject(currentArmor);
                    currentArmor = copy.ToNwObject<NwItem>()!;
                    _player.SendServerMessage($"[DEBUG-Armor] {part} model copied: {srcModel}", ColorConstants.Yellow);
                }
                else
                {
                    _player.SendServerMessage($"[DEBUG-Armor] FAILED to copy {part} model {srcModel}", ColorConstants.Red);
                    return false;
                }
            }
        }

        // Copy all armor color channels
        _player.SendServerMessage($"[DEBUG-Armor] Starting color copy...", ColorConstants.Yellow);
        foreach (ItemAppearanceArmorColor color in Enum.GetValues<ItemAppearanceArmorColor>())
        {
            byte srcColor = source.Appearance.GetArmorColor(color);
            byte currentColor = currentArmor.Appearance.GetArmorColor(color);

            _player.SendServerMessage($"[DEBUG-Armor] {color} - Source: {srcColor}, Current: {currentColor}", ColorConstants.Yellow);

            if (srcColor != currentColor)
            {
                int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + (int)color;
                _player.SendServerMessage($"[DEBUG-Armor] Calling CopyItemAndModify for {color} (colorType={colorType})", ColorConstants.Yellow);

                uint copy = NWScript.CopyItemAndModify(currentArmor, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, srcColor, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NWScript.DestroyObject(currentArmor);
                    currentArmor = copy.ToNwObject<NwItem>()!;
                    _player.SendServerMessage($"[DEBUG-Armor] ✓ {color} copied: {srcColor}", ColorConstants.Green);
                }
                else
                {
                    _player.SendServerMessage($"[DEBUG-Armor] ✗ FAILED to copy {color} (colorType={colorType}) to {srcColor}", ColorConstants.Red);
                    return false;
                }
            }
        }

        // Update the target reference and copy properties/variables
        if (!currentArmor.Equals(target))
        {
            CopyItemPropertiesAndVariables(source, currentArmor);
            // Need to update target reference in the caller - for now we'll just update via reference
            target = currentArmor;
        }

        return true;
    }

    /// <summary>
    /// Copies simple item appearance (helmet, cloak) using CopyItemAndModify for model and colors.
    /// </summary>
    private bool CopySimpleItemAppearanceViaModify(NwItem source, NwItem target, NwCreature targetCreature)
    {
        string itemName = target.Name;
        _player.SendServerMessage($"[DEBUG-{itemName}] CopyItemAndModify starting", ColorConstants.Yellow);
        _player.SendServerMessage($"[DEBUG-{itemName}] Source: {source.Name}, Target: {target.Name}", ColorConstants.Yellow);

        NwItem currentItem = target;

        // Copy simple model
        ushort srcModel = source.Appearance.GetSimpleModel();
        ushort currentModel = currentItem.Appearance.GetSimpleModel();

        _player.SendServerMessage($"[DEBUG-{itemName}] Simple model - Source: {srcModel}, Current: {currentModel}", ColorConstants.Yellow);

        if (srcModel != currentModel)
        {
            _player.SendServerMessage($"[DEBUG-{itemName}] Calling CopyItemAndModify for simple model {srcModel}", ColorConstants.Yellow);
            uint copy = NWScript.CopyItemAndModify(currentItem, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, srcModel, 1);
            _player.SendServerMessage($"[DEBUG-{itemName}] CopyItemAndModify returned: {copy} (valid={NWScript.GetIsObjectValid(copy)})", ColorConstants.Yellow);

            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NWScript.DestroyObject(currentItem);
                currentItem = copy.ToNwObject<NwItem>()!;
                _player.SendServerMessage($"[DEBUG-{itemName}] ✓ Simple model copied: {srcModel}", ColorConstants.Green);
            }
            else
            {
                _player.SendServerMessage($"[DEBUG-{itemName}] ✗ FAILED to copy simple model {srcModel}", ColorConstants.Red);
                return false;
            }
        }

        // Copy all armor color channels (used for tinting simple items)
        _player.SendServerMessage($"[DEBUG-{itemName}] Starting color copy...", ColorConstants.Yellow);
        foreach (ItemAppearanceArmorColor color in Enum.GetValues<ItemAppearanceArmorColor>())
        {
            byte srcColor = source.Appearance.GetArmorColor(color);
            byte currentColor = currentItem.Appearance.GetArmorColor(color);

            _player.SendServerMessage($"[DEBUG-{itemName}] {color} - Source: {srcColor}, Current: {currentColor}", ColorConstants.Yellow);

            if (srcColor != currentColor)
            {
                int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + (int)color;
                _player.SendServerMessage($"[DEBUG-{itemName}] Calling CopyItemAndModify for {color} (colorType={colorType})", ColorConstants.Yellow);

                uint copy = NWScript.CopyItemAndModify(currentItem, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, srcColor, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NWScript.DestroyObject(currentItem);
                    currentItem = copy.ToNwObject<NwItem>()!;
                    _player.SendServerMessage($"[DEBUG-{itemName}] ✓ {color} copied: {srcColor}", ColorConstants.Green);
                }
                else
                {
                    _player.SendServerMessage($"[DEBUG-{itemName}] ✗ FAILED to copy {color} (colorType={colorType}) to {srcColor}", ColorConstants.Red);
                    return false;
                }
            }
        }

        // Update the target reference and copy properties/variables
        if (!currentItem.Equals(target))
        {
            _player.SendServerMessage($"[DEBUG-{itemName}] Item reference changed, copying properties/variables", ColorConstants.Yellow);
            CopyItemPropertiesAndVariables(source, currentItem);
            target = currentItem;
        }

        return true;
    }

    /// <summary>
    /// Copies weapon appearance and optionally converts base item type if wield types match.
    /// If types differ but wield types match, creates a new weapon of source type.
    /// Returns a descriptive string for debugging.
    /// </summary>
    private async Task<string> CopyWeaponWithBaseTypeConversion(NwItem source, NwItem target, NwCreature targetCreature, InventorySlot slot)
    {
        string slotName = slot switch
        {
            InventorySlot.RightHand => "Main-Hand",
            InventorySlot.LeftHand => "Off-Hand",
            _ => slot.ToString()
        };

        _player.SendServerMessage($"[DEBUG-{slotName}] Starting appearance/type copy", ColorConstants.Yellow);
        _player.SendServerMessage($"[DEBUG-{slotName}] Source: {source.Name} ({source.BaseItem.ItemType})", ColorConstants.Yellow);
        _player.SendServerMessage($"[DEBUG-{slotName}] Target: {target.Name} ({target.BaseItem.ItemType})", ColorConstants.Yellow);

        NwItem workingItem = target;

        // Check if base item types match
        if (source.BaseItem.ItemType != target.BaseItem.ItemType)
        {
            // Check if types are wield-compatible (e.g., Greatsword ↔ Halberd)
            int srcWield = GetWeaponWieldType(source.BaseItem.ItemType);
            int tgtWield = GetWeaponWieldType(target.BaseItem.ItemType);

            _player.SendServerMessage($"[DEBUG-{slotName}] Base types don't match (source={source.BaseItem.ItemType}, target={target.BaseItem.ItemType})", ColorConstants.Yellow);
            _player.SendServerMessage($"[DEBUG-{slotName}] Wield types: source={srcWield}, target={tgtWield}", ColorConstants.Yellow);

            if (srcWield != tgtWield || srcWield < 0)
            {
                _player.SendServerMessage($"[DEBUG-{slotName}] Wield types incompatible, skipping", ColorConstants.Yellow);
                return $"{slotName}: base item type incompatible";
            }

            // Wield types match - convert target to source's base type
            _player.SendServerMessage($"[DEBUG-{slotName}] Wield types compatible, converting base type", ColorConstants.Yellow);
            string result = await ConvertWeaponBaseType(source, target, targetCreature, slotName, slot);
            if (result.Contains("failed"))
                return result;

            // Get the newly created weapon
            workingItem = targetCreature.GetItemInSlot(slot);
            if (workingItem == null || !workingItem.IsValid)
            {
                _player.SendServerMessage($"[DEBUG-{slotName}] Failed to retrieve converted weapon", ColorConstants.Red);
                return $"{slotName}: conversion failed";
            }

            _player.SendServerMessage($"[DEBUG-{slotName}] Base type conversion successful, now copying appearance", ColorConstants.Yellow);
        }

        // Now copy appearance using CopyItemAndModify
        bool isSimple = source.BaseItem.ModelType == BaseItemModelType.Simple;
        NwItem currentItem = workingItem;

        if (isSimple)
        {
            // Simple model weapon
            ushort srcModel = source.Appearance.GetSimpleModel();
            ushort currentModel = currentItem.Appearance.GetSimpleModel();

            _player.SendServerMessage($"[DEBUG-{slotName}] Simple model - Source: {srcModel}, Current: {currentModel}", ColorConstants.Yellow);

            if (srcModel != currentModel)
            {
                uint copy = NWScript.CopyItemAndModify(currentItem, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, srcModel, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NWScript.DestroyObject(currentItem);
                    currentItem = copy.ToNwObject<NwItem>()!;
                    _player.SendServerMessage($"[DEBUG-{slotName}] ✓ Simple model copied: {srcModel}", ColorConstants.Green);
                }
                else
                {
                    _player.SendServerMessage($"[DEBUG-{slotName}] ✗ FAILED to copy simple model {srcModel}", ColorConstants.Red);
                    return $"{slotName}: simple model copy failed";
                }
            }
        }
        else
        {
            // Complex weapon - top, middle, bottom
            ItemAppearanceWeaponModel[] parts = [ItemAppearanceWeaponModel.Top, ItemAppearanceWeaponModel.Middle, ItemAppearanceWeaponModel.Bottom];
            foreach (ItemAppearanceWeaponModel part in parts)
            {
                ushort srcModel = source.Appearance.GetWeaponModel(part);
                ushort currentModel = currentItem.Appearance.GetWeaponModel(part);

                _player.SendServerMessage($"[DEBUG-{slotName}] {part} - Source: {srcModel}, Current: {currentModel}", ColorConstants.Yellow);

                if (srcModel != currentModel)
                {
                    uint copy = NWScript.CopyItemAndModify(currentItem, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, (int)part, srcModel, 1);
                    if (NWScript.GetIsObjectValid(copy) == 1)
                    {
                        NWScript.DestroyObject(currentItem);
                        currentItem = copy.ToNwObject<NwItem>()!;
                        _player.SendServerMessage($"[DEBUG-{slotName}] ✓ {part} model copied: {srcModel}", ColorConstants.Green);
                    }
                    else
                    {
                        _player.SendServerMessage($"[DEBUG-{slotName}] ✗ FAILED to copy {part} model {srcModel}", ColorConstants.Red);
                        return $"{slotName}: {part} model copy failed";
                    }
                }
            }
        }

        // Copy weapon color channels
        _player.SendServerMessage($"[DEBUG-{slotName}] Starting color copy...", ColorConstants.Yellow);
        foreach (ItemAppearanceArmorColor color in Enum.GetValues<ItemAppearanceArmorColor>())
        {
            byte srcColor = source.Appearance.GetArmorColor(color);
            byte currentColor = currentItem.Appearance.GetArmorColor(color);

            _player.SendServerMessage($"[DEBUG-{slotName}] {color} - Source: {srcColor}, Current: {currentColor}", ColorConstants.Yellow);

            if (srcColor != currentColor)
            {
                int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + (int)color;
                uint copy = NWScript.CopyItemAndModify(currentItem, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, srcColor, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NWScript.DestroyObject(currentItem);
                    currentItem = copy.ToNwObject<NwItem>()!;
                    _player.SendServerMessage($"[DEBUG-{slotName}] ✓ Color {color} copied: {srcColor}", ColorConstants.Green);
                }
                else
                {
                    _player.SendServerMessage($"[DEBUG-{slotName}] ✗ FAILED to copy color {color}", ColorConstants.Red);
                    return $"{slotName}: color copy failed";
                }
            }
        }

        // Update reference and copy properties/variables
        if (!currentItem.Equals(workingItem))
        {
            _player.SendServerMessage($"[DEBUG-{slotName}] Item reference changed, copying properties/variables", ColorConstants.Yellow);
            CopyItemPropertiesAndVariables(source, currentItem);
        }

        return $"{slotName}: copied";
    }

    /// <summary>
    /// Converts a weapon's base item type to match source when wield types are compatible.
    /// Creates a new weapon blueprint, copies properties, unequips old, destroys old, equips new.
    /// </summary>
    private async Task<string> ConvertWeaponBaseType(NwItem source, NwItem target, NwCreature targetCreature, string slotName, InventorySlot slot)
    {
        string sourceType = source.BaseItem.ItemType.ToString();
        string targetType = target.BaseItem.ItemType.ToString();

        _player.SendServerMessage($"[DEBUG-{slotName}] Converting from {targetType} to {sourceType}", ColorConstants.Yellow);

        // Get the blueprint resref for the source weapon type
        string weaponResref = GetWeaponResref(source.BaseItem.ItemType);
        if (string.IsNullOrEmpty(weaponResref))
        {
            _player.SendServerMessage($"[DEBUG-{slotName}] No blueprint resref found for {sourceType}", ColorConstants.Red);
            return $"{slotName}: conversion failed (no blueprint)";
        }

        _player.SendServerMessage($"[DEBUG-{slotName}] Creating weapon with resref: {weaponResref}", ColorConstants.Yellow);

        // Create new weapon asynchronously
        NwItem? newWeapon = await NwItem.Create(weaponResref, targetCreature, 1, "");
        if (newWeapon == null || !newWeapon.IsValid)
        {
            _player.SendServerMessage($"[DEBUG-{slotName}] Failed to create new weapon", ColorConstants.Red);
            return $"{slotName}: conversion failed (create)";
        }

        _player.SendServerMessage($"[DEBUG-{slotName}] New weapon created, copying properties/variables", ColorConstants.Yellow);

        // Copy name and description from target (which came from source)
        // TODO: Uncomment when bugs are fixed
        // newWeapon.Name = target.Name;
        // newWeapon.Description = target.Description;

        // Copy all properties from source
        int propsCopied = 0;
        foreach (ItemProperty prop in source.ItemProperties)
        {
            newWeapon.AddItemProperty(prop, EffectDuration.Permanent);
            propsCopied++;
        }

        // Copy all variables from source
        int varsCopied = 0;
        foreach (ObjectVariable var in source.LocalVariables)
        {
            switch (var)
            {
                case LocalVariableInt li:
                    newWeapon.GetObjectVariable<LocalVariableInt>(li.Name).Value = li.Value;
                    varsCopied++;
                    break;
                case LocalVariableFloat lf:
                    newWeapon.GetObjectVariable<LocalVariableFloat>(lf.Name).Value = lf.Value;
                    varsCopied++;
                    break;
                case LocalVariableString ls:
                    newWeapon.GetObjectVariable<LocalVariableString>(ls.Name).Value = ls.Value ?? string.Empty;
                    varsCopied++;
                    break;
                case LocalVariableLocation lloc:
                    newWeapon.GetObjectVariable<LocalVariableLocation>(lloc.Name).Value = lloc.Value;
                    varsCopied++;
                    break;
                case LocalVariableObject<NwObject> lo:
                    newWeapon.GetObjectVariable<LocalVariableObject<NwObject>>(lo.Name).Value = lo.Value;
                    varsCopied++;
                    break;
            }
        }

        _player.SendServerMessage($"[DEBUG-{slotName}] Copied {propsCopied} properties, {varsCopied} variables", ColorConstants.Yellow);

        // Unequip and destroy the old weapon
        targetCreature.RunUnequip(target);
        target.Destroy();

        _player.SendServerMessage($"[DEBUG-{slotName}] Old weapon unequipped and destroyed, equipping new weapon", ColorConstants.Yellow);

        // Equip the new weapon
        targetCreature.RunEquip(newWeapon, slot);

        return "conversion_success";
    }

    /// <summary>
    /// Copies item properties and local variables from source to target.
    /// Used to preserve enchantments and custom data when creating new items via CopyItemAndModify.
    /// </summary>
    private void CopyItemPropertiesAndVariables(NwItem source, NwItem target)
    {
        if (source == null || target == null) return;

        _player.SendServerMessage($"[DEBUG] Copying properties and variables from {source.Name} to {target.Name}", ColorConstants.Yellow);

        // Copy item properties
        int propsCopied = 0;
        foreach (ItemProperty prop in source.ItemProperties)
        {
            target.AddItemProperty(prop, EffectDuration.Permanent);
            propsCopied++;
        }

        // Copy local variables
        int varsCopied = 0;
        foreach (ObjectVariable var in source.LocalVariables)
        {
            switch (var)
            {
                case LocalVariableInt li:
                    target.GetObjectVariable<LocalVariableInt>(li.Name).Value = li.Value;
                    varsCopied++;
                    break;
                case LocalVariableFloat lf:
                    target.GetObjectVariable<LocalVariableFloat>(lf.Name).Value = lf.Value;
                    varsCopied++;
                    break;
                case LocalVariableString ls:
                    target.GetObjectVariable<LocalVariableString>(ls.Name).Value = ls.Value ?? string.Empty;
                    varsCopied++;
                    break;
                case LocalVariableLocation lloc:
                    target.GetObjectVariable<LocalVariableLocation>(lloc.Name).Value = lloc.Value;
                    varsCopied++;
                    break;
                case LocalVariableObject<NwObject> lo:
                    target.GetObjectVariable<LocalVariableObject<NwObject>>(lo.Name).Value = lo.Value;
                    varsCopied++;
                    break;
            }
        }

        _player.SendServerMessage($"[DEBUG] Copied {propsCopied} properties, {varsCopied} variables", ColorConstants.Yellow);
    }

    /// <summary>
    /// Gets the AC value from an armor model number.
    /// Uses the complete mapping from CharacterCustomizationModel to ensure accuracy.
    /// Returns null if the model is not recognized.
    /// </summary>
    private static int? GetArmorAcFromModel(ushort model)
    {
        // AC 0 - Cloth
        if (new[] { 0, 1, 3, 5, 6, 7, 8, 9, 12, 19, 39, 50, 66, 67, 73, 74, 150, 158, 199, 200, 210, 228, 239, 240, 251 }.Contains(model))
            return 0;

        // AC 1 - Padded
        if (new[] { 20, 28, 40 }.Contains(model))
            return 1;

        // AC 2 - Hide
        if (new[] { 10, 13, 16, 27, 41, 42, 49, 58, 75, 76, 77, 86, 91, 92 }.Contains(model))
            return 2;

        // AC 3 - Studded
        if (new[] { 22, 29, 43, 44 }.Contains(model))
            return 3;

        // AC 4 - Scale
        if (new[] { 4, 15, 18, 34, 35, 36, 38, 54, 55, 56, 59, 63, 64, 68, 69, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105 }.Contains(model))
            return 4;

        // AC 5 - Chain
        if (new[] { 24, 25, 26, 31, 32, 204 }.Contains(model))
            return 5;

        // AC 6 - Banded
        if (new[] { 11, 17, 30, 45, 48 }.Contains(model))
            return 6;

        // AC 7 - Half-plate
        if (new[] { 33, 46, 47, 51, 52 }.Contains(model))
            return 7;

        // AC 8 - Full plate
        if (new[] { 14, 21, 23, 37, 53, 57, 60, 61, 62, 65, 70, 71, 72, 90, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 186, 190, 209, 220, 221, 222, 223, 252, 253 }.Contains(model))
            return 8;

        // Unknown model
        return null;
    }

    /// <summary>
    /// Gets the weapon blueprint resref for a given base item type.
    /// Used for safe base-type conversion when wield types are compatible.
    /// Ported from CustomSummon.WeaponChangePresenter.
    /// </summary>
    private string GetWeaponResref(BaseItemType weaponType)
    {
        return weaponType switch
        {
            // One-handed weapons
            BaseItemType.Longsword => "js_bla_wels",
            BaseItemType.Shortsword => "js_bla_wess",
            BaseItemType.Rapier => "js_bla_wera",
            BaseItemType.Scimitar => "js_bla_wesc",
            BaseItemType.Handaxe => "js_bla_weha",
            BaseItemType.Battleaxe => "js_bla_weba",
            BaseItemType.LightHammer => "js_bla_welh",
            BaseItemType.LightMace => "js_bla_wema",
            BaseItemType.Morningstar => "js_bla_wemo",
            BaseItemType.Club => "js_bla_wecl",
            BaseItemType.Dagger => "js_bla_weda",
            BaseItemType.Kama => "js_bla_wekm",
            BaseItemType.Kukri => "js_bla_weku",
            BaseItemType.Sickle => "js_bla_wesi",
            BaseItemType.Warhammer => "js_bla_wewa",
            BaseItemType.LightFlail => "js_bla_welf",
            BaseItemType.Whip => "js_bla_wewh",
            BaseItemType.Trident => "js_bla_wetr",
            BaseItemType.DwarvenWaraxe => "js_bla_wedw",
            BaseItemType.Bastardsword => "js_bla_webs",
            BaseItemType.Katana => "js_bla_weka",
            BaseItemType.MagicStaff => "js_bla_wems",

            // Two-handed weapons
            BaseItemType.Greatsword => "js_bla_wegs",
            BaseItemType.Greataxe => "js_bla_wega",
            BaseItemType.Halberd => "js_bla_wehb",
            BaseItemType.HeavyFlail => "js_bla_wehf",
            BaseItemType.Scythe => "js_bla_wesy",
            BaseItemType.Quarterstaff => "js_bla_wequ",
            BaseItemType.ShortSpear => "js_bla_wesp",

            // Bows
            BaseItemType.Longbow => "js_arch_bow",
            BaseItemType.Shortbow => "js_arch_sbow",

            // Crossbows
            BaseItemType.LightCrossbow => "js_arch_lbow",
            BaseItemType.HeavyCrossbow => "js_arch_cbow",

            // Double-sided
            BaseItemType.Doubleaxe => "js_bla_wedb",
            BaseItemType.TwoBladedSword => "js_bla_we2b",
            BaseItemType.DireMace => "js_bla_wedm",

            // Thrown
            BaseItemType.Dart => "js_arch_dart",
            BaseItemType.Sling => "js_arch_sling",
            BaseItemType.Shuriken => "js_arch_shrk",
            BaseItemType.ThrowingAxe => "js_arch_thax",

            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the wield type of a weapon for compatibility checking.
    /// Based on CustomSummon's GetWeaponWieldType logic:
    /// 0 = one-handed, 4 = two-handed, 5 = bow, 6 = crossbow, 8 = double-sided, 10 = dart/sling, 11 = shuriken/throwing axe
    /// </summary>
    private static int GetWeaponWieldType(BaseItemType baseItemType)
    {
        return baseItemType switch
        {
            // One-handed weapons (0)
            BaseItemType.Longsword or BaseItemType.Shortsword or BaseItemType.Rapier or
                BaseItemType.Scimitar or BaseItemType.Handaxe or BaseItemType.Battleaxe or
                BaseItemType.LightHammer or BaseItemType.LightMace or BaseItemType.Morningstar or
                BaseItemType.Club or BaseItemType.Dagger or BaseItemType.Kama or
                BaseItemType.Kukri or BaseItemType.Sickle or BaseItemType.Warhammer or
                BaseItemType.LightFlail or BaseItemType.Whip or BaseItemType.Trident or
                BaseItemType.DwarvenWaraxe or BaseItemType.Bastardsword or BaseItemType.Katana or
                BaseItemType.MagicStaff => 0,

            // Two-handed weapons (4)
            BaseItemType.Greatsword or BaseItemType.Greataxe or BaseItemType.Halberd or
                BaseItemType.HeavyFlail or BaseItemType.Scythe or BaseItemType.ShortSpear or
                BaseItemType.Quarterstaff => 4,

            // Bows (5)
            BaseItemType.Longbow or BaseItemType.Shortbow => 5,

            // Crossbows (6)
            BaseItemType.LightCrossbow or BaseItemType.HeavyCrossbow => 6,

            // Double-sided weapons (8)
            BaseItemType.Doubleaxe or BaseItemType.TwoBladedSword or BaseItemType.DireMace => 8,

            // Thrown light (10)
            BaseItemType.Dart or BaseItemType.Sling => 10,

            // Thrown (11)
            BaseItemType.Shuriken or BaseItemType.ThrowingAxe => 11,

            _ => -1 // Unknown
        };
    }
}


