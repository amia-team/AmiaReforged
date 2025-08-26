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
    private const BaseItemType Tools = (BaseItemType)113;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AssociateCustomizerService()
    {
        NwModule.Instance.OnActivateItem += CopyTargetAppearance;
        NwModule.Instance.OnActivateItem += StoreAssociateAppearance;
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

        CopyCreatureData(associateCustomizer, creature);

        bool hasArmor = creature.GetItemInSlot(InventorySlot.Chest) != null;
        bool hasHelmet = creature.GetItemInSlot(InventorySlot.Head) != null;
        bool hasCloak = creature.GetItemInSlot(InventorySlot.Cloak) != null;
        bool hasMainHand = creature.GetItemInSlot(InventorySlot.RightHand) != null;
        bool hasOffHand = creature.GetItemInSlot(InventorySlot.LeftHand) != null;

        bool vfxCopied = CopyVfxData(associateCustomizer, creature);

        string feedbackMessage = $"[Associate Customizer] Appearance data copied from {creature.Name}: Creature,";

        if (hasArmor) feedbackMessage += " Armor,";
        if (hasHelmet) feedbackMessage += " Helmet,";
        if (hasCloak) feedbackMessage += " Cloak,";
        if (hasMainHand) feedbackMessage += " Main Hand,";
        if (hasOffHand) feedbackMessage += " Off Hand,";
        if (vfxCopied) feedbackMessage += " Visual Effects,";

        feedbackMessage = feedbackMessage.TrimEnd(',');
        feedbackMessage += ".";

        player.SendServerMessage(feedbackMessage, ColorGreen);
        player.SendServerMessage("To assign the copied appearance to an associate, target the associate with the tool.", ColorWhite);
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

        LocalVariableString creatureData = associateCustomizer.GetObjectVariable<LocalVariableString>("creature");

        if (creatureData.Value == null)
        {
            player.SendServerMessage
            ("[Associate Customizer] You must first use the tool on a non-associate creature to copy its appearance.", ColorRed);
            return;
        }

        NwCreature? creatureCopy = NwCreature.Deserialize(Convert.FromBase64String(creatureData.Value));
        if (creatureCopy == null) return;

        if (MatchEquipment(associate, creatureCopy, player) == false)
        {
            creatureData.Delete();
            return;
        }

        AssignDataToAssociate(associateCustomizer, associate);

        AssignCustomizerToPc(associateCustomizer, associate);

        player.SendServerMessage
            ($"[Associate Customizer] Custom appearance stored for {associate.OriginalName}", ColorGreen);
        player.SendServerMessage
            ("Hand the tool over to the player and have them summon the associate to make sure it applies properly!", ColorWhite);
    }

    /// <summary>
    /// Checks the matching requirements of the associate's and the copied appearance's equipment and instructs the DM
    /// if there are issues
    /// </summary>
    /// <returns>Returns true if all necessary equipment matches; otherwise returns false</returns>
    private bool MatchEquipment(NwCreature associate, NwCreature creatureCopy, NwPlayer player)
    {
        bool armorsMatch = true;

        NwItem? associateArmor = associate.GetItemInSlot(InventorySlot.Chest);
        NwItem? copiedArmor = creatureCopy.GetItemInSlot(InventorySlot.Chest);

        if (associateArmor != null && copiedArmor != null && associateArmor.BaseACValue != copiedArmor.BaseACValue)
        {
            player.SendServerMessage
                ("[Associate Customizer] Armor appearance not copied. " +
                 "The base armors must match for customization.", ColorRed);

            armorsMatch = false;
        }

        if (associateArmor == null && copiedArmor?.BaseACValue > 0)
        {
            player.SendServerMessage
            ("[Associate Customizer] Armor appearance not copied. The base armor of the copied creature must be " +
             "cloth for customization (you can still use the robe options for armored looks).", ColorRed);

            armorsMatch = false;
        }

        bool mainHandsMatch = true;

        NwItem? associateMainHand = associate.GetItemInSlot(InventorySlot.RightHand);
        NwItem? copiedMainHand = creatureCopy.GetItemInSlot(InventorySlot.RightHand);

        if (associateMainHand != null && copiedMainHand != null &&
            associateMainHand.BaseItem != copiedMainHand.BaseItem)
        {
            player.SendServerMessage
            ("[Associate Customizer] Main hand item appearance not copied. " +
             "The base main hand items must match for customization.", ColorRed);

            mainHandsMatch = false;
        }

        bool offHandsMatch = true;

        NwItem? associateOffHand = associate.GetItemInSlot(InventorySlot.LeftHand);
        NwItem? copiedOffHand = creatureCopy.GetItemInSlot(InventorySlot.LeftHand);

        if (associateOffHand != null && copiedOffHand != null && copiedOffHand.BaseItem != associateOffHand.BaseItem)
        {
            player.SendServerMessage
            ("[Associate Customizer] Off hand item appearance not copied. " +
             "The base off hand items must match for customization.", ColorRed);

            offHandsMatch = false;
        }

        if (associateOffHand == null &&
            copiedOffHand != null && copiedOffHand.BaseItem.ItemType is not (BaseItemType.Torch or Tools))
        {
            player.SendServerMessage
            ("[Associate Customizer] Off hand item appearance not copied. " +
             "When the associate doesn't have an off hand item, you can only apply base item types torch or tools.", ColorRed);

            offHandsMatch = false;
        }

        if (associateMainHand != null && copiedOffHand != null)
        {
            BaseItemWeaponWieldType weaponType = associateMainHand.BaseItem.WeaponWieldType;
            BaseItemWeaponSize weaponSize = associateMainHand.BaseItem.WeaponSize;

            bool weaponIsTwoHanded = weaponType == BaseItemWeaponWieldType.TwoHanded || weaponType == BaseItemWeaponWieldType.Bow
                || weaponType == BaseItemWeaponWieldType.Crossbow || weaponType == BaseItemWeaponWieldType.DoubleSided
                || (int)weaponSize > (int)associate.Size;

            if (weaponIsTwoHanded)
            {
                player.SendServerMessage
                ("[Associate Customizer] Offhand appearance not copied. " +
                 "The associate's main hand item is held with both hands, so it can't hold an item in off hand. " +
                 "The base main hand items must match for customization.", ColorRed);

                offHandsMatch = false;
            }
        }

        return armorsMatch && mainHandsMatch && offHandsMatch;
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
    /// Assigns the Associate Customizer to the PC who it belongs to by renaming it
    /// </summary>
    private void AssignCustomizerToPc(NwItem associateCustomizer, NwCreature associate)
    {
        if (associate.Master == null) return;

        if (associateCustomizer.Name.Contains(associate.Master.OriginalFirstName)) return;

        string toolName = associateCustomizer.Name;

        associateCustomizer.Name = $"{associate.Master.OriginalFirstName}'s {toolName}";
    }
}
