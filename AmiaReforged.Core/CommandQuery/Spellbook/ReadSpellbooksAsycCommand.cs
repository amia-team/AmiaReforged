using AmiaReforged.Core.CommandQuery.Types;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.CommandQuery.Spellbook;

public class ReadSpellbooksAsycCommand : IAsycCommand<PlayerReadContext, List<SavedSpellbook>>
{
    [Inject] private Lazy<DatabaseContextFactory>? DbFactory { get; init; }
    public async Task<IResult<List<SavedSpellbook>>> Execute(PlayerReadContext context)
    {
        ReadSpellbooksResult result = new();
        result.IsSuccess = false;
        result.Error = "DB connection failed";

        if (DbFactory is null) return result;

        await using AmiaDbContext db = DbFactory.Value.CreateDbContext();
        
        result.Value = await db.SavedSpellbooks.ToListAsync();
        
        result.IsSuccess = true;
        result.Error = null;

        return result;
    }
}

public class PlayerReadContext : ICommandContext
{
}

public interface ICommandContext
{
}

public class ReadSpellbooksResult : IResult<List<SavedSpellbook>>
{
    public List<SavedSpellbook> Value { get; set; }
    public bool IsSuccess { get; set; }
    public string Error { get; set; }
}