using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Groups;

public class GroupPermissions
{
    [Key] public Guid Id;

    public required Guid GroupPositionId { get; set; }
    [ForeignKey("GroupPositionId")] public GroupPosition? GroupPosition { get; set; }

    public bool CanInvite { get; set; }
    public bool CanKick { get; set; }
    public bool CanPromote { get; set; }
    public bool CanDemote { get; set; }
    public bool CanDeposit { get; set; }
    public bool CanWithdraw { get; set; }
    public bool CanVote { get; set; }
}
