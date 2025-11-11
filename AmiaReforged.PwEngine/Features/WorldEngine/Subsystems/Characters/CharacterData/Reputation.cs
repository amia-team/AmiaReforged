namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

public class Reputation
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public Guid OrganizationId { get; init; }
    public int Level { get; set; }
}
