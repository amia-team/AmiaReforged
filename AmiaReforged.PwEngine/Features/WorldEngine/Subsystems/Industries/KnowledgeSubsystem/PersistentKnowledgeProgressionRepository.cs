using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// EF Core implementation of <see cref="IKnowledgeProgressionRepository"/>.
/// </summary>
[ServiceBinding(typeof(IKnowledgeProgressionRepository))]
public class PersistentKnowledgeProgressionRepository : IKnowledgeProgressionRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _ctx;

    public PersistentKnowledgeProgressionRepository(PwContextFactory factory)
    {
        _ctx = factory.CreateDbContext();
    }

    public KnowledgeProgression? GetByCharacterId(Guid characterId)
    {
        try
        {
            return _ctx.KnowledgeProgressions.FirstOrDefault(p => p.CharacterId == characterId);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to get KnowledgeProgression for character {characterId}");
            return null;
        }
    }

    public KnowledgeProgression GetOrCreate(Guid characterId)
    {
        KnowledgeProgression? existing = GetByCharacterId(characterId);
        if (existing != null) return existing;

        KnowledgeProgression progression = new()
        {
            CharacterId = characterId,
            EconomyEarnedKnowledgePoints = 0,
            LevelUpKnowledgePoints = 0,
            AccumulatedProgressionPoints = 0,
            CapProfileTag = null
        };

        Add(progression);
        return progression;
    }

    public void Update(KnowledgeProgression progression)
    {
        try
        {
            _ctx.KnowledgeProgressions.Update(progression);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to update KnowledgeProgression for character {progression.CharacterId}");
        }
    }

    public void Add(KnowledgeProgression progression)
    {
        try
        {
            _ctx.KnowledgeProgressions.Add(progression);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to add KnowledgeProgression for character {progression.CharacterId}");
        }
    }
}
