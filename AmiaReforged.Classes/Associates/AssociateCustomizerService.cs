using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Associates;

[ServiceBinding(typeof(AssociateCustomizerService))]
public class AssociateCustomizerService
{
    private static readonly Color ColorRed = Color.FromRGBA("#ff0032cc");
    private static readonly Color ColorGreen = Color.FromRGBA("#43ff64d9");
    private static readonly Color ColorWhite = Color.FromRGBA("#d2ffffd9");

    private const string ToolTag = "ass_customizer";

    // Baseitems.2da id numbers for left-hand holdable items
    private const uint Torch = 15;
    private const uint Tools = 113;

    // int TRUE for nwn reference
    private const int True = 1;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AssociateCustomizerService()
    {
        NwModule.Instance.OnActivateItem += CopyTargetAppearance;
        NwModule.Instance.OnActivateItem += StoreAssociateAppearance;
        NwModule.Instance.OnAssociateAdd += CustomizeAssociateAppearance;
        Log.Info("Associate Appearance Service initialized.");
    }

    /// <summary>
    ///     Copies the target creature's appearance, equipment appearance, and visual effects.
    /// </summary>
    private void CopyTargetAppearance(ModuleEvents.OnActivateItem eventData)
    {
        // Declare conditions for tool activation:
        // Works only if the user is a DM and the target is a non-associate creature
        if (eventData.ActivatedItem.Tag != ToolTag) return;
        if (!eventData.ItemActivator.IsPlayerControlled) return;
        if (!(eventData.ItemActivator.IsPlayerControlled(out NwPlayer? player) && player.IsDM))
        {
            player?.SendServerMessage("[Associate Customizer] Only a DM can use this tool.", ColorRed);
            return;
        }
        if (eventData.TargetObject is not NwCreature creature)
        {
            player.SendServerMessage("[Associate Customizer] Target must be a creature.", ColorRed);
            return;
        }
        if (creature.AssociateType != AssociateType.None) return;

        NwItem associateCustomizer = eventData.ActivatedItem;

        DeleteDanglers(associateCustomizer);

        // Gather the data of the appearance we want the associate customizer to change

        CopyCreatureData(associateCustomizer, creature);

        bool armorCopied = CopyArmorData(associateCustomizer, creature);

        bool helmetCopied = CopyHelmetData(associateCustomizer, creature);

        bool cloakCopied = CopyCloakData(associateCustomizer, creature);

        bool mainHandCopied = CopyMainHandData(associateCustomizer, creature);

        bool offHandCopied = CopyOffHandData(associateCustomizer, creature);

        bool vfxCopied = CopyVfxData(associateCustomizer, creature);

        player.SendServerMessage
            ("[Associate Customizer] Creature copied!", ColorGreen);
        if (armorCopied) player.SendServerMessage
            ("[Associate Customizer] Armor copied!", ColorGreen);
        if (helmetCopied) player.SendServerMessage
            ("[Associate Customizer] Helmet copied!", ColorGreen);
        if (cloakCopied) player.SendServerMessage
            ("[Associate Customizer] Cloak copied!", ColorGreen);
        if (mainHandCopied) player.SendServerMessage
            ("[Associate Customizer] Mainhand copied!", ColorGreen);
        if (offHandCopied) player.SendServerMessage
            ("[Associate Customizer] Offhand copied!", ColorGreen);
        if (vfxCopied) player.SendServerMessage
            ("[Associate Customizer] Visual effects copied!", ColorGreen);
        player.SendServerMessage
            ("To assign the copied appearance to an associate, target the associate with the tool.", ColorWhite);
    }

    /// <summary>
    /// Copies the creature's vfx data and stores it in the Associate Customizer item
    /// </summary>
    /// <returns>false if the creature has no vfx</returns>
    private bool CopyVfxData(NwItem associateCustomizer, NwCreature creature)
    {
        if (creature.ActiveEffects.All(effect => effect.EffectType != EffectType.VisualEffect))
            return false;

        List<int> vfxList = [];

        // Loop for each visual effect to get the length of the list
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.EffectType == EffectType.VisualEffect)
            {
                vfxList.Add(effect.IntParams[0]);
            }
        }

        // Loop for the visual effects row index and store each with a unique var name
        for (int i = 0; i < vfxList.Count; i++)
        {
            associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i).Value = vfxList[i];
        }

        // Store the count of vfxs for later use
        associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount").Value = vfxList.Count;

        return true;
    }

    /// <summary>
    /// Copies the creature's off hand data and stores it in the Associate Customizer item
    /// </summary>
    /// <returns>false if the creature has no off hand item</returns>
    private bool CopyOffHandData(NwItem associateCustomizer, NwCreature creature)
    {
        NwItem? offHand = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (offHand == null) return false;

        byte[]? offHandData = offHand.Serialize();
        if (offHandData == null) return false;

        associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Value
            = Convert.ToBase64String(offHandData);

        return true;
    }

    /// <summary>
    /// Copies the creature's main hand data and stores it in the Associate Customizer item
    /// </summary>
    /// <returns>false if the creature has no main hand item</returns>
    private bool CopyMainHandData(NwItem associateCustomizer, NwCreature creature)
    {
        NwItem? mainHand = creature.GetItemInSlot(InventorySlot.RightHand);
        if (mainHand == null) return false;

        byte[]? mainHandData = mainHand.Serialize();
        if (mainHandData == null) return false;

        associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Value
            = Convert.ToBase64String(mainHandData);

        return true;
    }

    /// <summary>
    /// Copies the creature's cloak data and stores it in the Associate Customizer item
    /// </summary>
    /// <returns>false if the creature has no cloak</returns>
    private bool CopyCloakData(NwItem associateCustomizer, NwCreature creature)
    {
        NwItem? cloak = creature.GetItemInSlot(InventorySlot.Cloak);
        if (cloak == null) return false;

        byte[]? cloakData = cloak.Serialize();
        if (cloakData == null) return false;

        associateCustomizer.GetObjectVariable<LocalVariableString>("cloak").Value
            = Convert.ToBase64String(cloakData);

        return true;
    }

    /// <summary>
    /// Copies the creature's helmet data and stores it in the Associate Customizer item
    /// </summary>
    /// <returns>false if the creature has no helmet</returns>
    private bool CopyHelmetData(NwItem associateCustomizer, NwCreature creature)
    {
            NwItem? helmet = creature.GetItemInSlot(InventorySlot.Head);
            if (helmet == null) return false;

            byte[]? helmetData = helmet.Serialize();
            if (helmetData == null) return false;

            associateCustomizer.GetObjectVariable<LocalVariableString>("helmet").Value
                = Convert.ToBase64String(helmetData);

            return true;
    }

    /// <summary>
    /// Copies the creature's armor data and stores it in the Associate Customizer item
    /// </summary>
    /// <returns>false if the creature has no armor</returns>
    private bool CopyArmorData(NwItem associateCustomizer, NwCreature creature)
    {
        NwItem? armor = creature.GetItemInSlot(InventorySlot.Chest);
        if (armor == null) return false;

        byte[]? armorData = armor.Serialize();
        if (armorData == null) return false;

        associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Value
            = Convert.ToBase64String(armorData);

        return true;
    }

    /// <summary>
    /// Copies the creature data and stores it in the Associate Customizer item
    /// </summary>
    private void CopyCreatureData(NwItem associateCustomizer, NwCreature creature)
    {
        byte[]? creatureData = creature.Serialize();

        if (creatureData == null) return;

        associateCustomizer.GetObjectVariable<LocalVariableString>("creature").Value
            = Convert.ToBase64String(creatureData);
    }

    /// <summary>
    /// Deletes dangerous dangling data stored in the tool from possible previous uses
    /// </summary>
    private void DeleteDanglers(NwItem associateCustomizer)
    {
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("creature").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("creature").Delete();

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("armor").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Delete();

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("helmet").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("helmet").Delete();

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("cloak").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("cloak").Delete();

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Delete();

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();

        if (associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount").Delete();
        for (int i = 0; i < 50; i++)
        {
            if (associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i).HasValue)
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i).Delete();
        }
    }

    /// <summary>
    ///     Stores the copied appearance for the associate.
    /// </summary>
    private void StoreAssociateAppearance(ModuleEvents.OnActivateItem obj)
    {
        // Works only if the user is a DM and the target is an non-dominated associate creature
        if (obj.ActivatedItem.Tag != ToolTag) return;
        if (!obj.ItemActivator.IsPlayerControlled) return;
        if (!(obj.ItemActivator.IsPlayerControlled(out NwPlayer? player) && player.IsDM)) return;
        if (obj.TargetObject is not NwCreature associate) return;
        if (associate.AssociateType is AssociateType.None or AssociateType.Dominated) return;

        NwItem associateCustomizer = obj.ActivatedItem;

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("creature").HasNothing)
        {
            player.SendServerMessage
            ("[Associate Customizer] You must first use the tool on a non-associate creature to copy its appearance.", ColorRed);
            return;
        }

        MatchArmors(associateCustomizer, player, associate);

        MatchMainHandItems(associateCustomizer, player, associate);

        MatchOffHandItems(associateCustomizer, player, associate);

        AssignDataToAssociate(associateCustomizer, associate);

        AssignCustomizerToPc(associateCustomizer, associate);

        player.SendServerMessage
            ($"[Associate Customizer] Custom appearance stored for {associate.OriginalName}", ColorGreen);
        player.SendServerMessage
            ("Hand the tool over to the player and have them summon the associate to make sure it applies properly!", ColorWhite);
    }

    /// <summary>
    /// Assigns the Associate Customizer to the PC who it belongs to by renaming it
    /// </summary>
    private void AssignCustomizerToPc(NwItem associateCustomizer, NwCreature associate)
    {
        if (associate.Master == null) return;

        if (associateCustomizer.Name.Contains(associate.Master.OriginalFirstName)) return;

        string toolName = associateCustomizer.Name;

        associateCustomizer.Name = $"{associate.Master.OriginalFirstName}'s {toolName}";
    }

    /// <summary>
    /// The data stored in the Associate Customizer item is assigned according to the associate's resref so that
    /// CustomizeAssociateAppearance can match the data with the associate
    /// </summary>
    private void AssignDataToAssociate(NwItem associateCustomizer, NwCreature associate)
    {
        string associateResRef = associate.ResRef;

        // Since animal companion and familiar resref differs every level, we just nab the first eight chars for them
        if (associate.AssociateType is AssociateType.AnimalCompanion or AssociateType.Familiar)
            associateResRef = associate.ResRef[..8];

        // Cycle through every appearance and vfx variable and store each variable to the appearance tool by the associate
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("creature").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("creature"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableString>("creature");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("armor").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("armor"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableString>("armor");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("helmet").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("helmet"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableString>("helmet");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("cloak").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("cloak"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableString>("cloak");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("offhand"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableString>("offhand");

        if (associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount").HasValue)
        {
            int vfxCount = associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount"+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount");
            for (int i = 0; i < vfxCount; i++)
            {
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i+associateResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i);
            }
        }
    }

    /// <summary>
    /// Check that the conditions between the copied creature and the associate match for offhand items.
    /// If not, delete that data so it doesn't carry over on associate add with CustomizeAssociateAppearance.
    /// </summary>
    private void MatchOffHandItems(NwItem associateCustomizer, NwPlayer player, NwCreature associate)
    {
        string? offHandData = associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Value;
        if (offHandData == null) return;

        // Associate's mainhand can't be twohanded for offhand item data to carry over
        NwItem? mainHandItem = associate.GetItemInSlot(InventorySlot.RightHand);

        if (mainHandItem != null)
        {
            BaseItemWeaponWieldType weaponType = mainHandItem.BaseItem.WeaponWieldType;
            BaseItemWeaponSize weaponSize = mainHandItem.BaseItem.WeaponSize;

            bool weaponIsTwoHanded = weaponType == BaseItemWeaponWieldType.TwoHanded || weaponType == BaseItemWeaponWieldType.Bow
                || weaponType == BaseItemWeaponWieldType.Crossbow || weaponType == BaseItemWeaponWieldType.DoubleSided
                || (int)weaponSize > (int)associate.Size;

            if (weaponIsTwoHanded)
            {
                player.SendServerMessage
                    ("[Associate Customizer] Offhand appearance not copied. " +
                     "The associate's main hand item is held with both hands, so it can't hold an item in off hand. " +
                     "The base main hand items must match for customization.", ColorRed);

                associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();
                return;
            }
        }

        byte[] convertedOffhandData = Convert.FromBase64String(offHandData);

        NwItem? offhandCopy = NwItem.Deserialize(convertedOffhandData);
        if (offhandCopy == null) return;

        // If associate's offhand is empty the copied offhand item must be torch or tools
        if (associate.GetItemInSlot(InventorySlot.LeftHand) == null && offhandCopy.BaseItem.Id != Torch && offhandCopy.BaseItem.Id != Tools)
        {
            player.SendServerMessage
            ("[Associate Customizer] Offhand appearance not copied. " +
             "The copied creature's offhand base item must be 'Torch' or 'Tools, Left' for customization.", ColorRed);

            associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();
            return;
        }

        // If associate's offhand isn't empty, the base items must match
        if (associate.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem == offhandCopy.BaseItem) return;

        player.SendServerMessage
            ("[Associate Customizer] Offhand appearance not copied. " +
             "The base offhand items must match for customization.", ColorRed);

        associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();
    }

    /// <summary>
    /// Check that the conditions between the copied creature and the associate match for main hand items.
    /// If not, delete that data so it doesn't carry over on associate add with CustomizeAssociateAppearance.
    /// </summary>
    private void MatchMainHandItems(NwItem associateCustomizer, NwPlayer player, NwCreature associate)
    {
        string? mainHandData = associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Value;
        if (mainHandData == null) return;

        byte[] convertedMainHandData = Convert.FromBase64String(mainHandData);

        NwItem? mainHandCopy = NwItem.Deserialize(convertedMainHandData);
        if (mainHandCopy == null) return;

        // Base mainhand items between associate and copied creature must match, and associate's mainhand can't be empty
        if (associate.GetItemInSlot(InventorySlot.RightHand)?.BaseItem == mainHandCopy.BaseItem) return;

        player.SendServerMessage
            ("[Associate Customizer] Main hand item appearance not copied. " +
             "The base main hand items must match for customization.", ColorRed);

        associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Delete();
    }

    /// <summary>
    /// Check that the conditions between the copied creature and the associate match for armor.
    /// If not, delete that data so it doesn't carry over on associate add with CustomizeAssociateAppearance.
    /// </summary>
    private void MatchArmors(NwItem associateCustomizer, NwPlayer player, NwCreature associate)
    {
        string? armorData = associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Value;
        if (armorData == null) return;

        byte[] convertedArmorData = Convert.FromBase64String(armorData);

        NwItem? armorCopy = NwItem.Deserialize(convertedArmorData);
        if (armorCopy == null) return;

        // Base armors between associate and copied creature must either match, or empty armor slot must match cloth armor
        if (associate.GetItemInSlot(InventorySlot.Chest) != null && associate.GetItemInSlot(InventorySlot.Chest)?.BaseACValue != armorCopy.BaseACValue)
        {
            player.SendServerMessage
                ("[Associate Customizer] Armor appearance not copied. The base armors must match for customization.", ColorRed);

            associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Delete();
        }

        if (associate.GetItemInSlot(InventorySlot.Chest) == null && armorCopy.BaseACValue > 0)
        {
            player.SendServerMessage
                ("[Associate Customizer] Armor appearance not copied. The base armor of the copied creature must be " +
                 "cloth for customization (you can still use the robe options for armored looks).", ColorRed);

            associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Delete();
        }
    }

    /// <summary>
    ///     When the associate is summoned, gets the copied appearance for the associate and customizes it.
    /// </summary>
    private void CustomizeAssociateAppearance(OnAssociateAdd eventData)
    {
        // Works only if the owner is player controlled, has the tool in inventory, and the associate isn't dominated
        if (!eventData.Owner.IsPlayerControlled) return;
        if (eventData.AssociateType == AssociateType.Dominated) return;

        NwItem? associateCustomizer = eventData.Owner.Inventory.Items.FirstOrDefault(item => item.Tag == ToolTag);
        if (associateCustomizer == null) return;

        NwCreature associate = eventData.Associate;

        string associateResRef = associate.ResRef;

        // Animal companion and familiar has a unique resref each level so just nab the first chars
        if (eventData.AssociateType is AssociateType.AnimalCompanion or AssociateType.Familiar)
            associateResRef = associate.ResRef[..8];

        NwCreature? creatureCopy = GetCreatureCopy(associateCustomizer, associateResRef);
        if (creatureCopy == null) return;

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
    }

    /// <summary>
    /// Updates the description to include information about the customized associate
    /// </summary>
    private string UpdateAssociateName(string customizerDescription, NwCreature associate, string newName)
    {
        if (customizerDescription.Contains(associate.OriginalName)) return customizerDescription;

        string nameUpdate = $"{associate.OriginalName}is {newName}".ColorString(ColorGreen);

        return $"{nameUpdate}\n\n{customizerDescription}";
    }

    /// <summary>
    /// Updates the description to include information about the customized animal companion or familiar
    /// </summary>
    private string UpdateCompanionName(string customizerDescription, NwCreature associate, string newName)
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
        string nameUpdate = $"{prefix} {companionType} is {newName}".ColorString(ColorGreen);

        return $"{nameUpdate}\n\n{customizerDescription}";
    }

    /// <summary>
    /// Applies the visuals for the creature copy
    /// </summary>
    private void ApplyVfxFromCopy(NwItem associateCustomizer, string associateResRef, NwCreature associate)
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
    private void ApplyOffHandAppearanceFromCopy(NwCreature associate, NwItem offHandCopy)
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
    private void ApplyMainHandAppearanceFromCopy(NwCreature associate, NwItem mainHandCopy)
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
    private void ApplyCloakAppearanceFromCopy(NwCreature associate, NwItem cloakCopy)
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
    private void ApplyHelmetAppearanceFromCopy(NwCreature associate, NwItem helmetCopy)
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
    private void ApplyArmorAppearanceFromCopy(NwCreature associate, NwItem armorCopy)
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
    private void HideEquipment(NwCreature associate, CreatureEquipment equipmentCopies)
    {
        NwItem? associateHelm = associate.GetItemInSlot(InventorySlot.Head);
        NwItem? associateCloak = associate.GetItemInSlot(InventorySlot.Cloak);
        NwItem? associateOffHand = associate.GetItemInSlot(InventorySlot.LeftHand);

        if (equipmentCopies.Helmet == null && associateHelm != null)
            associateHelm.HiddenWhenEquipped = True;

        if (equipmentCopies.Cloak == null && associateCloak != null)
            associateCloak.HiddenWhenEquipped = True;

        if (equipmentCopies.OffHand == null && associateOffHand != null)
            associateOffHand.HiddenWhenEquipped = True;
    }

    private class CreatureEquipment
    {
        public NwItem? Armor { get; init; }
        public NwItem? Helmet { get; init; }
        public NwItem? Cloak { get; init; }
        public NwItem? MainHand { get; init; }
        public NwItem? OffHand { get; init; }
    }

    private NwItem? GetItemFromVariable(NwItem associateCustomizer, string variableName)
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
    private CreatureEquipment GetItemCopies(NwItem associateCustomizer, string associateResRef)
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
    private void ApplyCreatureAppearanceFromCopy(NwCreature associate, NwCreature creatureCopy)
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
    private NwCreature? GetCreatureCopy(NwItem associateCustomizer, string associateResRef)
    {
        string? creatureData = associateCustomizer.GetObjectVariable<LocalVariableString>("creature"+associateResRef).Value;
        if (creatureData == null) return null;

        byte[] convertedCreatureData = Convert.FromBase64String(creatureData);

        NwCreature? creatureCopy = NwCreature.Deserialize(convertedCreatureData);

        return creatureCopy == null ? null : creatureCopy;
    }
}
