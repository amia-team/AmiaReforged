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

        // Gather the data of the appearance we want the associate customizer to change

        CopyCreatureData(associateCustomizer, creature);

        bool hasArmor = creature.GetItemInSlot(InventorySlot.Chest) != null;
        bool hasHelmet = creature.GetItemInSlot(InventorySlot.Head) != null;
        bool hasCloak = creature.GetItemInSlot(InventorySlot.Cloak) != null;
        bool hasMainHand = creature.GetItemInSlot(InventorySlot.RightHand) != null;
        bool hasOffHand = creature.GetItemInSlot(InventorySlot.LeftHand) != null;
        bool vfxCopied = CopyVfxData(associateCustomizer, creature);

        string feedbackMessage = "[Associate Customizer] Appearance data copied: Creature";

        if (hasArmor) feedbackMessage += " Armor,";
        if (hasHelmet) feedbackMessage += " Helmet,";
        if (hasCloak) feedbackMessage += " Cloak,";
        if (hasMainHand) feedbackMessage += " Main Hand,";
        if (hasOffHand) feedbackMessage += " Off Hand,";
        if (vfxCopied) feedbackMessage += " Visual Effects,";

        feedbackMessage = feedbackMessage.TrimEnd(',', ' ');
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
}
