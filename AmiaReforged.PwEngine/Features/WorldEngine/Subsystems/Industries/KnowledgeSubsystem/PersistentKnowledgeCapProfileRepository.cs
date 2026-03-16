using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// EF Core implementation of <see cref="IKnowledgeCapProfileRepository"/>.
/// </summary>
[ServiceBinding(typeof(IKnowledgeCapProfileRepository))]
public class PersistentKnowledgeCapProfileRepository : IKnowledgeCapProfileRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _ctx;

    public PersistentKnowledgeCapProfileRepository(PwContextFactory factory)
    {
        _ctx = factory.CreateDbContext();
    }

    public List<KnowledgeCapProfile> GetAll()
    {
        try
        {
            return _ctx.KnowledgeCapProfiles.OrderBy(p => p.Tag).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get all KnowledgeCapProfiles");
            return [];
        }
    }

    public KnowledgeCapProfile? GetByTag(string tag)
    {
        try
        {
            return _ctx.KnowledgeCapProfiles.FirstOrDefault(p => p.Tag == tag);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to get KnowledgeCapProfile by tag '{tag}'");
            return null;
        }
    }

    public void Add(KnowledgeCapProfile profile)
    {
        try
        {
            _ctx.KnowledgeCapProfiles.Add(profile);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to add KnowledgeCapProfile '{profile.Tag}'");
        }
    }

    public void Update(KnowledgeCapProfile profile)
    {
        try
        {
            _ctx.KnowledgeCapProfiles.Update(profile);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to update KnowledgeCapProfile '{profile.Tag}'");
        }
    }

    public bool Delete(string tag)
    {
        try
        {
            KnowledgeCapProfile? profile = _ctx.KnowledgeCapProfiles.FirstOrDefault(p => p.Tag == tag);
            if (profile == null) return false;

            _ctx.KnowledgeCapProfiles.Remove(profile);
            _ctx.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to delete KnowledgeCapProfile '{tag}'");
            return false;
        }
    }

    public bool IsInUse(string tag)
    {
        try
        {
            return _ctx.KnowledgeProgressions.Any(p => p.CapProfileTag == tag);
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to check if KnowledgeCapProfile '{tag}' is in use");
            return false;
        }
    }
}
