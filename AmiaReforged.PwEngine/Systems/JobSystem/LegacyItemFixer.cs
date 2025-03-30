using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.JobSystem;

[ServiceBinding(typeof(LegacyItemFixer))]
public class LegacyItemFixer
{
    private readonly MaterialResRefMapper _legacyMaterialMapper;
    private readonly ItemTypeResRefMapper _resRefMapper;
    private BaseItemTypeMapper _baseTypeMapper;
    private const string WelcomeAreResRef = "welcometotheeete";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LegacyItemFixer(MaterialResRefMapper legacyMaterialMapper, ItemTypeResRefMapper resRefMapper,
        BaseItemTypeMapper baseItemTypeMapper)
    {
        _legacyMaterialMapper = legacyMaterialMapper;
        _resRefMapper = resRefMapper;
        _baseTypeMapper = baseItemTypeMapper;

        NwArea? entryArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == WelcomeAreResRef);

        if (entryArea == null)
        {
            Log.Error($"Could not find the entry area with resref {WelcomeAreResRef}");
            Log.Error("Service initialization failed.");
            return;
        }

        entryArea.OnEnter += FixLegacyItems;
    }

    private void FixLegacyItems(AreaEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;

        if (player.LoginCreature == null)
        {
            Log.Error($"Player {player.PlayerName} has no login creature.");
            return;
        }

        foreach (NwItem item in player.LoginCreature.Inventory.Items.Where(i =>
                     i.LocalVariables.Any(lv => lv.Name != "JsFixed") && i.ResRef.StartsWith("js_")))
        {
            ItemType itemType = _resRefMapper.MapFrom(item.ResRef);

            HandleMissingMaterial(item);

            if (itemType == ItemType.Ingot || itemType == ItemType.Ore)
            {
                if (item.ItemProperties.Any(ip => ip.Property.PropertyType == ItemPropertyType.Material))
                {
                    NWScript.SetLocalInt(item, "JsFixed", NWScript.TRUE);
                }
                else
                {
                    Log.Error($"Item {item.Name} has no material property.");
                }
            }
        }
    }

    private void HandleMissingMaterial(NwItem item)
    {
        ItemType itemType = _resRefMapper.MapFrom(item.ResRef);

        // Try to map using the base type...
        if (itemType == ItemType.Unknown)
        {
            itemType = _baseTypeMapper.MapFrom(item);
        }

        if (itemType == ItemType.Unknown)
        {
            Log.Error($"Item {item.Name} has an unknown type.");
            return;
        }

        // Nothing to do, so move on.
        if (item.ItemProperties.Any(ip => ip.Property.PropertyType == ItemPropertyType.Material)) return;

        List<ItemType> handledTypes =
        [
            ItemType.Ingot,
            ItemType.Ore,
            ItemType.Plank,
            ItemType.Stone,
            ItemType.Gem,
            ItemType.Grain,
            ItemType.Flour,
            ItemType.FoodIngredient,
            ItemType.Food
        ];
        
        if (handledTypes.Contains(itemType))
        {
            AddGenericMaterial(item);
        }
    }

    private void AddGenericMaterial(NwItem item)
    {
        MaterialEnum matType = _legacyMaterialMapper.MapFrom(item.ResRef);
        if (matType == MaterialEnum.None)
        {
            Log.Error($"Item {item.Name} has an unknown material.");
            return;
        }

        ItemProperty material = ItemProperty.Material((int)matType);

        item.AddItemProperty(material, EffectDuration.Permanent);
    }
}