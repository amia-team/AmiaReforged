using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.EconomySystem.Entities;

public class BaseItemDefinition
{
    public BaseItemType BaseType { get; set; }
    public ItemType Category { get; set; }
    public List<MaterialEnum> SupportedMaterials { get; set; }
    public float CostModifier { get; set; }
    public List<EconomyResourceNode> ObtainableFrom { get; set; }
}

public class EconomyResourceNode
{
    public string Name { get; set; }
    public List<Region> FoundIn { get; set; }
}

public class Region
{
    public string Name { get; set; }
    public List<NwArea> Areas { get; set; }
    public List<RegionContender> Contenders { get; set; }
}

public class RegionContender
{
    [Key] public long Id { get; set; }
    public Organization Contender { get; set; }
    public int Influence { get; set; }
    public int MilitaryPresence { get; set; }
    public int CulturalInfluence { get; set; }
}

public class Organization
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public List<OrganizationMember> Members { get; set; }
}

public class OrganizationMember
{
    [Key] public long Id { get; set; }
    public long CharacterId { get; set; }
    [ForeignKey("CharacterId")] public WorldCharacter Member { get; set; }
    public long OrganizationId { get; set; }
    [ForeignKey("OrganizationId")] public Organization Organization { get; set; }

    public long RankId { get; set; }
    [ForeignKey("RankId")] public OrganizationRank Rank { get; set; }
}

public class OrganizationRank
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<OrganizationPermission> Permissions { get; set; }
}

public enum OrganizationPermission
{
    AddMember,
    RemoveMember,
    ChangeRank,
    ChangePermissions,
    ChangeName
}