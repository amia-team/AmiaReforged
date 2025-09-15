using System.Text;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(StalePropertiesCleaner))]
public class StalePropertiesCleaner
{
    private const string StalePropertiesCleaned = "stale_properties_cleaned";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StalePropertiesCleaner()
    {
        NwModule.Instance.OnClientEnter += CleanStaleProperties;
        Log.Info("Stale Properties Cleaner initialized.");
    }

    /// <summary>
    /// Sometimes temporary item properties get stuck, this just makes sure they're properly removed on the character's
    /// first server login every reset
    /// </summary>
    private void CleanStaleProperties(ModuleEvents.OnClientEnter eventData)
    {
        NwPlayer player = eventData.Player;
        if (player.ControlledCreature is not { } playerCharacter) return;

        CleanPropertiesWithLostContext(playerCharacter);

        LocalVariableInt pcCleaned = playerCharacter.GetObjectVariable<LocalVariableInt>(StalePropertiesCleaned);

        // Every reset player characters' local variables are wiped; if it's 0, the character hasn't logged in this reset
        if (pcCleaned.Value != 0) return;

        Dictionary<NwItem, ItemProperty[]> removedPropertiesByItem = GetStaleProperties(playerCharacter);

        if (removedPropertiesByItem.Count != 0)
        {
            string cleanupMessage = CleanAndLogItems(removedPropertiesByItem);

            player.SendServerMessage(cleanupMessage);
        }

        pcCleaned.Value = 1;
    }

    /// <summary>
    /// Sometimes temp properties get stuck on the character when the context of whoever created the property is lost
    /// </summary>
    private void CleanPropertiesWithLostContext(NwCreature playerCharacter)
    {
        foreach (InventorySlot slot in Enum.GetValues(typeof(InventorySlot)))
        {
            NwItem? item = playerCharacter.GetItemInSlot(slot);

            if (item == null) continue;

            foreach (ItemProperty property in
                     item.ItemProperties.Where(ip => ip.DurationType == EffectDuration.Temporary && ip.Creator == null))
                item.RemoveItemProperty(property);
        }
    }

    private Dictionary<NwItem, ItemProperty[]> GetStaleProperties(NwCreature playerCharacter)
    {
        Dictionary<NwItem, ItemProperty[]> removedPropertiesByItem = new();

        foreach (NwItem item in playerCharacter.Inventory.Items)
        {
            ItemProperty[] propertiesToRemove =
                item.ItemProperties.Where(ip => ip.DurationType == EffectDuration.Temporary).ToArray();

            if (propertiesToRemove.Length == 0) continue;

            removedPropertiesByItem[item] = propertiesToRemove;
        }

        foreach (InventorySlot slot in Enum.GetValues(typeof(InventorySlot)))
        {
            NwItem? item = playerCharacter.GetItemInSlot(slot);

            if (item == null) continue;

            ItemProperty[] propertiesToRemove =
                item.ItemProperties.Where(ip => ip.DurationType == EffectDuration.Temporary).ToArray();

            if (propertiesToRemove.Length == 0) continue;

            removedPropertiesByItem[item] = propertiesToRemove;
        }

        return removedPropertiesByItem;
    }

    private string CleanAndLogItems(Dictionary<NwItem, ItemProperty[]> removedPropertiesByItem)
    {
        StringBuilder cleanupMessage = new("Stale properties cleaned:");

        foreach ((NwItem? item, ItemProperty[]? propertiesToRemove) in removedPropertiesByItem)
        {
            cleanupMessage.Append($"\n{item.Name}: ");
            string propertyNames = string.Join(", ", propertiesToRemove
                .Select(p => p.Spell?.Name ?? p.Property.Name));
            cleanupMessage.Append(propertyNames);

            foreach (ItemProperty itemProperty in propertiesToRemove)
            {
                item.RemoveItemProperty(itemProperty);
            }
        }

        return cleanupMessage.ToString().ColorString(ColorConstants.Gray);
    }
}
