using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities.Groups;

public class Group
{
    [Key] public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public List<GroupMember> Members { get; set; } = [];
    public List<GroupPosition> Positions { get; set; } = [];
}
