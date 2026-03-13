using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing coinhouse (bank) definitions.
/// Supports CRUD for the admin panel.
/// </summary>
public class CoinhouseController
{
    private const string BasePath = "/api/worldengine/coinhouses";

    /// <summary>
    /// List all coinhouses with optional search and pagination.
    /// GET /api/worldengine/coinhouses?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet(BasePath)]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        using PwEngineContext context = ResolveContext();

        IQueryable<CoinHouse> query = context.CoinHouses;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();
            query = query.Where(c => c.Tag.ToLower().Contains(term));
        }

        int totalCount = await query.CountAsync();

        List<CoinHouse> items = await query
            .OrderBy(c => c.Tag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Accounts)
            .ToListAsync();

        return new ApiResult(200, new
        {
            items = items.Select(ToDto).ToArray(),
            totalCount,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Get a single coinhouse by tag.
    /// GET /api/worldengine/coinhouses/{tag}
    /// </summary>
    [HttpGet(BasePath + "/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using PwEngineContext context = ResolveContext();
        CoinHouse? coinhouse = await context.CoinHouses
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Tag == tag);

        if (coinhouse == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No coinhouse with tag '{tag}'"));
        }

        return new ApiResult(200, ToDto(coinhouse));
    }

    /// <summary>
    /// Create a new coinhouse.
    /// POST /api/worldengine/coinhouses
    /// </summary>
    [HttpPost(BasePath)]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        CoinhouseApiDto? dto = await ctx.ReadJsonBodyAsync<CoinhouseApiDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        using PwEngineContext context = ResolveContext();

        bool exists = await context.CoinHouses.AnyAsync(c => c.Tag == dto.Tag);
        if (exists)
        {
            return new ApiResult(409, new ErrorResponse(
                "Conflict", $"A coinhouse with tag '{dto.Tag}' already exists"));
        }

        CoinHouse coinhouse = FromDto(dto);
        context.CoinHouses.Add(coinhouse);
        await context.SaveChangesAsync();

        return new ApiResult(201, ToDto(coinhouse));
    }

    /// <summary>
    /// Update an existing coinhouse.
    /// PUT /api/worldengine/coinhouses/{tag}
    /// </summary>
    [HttpPut(BasePath + "/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using PwEngineContext context = ResolveContext();
        CoinHouse? existing = await context.CoinHouses
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Tag == tag);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No coinhouse with tag '{tag}'"));
        }

        CoinhouseApiDto? dto = await ctx.ReadJsonBodyAsync<CoinhouseApiDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        string? validationError = ValidateDto(dto);
        if (validationError != null)
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", validationError));
        }

        // Update mutable fields — Tag is immutable
        existing.Settlement = dto.Settlement;
        existing.EngineId = dto.EngineId;
        existing.StoredGold = dto.StoredGold;
        existing.PersonaIdString = dto.PersonaIdString;

        await context.SaveChangesAsync();

        return new ApiResult(200, ToDto(existing));
    }

    /// <summary>
    /// Delete a coinhouse.
    /// DELETE /api/worldengine/coinhouses/{tag}
    /// </summary>
    [HttpDelete(BasePath + "/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        using PwEngineContext context = ResolveContext();
        CoinHouse? existing = await context.CoinHouses
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Tag == tag);

        if (existing == null)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", $"No coinhouse with tag '{tag}'"));
        }

        // Remove associated accounts, holders, and transactions
        if (existing.Accounts is { Count: > 0 })
        {
            foreach (CoinHouseAccount account in existing.Accounts)
            {
                List<CoinHouseAccountHolder> holders = await context.CoinHouseAccountHolders
                    .Where(h => h.AccountId == account.Id)
                    .ToListAsync();
                context.CoinHouseAccountHolders.RemoveRange(holders);

                List<CoinHouseTransaction> transactions = await context.CoinHouseTransactions
                    .Where(t => t.CoinHouseAccountId == account.Id)
                    .ToListAsync();
                context.CoinHouseTransactions.RemoveRange(transactions);
            }

            context.CoinHouseAccounts.RemoveRange(existing.Accounts);
        }

        context.CoinHouses.Remove(existing);
        await context.SaveChangesAsync();

        return new ApiResult(204, new { message = "Deleted" });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static PwEngineContext ResolveContext()
    {
        PwContextFactory factory = AnvilCore.GetService<PwContextFactory>()
                                   ?? throw new InvalidOperationException("PwContextFactory service not available");
        return factory.CreateDbContext();
    }

    private static string? ValidateDto(CoinhouseApiDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (dto.Tag.Length > 100) return "Tag must not exceed 100 characters";
        if (dto.Settlement <= 0) return "Settlement must be a positive integer";
        if (dto.StoredGold < 0) return "StoredGold cannot be negative";
        return null;
    }

    private static object ToDto(CoinHouse c)
    {
        return new
        {
            c.Id,
            c.Tag,
            c.Settlement,
            c.EngineId,
            c.StoredGold,
            c.PersonaIdString,
            AccountCount = c.Accounts?.Count ?? 0,
            TotalDeposits = c.Accounts?.Sum(a => a.Debit) ?? 0,
            TotalCredits = c.Accounts?.Sum(a => a.Credit) ?? 0
        };
    }

    private static CoinHouse FromDto(CoinhouseApiDto dto)
    {
        return new CoinHouse
        {
            Tag = dto.Tag.Trim().ToLowerInvariant(),
            Settlement = dto.Settlement,
            EngineId = dto.EngineId == Guid.Empty ? Guid.NewGuid() : dto.EngineId,
            StoredGold = dto.StoredGold,
            PersonaIdString = dto.PersonaIdString
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DTOs
    // ═══════════════════════════════════════════════════════════════════

    private record CoinhouseApiDto
    {
        public string Tag { get; init; } = string.Empty;
        public int Settlement { get; init; }
        public Guid EngineId { get; init; }
        public int StoredGold { get; init; }
        public string? PersonaIdString { get; init; }
    }
}
