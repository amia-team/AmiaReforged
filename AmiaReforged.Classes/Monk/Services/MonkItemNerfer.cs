using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MonkItemNerfer))]
public class MonkItemNerfer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly string[] MonkItemResRefs =
    {
        "mastersbelt", // Advanced Belt
        "mastersbelt001", // Master's Belt
        "epx_glov_mastg" // The Master's Grit (epic monk gloves)
    };

    public MonkItemNerfer()
    {
        NwModule.Instance.OnAcquireItem += RemoveNaughtyProperties;
        Log.Info(message: "Monk Item Nerfer initialized.");
    }

    private void RemoveNaughtyProperties(ModuleEvents.OnAcquireItem eventData)
    {
        if (!eventData.AcquiredBy.IsPlayerControlled(out NwPlayer? player)
            || eventData.Item is not { } item
            || !MonkItemResRefs.Contains(eventData.Item.ResRef))
            return;

        // If a bonus feat exists for any of these items, we know it's Weapon Spec Unarmed
        ItemProperty? bonusFeat
            = item.ItemProperties.FirstOrDefault(ip => ip.Property.PropertyType == ItemPropertyType.BonusFeat);

        if (bonusFeat != null)
        {
            item.RemoveItemProperty(bonusFeat);
            SendPropertyRemovalMessage(player, item, property: bonusFeat);
        }

        // We're only worried about the Master's Grit now, removing Disc bonus and Vamp regen
        // to tone down the DC item point total to the max 12 points
        if (item.ResRef != MonkItemResRefs.Last()) return;

        foreach (ItemProperty property in item.ItemProperties)
        {
            if ((property.Property.PropertyType == ItemPropertyType.SkillBonus
                && property.IntParams[1] == (int)Skill.Discipline)
                || property.Property.PropertyType == ItemPropertyType.RegenerationVampiric)
            {
                item.RemoveItemProperty(property);
                SendPropertyRemovalMessage(player, item, property);
            }
        }
    }

    private void SendPropertyRemovalMessage(NwPlayer player, NwItem item, ItemProperty property)
    {
        player.SendServerMessage(
            "Property " +
            $"{property.Property.Name?.ToString().ColorString(ColorConstants.Green)}"+
            " has been removed from item "+
            $"{item.Name.ColorString(ColorConstants.Lime)}"
        );
    }
}
