namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.Characters.Entities;

public record PlayerId(string CdKey);
public record DmId(string CdKey);

public sealed class Ownership
{
    public OwnershipKind Kind { get; }
    public PlayerId? PlayerId { get; }
    public DmId? DmId { get; }

    private Ownership(OwnershipKind kind, PlayerId? playerId = null, DmId? dmId = null)
    {
        Kind = kind;
        PlayerId = playerId;
        DmId = dmId;
    }

    public static Ownership World() => new(OwnershipKind.World);
    public static Ownership DungeonMaster(DmId dmId) => new(OwnershipKind.DungeonMaster, dmId: dmId);
    public static Ownership Player(PlayerId playerId) => new(OwnershipKind.Player, playerId: playerId);

    public bool IsWorld => Kind == OwnershipKind.World;
    public bool IsDm => Kind == OwnershipKind.DungeonMaster;
    public bool IsPlayer => Kind == OwnershipKind.Player;
}
