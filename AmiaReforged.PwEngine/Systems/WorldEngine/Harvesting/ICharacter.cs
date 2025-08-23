using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface ICharacter
{
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot> GetEquipment();
    List<SkillData> GetSkills();
}