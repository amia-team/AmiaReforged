using System.Runtime.InteropServices;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.DMS.Services;

[ServiceBinding(typeof(AssociateCustomizerService))]
public class AssociateCustomizerService
{
    static readonly Color COLOR_RED = Color.FromRGBA("#ff0032cc");
    static readonly Color COLOR_GREEN = Color.FromRGBA("#43ff64d9");
    static readonly Color COLOR_WHITE = Color.FromRGBA("#d2ffffd9");
    static readonly string TOOL_TAG = "ass_customizer";
    // Baseitems.2da id numbers for left-hand holdable items
    static readonly uint TORCH = 15;
    static readonly uint TOOLS = 113;



    
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
    private void CopyTargetAppearance(ModuleEvents.OnActivateItem obj)
    {
        // Declare conditions for tool activation:
        // Works only if the user is a DM and the target is a non-associate creature
        if (obj.ActivatedItem.Tag != TOOL_TAG) return;
        if (!obj.ItemActivator.IsPlayerControlled) return;
        if (!(obj.ItemActivator.IsPlayerControlled(out NwPlayer? player) && player.IsDM))
        {
            obj.ItemActivator.LoginPlayer?.SendServerMessage("[Associate Customizer] Only a DM can use this tool.", COLOR_RED);
            return;
        }
        if (obj.TargetObject is not NwCreature)
        {
            obj.ItemActivator.LoginPlayer?.SendServerMessage("[Associate Customizer] Target must be a creature.", COLOR_RED);
            return;
        }

        // Declare variables

        NwCreature creature = (NwCreature)obj.TargetObject;
        if (creature.AssociateType != AssociateType.None) return;

        NwItem associateCustomizer = obj.ActivatedItem;

        bool armorCopied = false;
        bool helmetCopied = false;
        bool cloakCopied = false;
        bool mainhandCopied = false;
        bool offhandCopied = false;
        bool vfxCopied = false;

        // First delete dangerous danglers from former tool activations
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

        // Copies the creature data for later calling
        byte[] creatureCopy = creature.Serialize()!;
        associateCustomizer.GetObjectVariable<LocalVariableString>("creature").Value = Convert.ToBase64String(creatureCopy);

        // If target creature has an armor, copies the data for later calling
        if (creature.GetItemInSlot(InventorySlot.Chest) != null)
        {
            NwItem armor = creature.GetItemInSlot(InventorySlot.Chest);
            byte[] armorCopy = armor.Serialize()!;
            associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Value = Convert.ToBase64String(armorCopy);
            armorCopied = true;
        }

        // If target creature has a helmet, copies the data for later calling
        if (creature.GetItemInSlot(InventorySlot.Head) != null)
        {
            NwItem helmet = creature.GetItemInSlot(InventorySlot.Head);
            byte[] helmetCopy = helmet.Serialize()!;
            associateCustomizer.GetObjectVariable<LocalVariableString>("helmet").Value = Convert.ToBase64String(helmetCopy);
            helmetCopied = true;
        }

        // // If target creature has a cloak, copies the data for later calling
        if (creature.GetItemInSlot(InventorySlot.Cloak) != null)
        {
            NwItem cloak = creature.GetItemInSlot(InventorySlot.Cloak);
            byte[] cloakCopy = cloak.Serialize()!;
            associateCustomizer.GetObjectVariable<LocalVariableString>("cloak").Value = Convert.ToBase64String(cloakCopy);
            cloakCopied = true;
        }

        // If target creature has a mainhand, copies the data for later calling
        if (creature.GetItemInSlot(InventorySlot.RightHand) != null)
        {
            NwItem mainhand = creature.GetItemInSlot(InventorySlot.RightHand);
            byte[] mainhandCopy = mainhand.Serialize()!;
            associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Value = Convert.ToBase64String(mainhandCopy);
            mainhandCopied = true;
        }

        // If target creature has an offhand, copies the data for later calling
        if (creature.GetItemInSlot(InventorySlot.LeftHand) != null)
        {
            NwItem offhand = creature.GetItemInSlot(InventorySlot.LeftHand);
            byte[] offhandCopy = offhand.Serialize()!;
            associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Value = Convert.ToBase64String(offhandCopy);
            offhandCopied = true;
        }

        // If target creature has vfxs, copies the data for later calling
        if (creature.ActiveEffects.Any(effect => effect.EffectType == EffectType.VisualEffect))
        {
            List<int> vfxList = new();

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
            vfxCopied = true;
        }

        obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Creature copied!", COLOR_GREEN);
        if (armorCopied) obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Armor copied!", COLOR_GREEN);
        if (helmetCopied) obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Helmet copied!", COLOR_GREEN);
        if (cloakCopied) obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Cloak copied!", COLOR_GREEN);
        if (mainhandCopied) obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Mainhand copied!", COLOR_GREEN);
        if (offhandCopied) obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Offhand copied!", COLOR_GREEN);
        if (vfxCopied) obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] Visual effects copied!", COLOR_GREEN);
        obj.ItemActivator.LoginPlayer.SendServerMessage
            ("To assign the copied appearance to an associate, target the associate with the tool.", COLOR_WHITE);
    }

    /// <summary>
    ///     Stores the copied appearance for the associate.
    /// </summary>
    private void StoreAssociateAppearance(ModuleEvents.OnActivateItem obj)
    {
        // Works only if the user is a DM and the target is an non-dominated associate creature
        if (obj.ActivatedItem.Tag != TOOL_TAG) return;
        if (!obj.ItemActivator.IsPlayerControlled) return;
        if (!(obj.ItemActivator.IsPlayerControlled(out NwPlayer? player) && player.IsDM)) return;
        if (obj.TargetObject is not NwCreature) return;
        NwCreature associate = (NwCreature)obj.TargetObject;
        if (associate.AssociateType == AssociateType.None || 
            associate.AssociateType == AssociateType.Dominated) return;

        NwItem associateCustomizer = obj.ActivatedItem;

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("creature").HasNothing)
        {
            obj.ItemActivator.LoginPlayer.SendServerMessage
            ("[Associate Customizer] You must first use the tool on a non-associate creature to copy its appearance.", COLOR_RED);
            return;
        }

        // Check that the conditions between the copied creature and the associate match for armor, mainhand, and offhand.
        // If conditions aren't met, delete that data so it doesn't carry over; give feedback to DMs what to do if they want it to carry over.
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("armor").HasValue)
        {
            byte[] armorData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Value);
            NwItem armorCopy = NwItem.Deserialize(armorData);
            // Base armors between associate and copied creature must either match, or empty armor slot must match cloth armor
            if (associate.GetItemInSlot(InventorySlot.Chest) != null && associate.GetItemInSlot(InventorySlot.Chest).BaseACValue != armorCopy.BaseACValue)
            {
                obj.ItemActivator.LoginPlayer.SendServerMessage
                ("[Associate Customizer] Armor appearance not copied. The base armors must match for customization.", COLOR_RED);
                associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Delete();
            }
            if (associate.GetItemInSlot(InventorySlot.Chest) == null && armorCopy.BaseACValue > 0)
            {
                obj.ItemActivator.LoginPlayer.SendServerMessage
                ("[Associate Customizer] Armor appearance not copied. The base armor of the copied creature must be cloth for customization (you can still use the robe options for armored looks).", COLOR_RED);
                associateCustomizer.GetObjectVariable<LocalVariableString>("armor").Delete();
            }
        }
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").HasValue)
        {
            byte[] mainhandData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Value);
            NwItem mainhandCopy = NwItem.Deserialize(mainhandData);
            // Base mainhand items between associate and copied creature must match, and associate's mainhand can't be empty
            if ((associate.GetItemInSlot(InventorySlot.RightHand) != null && associate.GetItemInSlot(InventorySlot.RightHand).BaseItem != mainhandCopy.BaseItem)
                || (associate.GetItemInSlot(InventorySlot.RightHand) == null))
            {
                obj.ItemActivator.LoginPlayer.SendServerMessage
                ("[Associate Customizer] Mainhand appearance not copied. The base mainhand items must match for customization.", COLOR_RED);
                associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").Delete();
            }
        }
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").HasValue)
        {
            // Associate's mainhand can't be twohanded for offhand item data to carry over
            if (associate.GetItemInSlot(InventorySlot.RightHand) != null)
            {
                BaseItemWeaponWieldType weaponType = associate.GetItemInSlot(InventorySlot.RightHand).BaseItem.WeaponWieldType;
                BaseItemWeaponSize weaponSize = associate.GetItemInSlot(InventorySlot.RightHand).BaseItem.WeaponSize;
                bool weaponIsTwoHanded = weaponType == BaseItemWeaponWieldType.TwoHanded || weaponType == BaseItemWeaponWieldType.Bow
                    || weaponType == BaseItemWeaponWieldType.Crossbow || weaponType == BaseItemWeaponWieldType.DoubleSided
                    || (int)weaponSize > (int)associate.Size;
                if (weaponIsTwoHanded)
                {
                    obj.ItemActivator.LoginPlayer.SendServerMessage
                    ("[Associate Customizer] Offhand appearance not copied. The associate's mainhand item is held with both hands, so it can't hold an item in offhand. The base mainhand items must match for customization.", COLOR_RED);
                    associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();
                }
            }
              
            byte[] offhandData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Value);
            NwItem offhandCopy = NwItem.Deserialize(offhandData);

            // If associate's offhand is empty the copied offhand item must be torch or tools
            if (associate.GetItemInSlot(InventorySlot.LeftHand) == null && offhandCopy.BaseItem.Id != TORCH && offhandCopy.BaseItem.Id != TOOLS)
            {
                obj.ItemActivator.LoginPlayer.SendServerMessage
                ("[Associate Customizer] Offhand appearance not copied. The copied creature's offhand base item must be 'Torch' or 'Tools, Left' for customization.", COLOR_RED);
                associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();
            }
            // If associate's offhand isn't empty, the base items must match
            if (associate.GetItemInSlot(InventorySlot.LeftHand) != null && associate.GetItemInSlot(InventorySlot.LeftHand).BaseItem != offhandCopy.BaseItem)
            {
                obj.ItemActivator.LoginPlayer.SendServerMessage
                ("[Associate Customizer] Offhand appearance not copied. The base offhand items must match for customization.", COLOR_RED);
                associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").Delete();
            }
        } 

        // Cycle through every appearance and vfx variable and store each variable to the appearance tool by the associate
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("creature").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("creature"+associate.ResRef).Value = 
                associateCustomizer.GetObjectVariable<LocalVariableString>("creature");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("armor").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("armor"+associate.ResRef).Value = 
                associateCustomizer.GetObjectVariable<LocalVariableString>("armor");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("helmet").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("helmet"+associate.ResRef).Value = 
                associateCustomizer.GetObjectVariable<LocalVariableString>("helmet");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("cloak").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("cloak"+associate.ResRef).Value = 
                associateCustomizer.GetObjectVariable<LocalVariableString>("cloak");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand"+associate.ResRef).Value = 
                associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand");

        if (associateCustomizer.GetObjectVariable<LocalVariableString>("offhand").HasValue)
            associateCustomizer.GetObjectVariable<LocalVariableString>("offhand"+associate.ResRef).Value = 
                associateCustomizer.GetObjectVariable<LocalVariableString>("offhand");

        if (associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount").HasValue)
        {
            int vfxCount = associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount"+associate.ResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount");
            for (int i = 0; i < vfxCount; i++)
            {
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i+associate.ResRef).Value =
                associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i);
            }
        }

        // If the associate's custom appearance hasn't been stored before, add note to tool description
        if(!associateCustomizer.Description.Contains(associate.OriginalName))
        {
            string toolDescription = associateCustomizer.Description;
            string storedString = StringExtensions.ColorString("Custom "+associate.OriginalName, COLOR_GREEN);
            associateCustomizer.Description = $"{storedString}\n\n{toolDescription}";
        }

        // If the Associate Customizer tool hasn't been assigned to a player yet, name it after the associate's master ie tool's new owner
        if(!associateCustomizer.Name.Contains(associate.Master.OriginalFirstName))
        {
            string toolName = associateCustomizer.Name;
            associateCustomizer.Name = $"{associate.Master.OriginalFirstName}'s {toolName}";
        }

        obj.ItemActivator.LoginPlayer.SendServerMessage
            ($"[Associate Customizer] Custom appearance stored for {associate.OriginalName}", COLOR_GREEN);
        obj.ItemActivator.LoginPlayer.SendServerMessage
            ("Hand the tool over to the player and have them summon the associate to make sure it applies properly!", COLOR_WHITE);
    }

    /// <summary>
    ///     When the associate is summoned, gets the copied appearance for the associate and customizes it .
    /// </summary>
    private async void CustomizeAssociateAppearance(OnAssociateAdd obj)
    {
        // Works only if the owner is player controlled, has the tool in inventory, and the associate isn't dominated
        if (!obj.Owner.IsPlayerControlled) return;
        if (!obj.Owner.Inventory.Items.Any(item => item.Tag == TOOL_TAG)) return;
        if (obj.Associate.AssociateType == AssociateType.Dominated) return;

        NwItem associateCustomizer = obj.Owner.Inventory.Items.First(item => item.Tag == TOOL_TAG);
        NwCreature associate = obj.Associate;

        // Apply custom creature appearance, soundset, description, name
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("creature"+associate.ResRef).HasValue)
        {
            byte[] creatureData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("creature"+associate.ResRef).Value);
            NwCreature creatureCopy = NwCreature.Deserialize(creatureData);

            associate.Appearance = creatureCopy.Appearance;
            associate.PortraitId = creatureCopy.PortraitId;
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
            if (!(associate.AssociateType == AssociateType.Familiar || associate.AssociateType == AssociateType.AnimalCompanion)) 
                associate.Name = creatureCopy.Name;
        }
        // Apply custom armor appearance
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("armor"+associate.ResRef).HasValue)
        {
            byte[] armorData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("armor"+associate.ResRef).Value);
            NwItem armorCopy = NwItem.Deserialize(armorData);

            if (associate.GetItemInSlot(InventorySlot.Chest) != null)
            {
                NwItem armor = associate.GetItemInSlot(InventorySlot.Chest);
                armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
                armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
                armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
                armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
                armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
                armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
                armor.Appearance.SetArmorModel(CreaturePart.Belt, armorCopy.Appearance.GetArmorModel(CreaturePart.Belt));
                armor.Appearance.SetArmorModel(CreaturePart.LeftBicep, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftBicep));
                armor.Appearance.SetArmorModel(CreaturePart.LeftFoot, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftFoot));
                armor.Appearance.SetArmorModel(CreaturePart.LeftForearm, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftForearm));
                armor.Appearance.SetArmorModel(CreaturePart.LeftHand, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftHand));
                armor.Appearance.SetArmorModel(CreaturePart.LeftShin, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftShin));
                armor.Appearance.SetArmorModel(CreaturePart.LeftShoulder, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftShoulder));
                armor.Appearance.SetArmorModel(CreaturePart.LeftThigh, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftThigh));
                armor.Appearance.SetArmorModel(CreaturePart.Neck, armorCopy.Appearance.GetArmorModel(CreaturePart.Neck));
                armor.Appearance.SetArmorModel(CreaturePart.Pelvis, armorCopy.Appearance.GetArmorModel(CreaturePart.Pelvis));
                armor.Appearance.SetArmorModel(CreaturePart.RightBicep, armorCopy.Appearance.GetArmorModel(CreaturePart.RightBicep));
                armor.Appearance.SetArmorModel(CreaturePart.RightFoot, armorCopy.Appearance.GetArmorModel(CreaturePart.RightFoot));
                armor.Appearance.SetArmorModel(CreaturePart.RightForearm, armorCopy.Appearance.GetArmorModel(CreaturePart.RightForearm));
                armor.Appearance.SetArmorModel(CreaturePart.RightHand, armorCopy.Appearance.GetArmorModel(CreaturePart.RightHand));
                armor.Appearance.SetArmorModel(CreaturePart.RightShin, armorCopy.Appearance.GetArmorModel(CreaturePart.RightShin));
                armor.Appearance.SetArmorModel(CreaturePart.RightShoulder, armorCopy.Appearance.GetArmorModel(CreaturePart.RightShoulder));
                armor.Appearance.SetArmorModel(CreaturePart.RightThigh, armorCopy.Appearance.GetArmorModel(CreaturePart.RightThigh));
                armor.Appearance.SetArmorModel(CreaturePart.Robe, armorCopy.Appearance.GetArmorModel(CreaturePart.Robe));
                armor.Appearance.SetArmorModel(CreaturePart.Torso, armorCopy.Appearance.GetArmorModel(CreaturePart.Torso));
            }
            if (associate.GetItemInSlot(InventorySlot.Chest) == null)
            {
                armorCopy.Clone(associate);
                if (associate.Inventory.Items.Any(item => item == armorCopy))
                {
                    armorCopy.RemoveItemProperties();
                    armorCopy.Droppable = false;
                    associate.RunEquip(armorCopy, InventorySlot.Chest);
                }
                
                /* NwItem? armor = await NwItem.Create("newb_cloth", associate);
                // Failsafe if resref is missing
                if (associate.Inventory.Items.Any(item => item.ResRef == "newb_cloth"))
                {
                    armor.Droppable = false;
                    associate.RunEquip(armor, InventorySlot.Chest);
                    armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
                    armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
                    armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
                    armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
                    armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
                    armor.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, armorCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
                    armor.Appearance.SetArmorModel(CreaturePart.Belt, armorCopy.Appearance.GetArmorModel(CreaturePart.Belt));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftBicep, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftBicep));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftFoot, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftFoot));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftForearm, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftForearm));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftHand, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftHand));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftShin, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftShin));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftShoulder, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftShoulder));
                    armor.Appearance.SetArmorModel(CreaturePart.LeftThigh, armorCopy.Appearance.GetArmorModel(CreaturePart.LeftThigh));
                    armor.Appearance.SetArmorModel(CreaturePart.Neck, armorCopy.Appearance.GetArmorModel(CreaturePart.Neck));
                    armor.Appearance.SetArmorModel(CreaturePart.Pelvis, armorCopy.Appearance.GetArmorModel(CreaturePart.Pelvis));
                    armor.Appearance.SetArmorModel(CreaturePart.RightBicep, armorCopy.Appearance.GetArmorModel(CreaturePart.RightBicep));
                    armor.Appearance.SetArmorModel(CreaturePart.RightFoot, armorCopy.Appearance.GetArmorModel(CreaturePart.RightFoot));
                    armor.Appearance.SetArmorModel(CreaturePart.RightForearm, armorCopy.Appearance.GetArmorModel(CreaturePart.RightForearm));
                    armor.Appearance.SetArmorModel(CreaturePart.RightHand, armorCopy.Appearance.GetArmorModel(CreaturePart.RightHand));
                    armor.Appearance.SetArmorModel(CreaturePart.RightShin, armorCopy.Appearance.GetArmorModel(CreaturePart.RightShin));
                    armor.Appearance.SetArmorModel(CreaturePart.RightShoulder, armorCopy.Appearance.GetArmorModel(CreaturePart.RightShoulder));
                    armor.Appearance.SetArmorModel(CreaturePart.RightThigh, armorCopy.Appearance.GetArmorModel(CreaturePart.RightThigh));
                    armor.Appearance.SetArmorModel(CreaturePart.Robe, armorCopy.Appearance.GetArmorModel(CreaturePart.Robe));
                    armor.Appearance.SetArmorModel(CreaturePart.Torso, armorCopy.Appearance.GetArmorModel(CreaturePart.Torso));
                }
                else associate.Master?.ControllingPlayer?.SendServerMessage("[Associate Customizer] Dummy armor resref missing, alert devs.", COLOR_RED); */
            }
        }
        // Apply custom helmet appearance
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("helmet"+associate.ResRef).HasValue)
        {
            byte[] helmetData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("helmet"+associate.ResRef).Value);
            NwItem helmetCopy = NwItem.Deserialize(helmetData);

            if (associate.GetItemInSlot(InventorySlot.Head) != null)
            {
                NwItem helmet = associate.GetItemInSlot(InventorySlot.Head);
                helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
                helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
                helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
                helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
                helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
                helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
                helmet.Appearance.SetSimpleModel(helmetCopy.Appearance.GetSimpleModel());       
            }
            if (associate.GetItemInSlot(InventorySlot.Head) == null)
            {
                NwItem? helmet = await NwItem.Create("hood", associate);
                if (associate.Inventory.Items.Any(item => item.ResRef == "hood"))
                {
                    helmet.Droppable = false;
                    associate.RunEquip(helmet, InventorySlot.Head);
                    helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
                    helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
                    helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
                    helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
                    helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
                    helmet.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, helmetCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
                    helmet.Appearance.SetSimpleModel(helmetCopy.Appearance.GetSimpleModel());
                }
                else associate.Master?.ControllingPlayer?.SendServerMessage("[Associate Customizer] Dummy helmet resref missing, alert devs.", COLOR_RED);
            }
        }
        // Apply custom cloak appearance
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("cloak"+associate.ResRef).HasValue)
        {
            byte[] cloakData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("cloak"+associate.ResRef).Value);
            NwItem cloakCopy = NwItem.Deserialize(cloakData);

            if (associate.GetItemInSlot(InventorySlot.Cloak) != null)
            {
                NwItem cloak = associate.GetItemInSlot(InventorySlot.Cloak);
                cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
                cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
                cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
                cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
                cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
                cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
                cloak.Appearance.SetSimpleModel(cloakCopy.Appearance.GetSimpleModel());
            }
            if (associate.GetItemInSlot(InventorySlot.Cloak) == null)
            {
                NwItem? cloak = await NwItem.Create("itc_cl_basic07", associate);
                if (associate.Inventory.Items.Any(item => item.ResRef == "itc_cl_basic07"))
                {
                    cloak.Droppable = false;
                    associate.RunEquip(cloak, InventorySlot.Cloak);
                    cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth1));
                    cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Cloth2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Cloth2));
                    cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather1));
                    cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Leather2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Leather2));
                    cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal1, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal1));
                    cloak.Appearance.SetArmorColor(ItemAppearanceArmorColor.Metal2, cloakCopy.Appearance.GetArmorColor(ItemAppearanceArmorColor.Metal2));
                    cloak.Appearance.SetSimpleModel(cloakCopy.Appearance.GetSimpleModel());
                }
                else associate.Master?.ControllingPlayer?.SendServerMessage("[Associate Customizer] Dummy cloak resref missing, alert devs.", COLOR_RED);
            }
        }
        // Apply custom mainhand appearance
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand"+associate.ResRef).HasValue)
        {
            byte[] mainhandData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("mainhand"+associate.ResRef).Value);
            NwItem mainhandCopy = NwItem.Deserialize(mainhandData);

            NwItem mainhand = associate.GetItemInSlot(InventorySlot.RightHand);
            mainhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Bottom, mainhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Bottom));
            mainhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Middle, mainhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Middle));
            mainhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Top, mainhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Top));
            mainhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, mainhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom));
            mainhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, mainhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle));
            mainhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, mainhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top));
        }
        // Apply custom offhand appearance
        if (associateCustomizer.GetObjectVariable<LocalVariableString>("offhand"+associate.ResRef).HasValue)
        {
            byte[] offhandData = Convert.FromBase64String(associateCustomizer.GetObjectVariable<LocalVariableString>("offhand"+associate.ResRef).Value);
            NwItem offhandCopy = NwItem.Deserialize(offhandData);

            if (associate.GetItemInSlot(InventorySlot.LeftHand) != null)
            {
                NwItem offhand = associate.GetItemInSlot(InventorySlot.LeftHand);
                if (offhand.BaseItem.Category == BaseItemCategory.Shield)
                {
                    offhand.Appearance.SetSimpleModel(offhandCopy.Appearance.GetSimpleModel());
                }
                else
                {
                    offhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Bottom, offhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Bottom));
                    offhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Middle, offhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Middle));
                    offhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Top, offhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Top));
                    offhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, offhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom));
                    offhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, offhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle));
                    offhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, offhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top));
                }
            }
            if (associate.GetItemInSlot(InventorySlot.LeftHand) == null && offhandCopy.BaseItem.Id == TORCH)
            {
                NwItem? offhand = await NwItem.Create("hldb_bartray", associate);
                if (associate.Inventory.Items.Any(item => item.ResRef == "hldb_bartray"))
                {
                    offhand.Droppable = false;
                    associate.RunEquip(offhand, InventorySlot.LeftHand);
                    offhand.Appearance.SetSimpleModel(offhandCopy.Appearance.GetSimpleModel());
                }
                else associate.Master?.ControllingPlayer?.SendServerMessage("[Associate Customizer] Dummy 'Torch' resref missing, alert devs.", COLOR_RED);
            }
            if (associate.GetItemInSlot(InventorySlot.LeftHand) == null && offhandCopy.BaseItem.Id == TOOLS)
            {
                NwItem? offhand = await NwItem.Create("hldb_bucket", associate);
                if (associate.Inventory.Items.Any(item => item.ResRef == "hldb_bucket"))
                {
                    offhand.Droppable = false;
                    associate.RunEquip(offhand, InventorySlot.LeftHand);
                    offhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Bottom, offhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Bottom));
                    offhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Middle, offhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Middle));
                    offhand.Appearance.SetWeaponColor(ItemAppearanceWeaponColor.Top, offhandCopy.Appearance.GetWeaponColor(ItemAppearanceWeaponColor.Top));
                    offhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, offhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom));
                    offhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, offhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle));
                    offhand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, offhandCopy.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top));
                }
                else associate.Master?.ControllingPlayer?.SendServerMessage("[Associate Customizer] Dummy 'Tools, Left' resref missing, alert devs.", COLOR_RED);
            }
        }
        // Apply custom visual effects
        if (associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount"+associate.ResRef).Value > 0)
        {
            int vfxCount = associateCustomizer.GetObjectVariable<LocalVariableInt>("vfxcount"+associate.ResRef).Value;
            for (int i = 0; i < vfxCount; i++)
            {   
                int vfxId = associateCustomizer.GetObjectVariable<LocalVariableInt>("vfx"+i+associate.ResRef).Value;
                Effect vfx = Effect.VisualEffect((VfxType)vfxId);
                vfx.SubType = EffectSubType.Supernatural;
                associate.ApplyEffect(EffectDuration.Permanent, vfx);
            }
        }
    }
}