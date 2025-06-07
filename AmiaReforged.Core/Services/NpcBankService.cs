using AmiaReforged.Core.Models;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(NpcBankService))]
public class NpcBankService(DatabaseContextFactory factory)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public IEnumerable<Npc> GetNpcs(string dmKey)
    {
        AmiaDbContext ctx = factory.CreateDbContext();
        IEnumerable<Npc> npcs = new List<Npc>();
        Log.Info($"Getting NPCs for {dmKey}");

        try
        {
            npcs = ctx.Npcs.Where(n => n.Public || n.DmCdKey == dmKey);
            Log.Info($"Found {npcs.Count()} npcs");
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return npcs;
    }

    public void AddNpc(Npc npc)
    {
        AmiaDbContext ctx = factory.CreateDbContext();

        try
        {
            Log.Info("adding npc");
            ctx.Add(npc);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
        
    }

    public void DeleteNpcAsync(long id)
    {
        AmiaDbContext ctx = factory.CreateDbContext();

        try
        {
            Npc? npc = ctx.Npcs.Find(id);
            
            if (npc == null)
            {
                Log.Warn("NPC not found");
                
                return;
            }
            
            ctx.Remove(npc);
            ctx.SaveChanges();
            
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void SetPublic(long npcId, bool b)
    {
        AmiaDbContext ctx = factory.CreateDbContext();

        try
        {
            Npc? npc = ctx.Npcs.Find(npcId);
            if (npc == null)
            {
                Log.Warn("NPC not found");
                return;
            }
            
            npc.Public = b;
            ctx.Npcs.Update(npc);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
        
    }
}