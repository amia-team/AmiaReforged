using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Groups;

public class GroupPosition
{
    [Key] public Guid Id;

    public required Guid GroupId { get; set; }
    [ForeignKey("GroupId")] public Group? Group { get; set; }

    public required string Name { get; set; }
    public required GroupPermissions Permissions { get; set; }
}