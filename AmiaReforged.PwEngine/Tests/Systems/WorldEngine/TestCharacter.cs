using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using Anvil.API;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

/// <summary>
/// For use in testing only.
/// </summary>
/// <param name="injectedEquipment"></param>
/// <param name="skills"></param>
/// <param name="inventory"></param>
public class TestCharacter(
    Dictionary<EquipmentSlots, ItemSnapshot> injectedEquipment,
    List<SkillData> skills,
    Guid id,
    List<ItemSnapshot>? inventory = null,
    int knowledgePoints = 0)
    : ICharacter
{
    private readonly List<ItemSnapshot> _inventory = inventory ?? [];
    private int _knowledgePoints = knowledgePoints;

    public int GetKnowledgePoints()
    {
        return _knowledgePoints;
    }

    public void DeductPoints(int points)
    {
        _knowledgePoints -= points;
    }

    public Guid GetId()
    {
        return id;
    }

    public void AddItem(ItemDto item) =>
        _inventory.Add(new ItemSnapshot(item.BaseDefinition.ItemTag, item.Quality, item.BaseDefinition.Materials,
            item.BaseDefinition.JobSystemType, item.BaseDefinition.BaseItemType, null));

    public List<ItemSnapshot> GetInventory() => _inventory;

    public Dictionary<EquipmentSlots, ItemSnapshot> GetEquipment() => injectedEquipment;

    public List<SkillData> GetSkills()
    {
        return skills;
    }
}
