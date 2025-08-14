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

        LocalVariableInt pcCleaned = playerCharacter.GetObjectVariable<LocalVariableInt>(StalePropertiesCleaned);

        // Every reset player characters' local variables are wiped; if it's 0, the character hasn't logged in this reset
        if (pcCleaned.Value != 0) return;
        
        foreach (NwItem item in playerCharacter.Inventory.Items)
        {
            List<ItemProperty> propertiesToRemove =
                item.ItemProperties.Where(ip => ip.DurationType == EffectDuration.Temporary).ToList();

            foreach (ItemProperty itemProperty in propertiesToRemove)
            {
                item.RemoveItemProperty(itemProperty);
            }
        }

        foreach (InventorySlot slot in Enum.GetValues(typeof(InventorySlot)))
        {
            // Now you can use the 'slot' variable to get the item
            NwItem? item = playerCharacter.GetItemInSlot(slot);

            if (item == null) continue;

            List<ItemProperty> propertiesToRemove =
                item.ItemProperties.Where(ip => ip.DurationType == EffectDuration.Temporary).ToList();

            foreach (ItemProperty itemProperty in propertiesToRemove)
            {
                item.RemoveItemProperty(itemProperty);
            }
        }

        pcCleaned.Value = 1;
    }
}
