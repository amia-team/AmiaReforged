using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.HarvestActions;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(NodeEventHandler))]
public class NodeEventHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<Guid, ResourceNodeInstance> _nodeInstances = new();

    private readonly Location? _setupLocation;

    private readonly EconomyDefinitions _definitions;

    public NodeEventHandler(EconomyDefinitions definitions)
    {
        NwWaypoint? firstOrDefault = NwObject.FindObjectsWithTag<NwWaypoint>("ds_copy").FirstOrDefault();

        _setupLocation = firstOrDefault?.Location;

        _definitions = definitions;

        if (firstOrDefault == null)
        {
            Log.Error("No system waypoint found in Area To Rest");
        }
    }

    public void RegisterNode(NwPlaceable plc, ResourceNodeInstance instance)
    {
        bool success = _nodeInstances.TryAdd(plc.UUID, instance);
        if (!success)
        {
            Log.Error("Failed to register instance: UUID collision occurred...");
            return;
        }

        RegisterPlcEvents(plc, instance);
    }


    private void RegisterPlcEvents(NwPlaceable plc, ResourceNodeInstance instance)
    {
        switch (instance.Definition.HarvestAction)
        {
            case HarvestActionEnum.Undefined:
                Log.Error($"Invalid harvest for node {plc.Area?.Name}. Event not subscribed.");
                break;
            case HarvestActionEnum.Attack:
                plc.OnPhysicalAttacked += HarvestAttackableNode;
                plc.OnUsed += RedirectToAttack;
                break;
            case HarvestActionEnum.Use:
                plc.OnUsed += HarvestUsableNode;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HarvestAttackableNode(PlaceableEvents.OnPhysicalAttacked obj)
    {
        if (obj.Attacker == null)
        {
            return;
        }

        NwItem? mainHand = obj.WeaponUsed(obj.Attacker);

        if (mainHand == null)
        {
            obj.Placeable.SpeakString("*Ore cannot be harvested using your bare hands*");
            obj.Attacker.ApplyEffect(EffectDuration.Instant, Effect.Damage(1, DamageType.Bludgeoning));
            return;
        }

        ResourceNodeInstance resourceNodeInstance = _nodeInstances[obj.Placeable.UUID];
        if (ToolFromMainhand((int)mainHand.BaseItem.ItemType) !=
            resourceNodeInstance.Definition.RequiredTool)
        {
            obj.Placeable.SpeakString(
                $"*This {resourceNodeInstance.Definition.Type} requires a {resourceNodeInstance.Definition.RequiredTool} to harvest*");
            return;
        }

        // TODO: Extract to its own service.
        foreach (ResourceNodeDefinition.YieldItem itemTag in resourceNodeInstance.Definition.YieldItems)
        {
            Random rng = new();

            float attempt = rng.NextFloat(0, 1.0f);

            if (!(attempt <= itemTag.Chance)) continue;

            ItemDefinition? def = _definitions.Items.FirstOrDefault(i => i.Tag == itemTag.ItemTag);

            if (def == null)
            {
                Log.Error($"Invalid item tag defined in {resourceNodeInstance.Definition.Name}");
                continue;
            }

            if (_setupLocation == null || !_setupLocation.IsValid)
            {
                Log.Error("Setup location for spawning items was null or not valid");
                continue;
            }

            NwItem? item = NwItem.Create(def.BaseItemResRef, _setupLocation);

            if (item == null)
            {
                Log.Error($"Failed to create base item: {def.BaseItemResRef}");
                continue;
            }

            item.Appearance.SetSimpleModel((ushort)def.Appearance);
            item.Name = def.Name;
            item.Tag = def.Tag;
            item.Description = def.Description;
            item.Stolen = true;

            // TODO: Derive quality from skill and knowledge.
            int minQuality = (int)def.MinQuality.ToItemPropertyEnum();
            int maxQuality = (int)def.MaxQuality.ToItemPropertyEnum();

            int quality = rng.Next(minQuality, maxQuality);
            int valueAdjustment = quality >= NWScript.IP_CONST_QUALITY_AVERAGE
                ? def.BaseCost * quality / 10
                : -(def.BaseCost * quality / 10);

            int totalValue = (int)(item.BaseGoldValue + valueAdjustment > 0 ? item.BaseGoldValue + valueAdjustment : 1);


            MaterialDefinition? matDef = _definitions.Materials.FirstOrDefault(m => m.MaterialType == def.MaterialType);
            if (def.MaterialType != null && matDef != null)
            {
                totalValue = (int)(totalValue + totalValue * matDef.CostModifier);
            }

            NWScript.SetLocalInt(item, WorldConstants.MarketValueBaseLvar, totalValue);

            ItemProperty qualProp = ItemProperty.Quality((IPQuality)quality);
            item.AddItemProperty(qualProp, EffectDuration.Permanent);

            if (def.MaterialType != null)
            {
                Log.Info("Material not null");
                if (def.MaterialType != MaterialEnum.None)
                {
                    Log.Info($"{def.MaterialType}");
                    ItemProperty matProp = ItemProperty.Material((int)def.MaterialType);
                    item.AddItemProperty(matProp, EffectDuration.Permanent);
                }
            }

            NWScript.CopyItem(item, obj.Attacker, NWScript.TRUE);
            item.Destroy();
        }
    }

    private ToolEnum ToolFromMainhand(int itemType)
    {
        return itemType switch
        {
            NWScript.BASE_ITEM_WARHAMMER => ToolEnum.PickAxe, // TODO: Replace with picks when added to the haks
            NWScript.BASE_ITEM_BATTLEAXE => ToolEnum.Axe,
            NWScript.BASE_ITEM_HANDAXE => ToolEnum.Axe,
            NWScript.BASE_ITEM_GREATAXE => ToolEnum.Axe,
            NWScript.BASE_ITEM_DWARVENWARAXE => ToolEnum.Axe,
            _ => ToolEnum.None
        };
    }

    private void RedirectToAttack(PlaceableEvents.OnUsed obj)
    {
        obj.UsedBy.ActionAttackTarget(obj.Placeable);
    }

    private void HarvestUsableNode(PlaceableEvents.OnUsed obj)
    {
        obj.Placeable.SpeakString($"{obj.Placeable.Name} harvested");
    }
}
