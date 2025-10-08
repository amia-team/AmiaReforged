using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Groups;

public class GroupMember
{
    [Key] public Guid Id;
    public required Guid GroupId { get; set; }
    [ForeignKey("GroupId")] public Group? Group { get; set; }

    public required Guid PositionId { get; set; }
    [ForeignKey("PositionId")] public GroupPosition? Position { get; set; }

    public required Guid CharacterId { get; set; }
    [ForeignKey("CharacterId")] public PersistedCharacter? Character { get; set; }

    public bool IsLeader { get; set; }
}
