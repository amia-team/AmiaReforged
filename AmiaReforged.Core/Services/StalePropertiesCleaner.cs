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
        if (player.ControlledCreature is not { } pc) return;

        LocalVariableInt pcCleaned = pc.GetObjectVariable<LocalVariableInt>(StalePropertiesCleaned);

        // Every reset player characters' local variables are wiped; if it's 0, the character hasn't logged in this reset
        if (pcCleaned.Value != 0) return;

        foreach (NwItem item in pc.Inventory.Items)
        {
            foreach (ItemProperty itemProperty in item.ItemProperties.Where(ip => ip.DurationType == EffectDuration.Temporary))
            {
                item.RemoveItemProperty(itemProperty);
                player.SendServerMessage($"Removed stale property: {itemProperty.Property.Name}".ColorString(ColorConstants.Gray));
            }
        }

        pcCleaned.Value = 1;
    }
}
