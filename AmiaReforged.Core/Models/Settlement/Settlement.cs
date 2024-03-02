using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core.Models.Settlement;

public class Settlement
{
    [Key] public Guid Id;
    public Economy Economy;
    public string Name;

    public Guid StateId;
    [ForeignKey("StateId")] public State State;
}

public class Economy
{
    [Key] public Guid Id;

    public List<Warehouse> Warehouses;
}

public class RawMaterial
{
    [Key] public Guid Id;
    public string Name;
    public MaterialType Type;
}

public class ProcessedMaterial
{
    [Key] public Guid Id;
    public string Name;
    public MaterialType Type;
    public float Quality;
}

public class RefinedMaterial
{
    [Key] public Guid Id;
    public string Name;
    public MaterialType Type;
    public float Quality;
}

public enum Quality
{
    Poor,
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum MaterialType
{
    Metal,
    Wood,
    Stone,
    Cloth,
    Leather,
    Glass,
    Ceramic,
    Paper
}

public enum BaseItemType
{
    Weapon,
    Armor,
    Clothing,
    Jewelry,
    Tool,
    Consumable,
    Container,
    Misc
}

public class Warehouse
{
    [Key] public Guid Id;
}

public class State
{
    [Key] public Guid Id;
    public string Name;
    public Nation Nation;
}

public class Nation
{
    [Key] public Guid Id;
    public string Name;
    public List<State> States;
    public List<Settlement> Settlements;
}