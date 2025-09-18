namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public class CharacterStatistics
{
    public Guid Id { get; init; }
    public Guid CharacterId { get; init; }
    public int KnowledgePoints { get; set; }
    public int TimesDied { get; set; }
    public int TimesRankedUp { get; set; }
    public int IndustriesJoined { get; set; }
    public int PlayTime { get; set; }
}
