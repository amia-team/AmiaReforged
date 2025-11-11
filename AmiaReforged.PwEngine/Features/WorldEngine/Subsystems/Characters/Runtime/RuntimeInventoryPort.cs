using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;

public class RuntimeInventoryPort(NwCreature creature) : IInventoryPort
{
    private const int MiscSmall2 = 119;
    private const int MiscSmall3 = 120;

    public void AddItem(ItemDto item)
    {
        if (creature.Location is null) return;

        NwItem? gameItem = NwItem.Create(item.BaseDefinition.ResRef, creature.Location);

        if (gameItem is null) return;

        string qualityLabel = QualityLabel.QualityLabelForItem(item.BaseDefinition.JobSystemType, item.Quality);
        gameItem.Name = qualityLabel.IsNullOrEmpty()
            ? $"{item.BaseDefinition.Name}"
            : $"{item.BaseDefinition.Name} ({qualityLabel})";

        ItemProperty quality = ItemProperty.Quality(item.Quality);
        gameItem.AddItemProperty(quality, EffectDuration.Permanent);
        gameItem.Description = item.BaseDefinition.Description;

        CalculateAddedValue(item, gameItem);
        IPWeightIncrease weight = (IPWeightIncrease)item.BaseDefinition.WeightIncreaseConstant;

        if (item.BaseDefinition.WeightIncreaseConstant >= 0)
        {
            gameItem.AddItemProperty(
                ItemProperty.WeightIncrease(weight),
                EffectDuration.Permanent);
        }

        string materialNumbers = "";
        foreach (MaterialEnum m in item.BaseDefinition.Materials)
        {
            ItemProperty matProp = ItemProperty.Material((int)m);
            gameItem.AddItemProperty(matProp, EffectDuration.Permanent);

            materialNumbers += materialNumbers.IsNullOrEmpty() ? $"{m}" : $", {m}";
        }

        NWScript.SetLocalString(gameItem, WorldConstants.MaterialLvar, materialNumbers);

        if (item.BaseDefinition.BaseItemType
            is NWScript.BASE_ITEM_MISCSMALL
            or MiscSmall2
            or MiscSmall3)
        {
            gameItem.Appearance.SetSimpleModel((ushort)(item.BaseDefinition.Appearance.SimpleModelNumber ?? 1));

            NWScript.SetLocalInt(gameItem, WorldConstants.ItemVariableQuality, (int)item.Quality);
        }

        creature.AcquireItem(gameItem);
    }

    private static void CalculateAddedValue(ItemDto item, NwItem gameItem)
    {
        int qualityValueAdded = (int)(item.BaseDefinition.BaseValue * ItemCostHelper.GetCostMultiplier(item.Quality));
        int materialValueAdded = ItemCostHelper.GetAddedCost(item, item.BaseDefinition.Materials);

        gameItem.AddGoldValue = item.BaseDefinition.BaseValue + qualityValueAdded + materialValueAdded;
    }

    public List<ItemSnapshot> GetInventory()
    {
        return [];
    }

    public Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment()
    {
        return new Dictionary<EquipmentSlots, ItemSnapshot?>()
        {
            { EquipmentSlots.Head, ToItemSnapshot(creature.GetItemInSlot(InventorySlot.Head)) },
            { EquipmentSlots.RightHand, ToItemSnapshot(creature.GetItemInSlot(InventorySlot.RightHand)) },
            { EquipmentSlots.LeftHand, ToItemSnapshot(creature.GetItemInSlot(InventorySlot.LeftHand)) },
        };
    }

    private ItemSnapshot? ToItemSnapshot(NwItem? equipped)
    {
        if (equipped is null) return null;

        int storedQuality = NWScript.GetLocalInt(equipped, WorldConstants.ItemVariableQuality);
        int storedToolType = NWScript.GetLocalInt(equipped, WorldConstants.ToolTypeVariable);

        IPQuality quality = storedQuality == 0 ? IPQuality.Unknown : (IPQuality)storedQuality;
        return new ItemSnapshot(equipped.Tag,
            equipped.Name,
            equipped.Description,
            quality,
            [],
            (JobSystemItemType)storedToolType,
            NWScript.GetBaseItemType(equipped),
            equipped.Serialize());
    }

    public static RuntimeInventoryPort For(NwCreature creature)
    {
        return new RuntimeInventoryPort(creature);
    }
}
