using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Queries;
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
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<CoinHouse> matches = await facade.QueryAsync<SearchCoinhouseDefinitionsQuery, List<CoinHouse>>(
            new SearchCoinhouseDefinitionsQuery { SearchTerm = search }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<CoinHouse> items = matches
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

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

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CoinHouse? coinhouse = await facade.QueryAsync<GetCoinhouseDefinitionQuery, CoinHouse?>(
            new GetCoinhouseDefinitionQuery { Tag = tag }, ctx.CancellationToken);

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

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CoinHouse coinhouse = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new CreateCoinhouseCommand
        {
            Coinhouse = coinhouse
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        CoinHouse? created = await facade.QueryAsync<GetCoinhouseDefinitionQuery, CoinHouse?>(
            new GetCoinhouseDefinitionQuery { Tag = coinhouse.Tag }, ctx.CancellationToken);

        return new ApiResult(201, ToDto(created ?? coinhouse));
    }

    /// <summary>
    /// Update an existing coinhouse.
    /// PUT /api/worldengine/coinhouses/{tag}
    /// </summary>
    [HttpPut(BasePath + "/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

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

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CoinHouse coinhouse = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new UpdateCoinhouseCommand
        {
            Tag = tag,
            Coinhouse = coinhouse
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No coinhouse with tag", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

        CoinHouse? updated = await facade.QueryAsync<GetCoinhouseDefinitionQuery, CoinHouse?>(
            new GetCoinhouseDefinitionQuery { Tag = tag }, ctx.CancellationToken);

        return new ApiResult(200, ToDto(updated ?? coinhouse));
    }

    /// <summary>
    /// Delete a coinhouse.
    /// DELETE /api/worldengine/coinhouses/{tag}
    /// </summary>
    [HttpDelete(BasePath + "/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteCoinhouseCommand
        {
            Tag = tag
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage));
        }

        return new ApiResult(204, new { message = "Deleted" });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

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
