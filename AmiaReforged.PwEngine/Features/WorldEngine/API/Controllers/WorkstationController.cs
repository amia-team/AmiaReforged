using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing workstation definitions.
/// Workstations are global — shared across industries.
/// </summary>
public class WorkstationController
{
    private static readonly JsonSerializerOptions ImportOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all workstations with optional search and pagination.
    /// GET /api/worldengine/workstations?search=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet("/api/worldengine/workstations")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<Workstation> matches = await facade.QueryAsync<SearchWorkstationDefinitionsQuery, List<Workstation>>(
            new SearchWorkstationDefinitionsQuery { SearchTerm = search }, ctx.CancellationToken);

        int totalCount = matches.Count;
        List<Workstation> paged = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return await Task.FromResult(new ApiResult(200, new
        {
            items = paged.Select(ToDto),
            totalCount,
            page,
            pageSize
        }));
    }

    /// <summary>
    /// Get a single workstation by tag.
    /// GET /api/worldengine/workstations/{tag}
    /// </summary>
    [HttpGet("/api/worldengine/workstations/{tag}")]
    public static async Task<ApiResult> GetByTag(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        Workstation? workstation = await facade.QueryAsync<GetWorkstationDefinitionQuery, Workstation?>(
            new GetWorkstationDefinitionQuery { Tag = tag }, ctx.CancellationToken);
        if (workstation == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No workstation with tag '{tag}'")));
        }

        return await Task.FromResult(new ApiResult(200, ToDto(workstation)));
    }

    /// <summary>
    /// Create a new workstation definition.
    /// POST /api/worldengine/workstations
    /// </summary>
    [HttpPost("/api/worldengine/workstations")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        WorkstationDto? dto = await ctx.ReadJsonBodyAsync<WorkstationDto>();
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

        Workstation workstation = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new CreateWorkstationCommand
        {
            Workstation = workstation
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        return new ApiResult(201, ToDto(workstation));
    }

    /// <summary>
    /// Update an existing workstation definition by tag.
    /// PUT /api/worldengine/workstations/{tag}
    /// </summary>
    [HttpPut("/api/worldengine/workstations/{tag}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");

        WorkstationDto? dto = await ctx.ReadJsonBodyAsync<WorkstationDto>();
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

        dto = dto with { Tag = tag };
        Workstation workstation = FromDto(dto);
        CommandResult result = await facade.ExecuteAsync(new UpdateWorkstationCommand
        {
            Tag = tag,
            Workstation = workstation
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.StartsWith("No workstation with tag", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

        return new ApiResult(200, ToDto(workstation));
    }

    /// <summary>
    /// Delete a workstation definition by tag.
    /// DELETE /api/worldengine/workstations/{tag}
    /// </summary>
    [HttpDelete("/api/worldengine/workstations/{tag}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string tag = ctx.GetRouteValue("tag");
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DeleteWorkstationCommand
        {
            Tag = tag
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", result.ErrorMessage)));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Deleted" }));
    }

    /// <summary>
    /// Export all workstation definitions as a JSON array.
    /// GET /api/worldengine/workstations/export
    /// </summary>
    [HttpGet("/api/worldengine/workstations/export")]
    public static async Task<ApiResult> Export(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        List<Workstation> workstations = await facade.QueryAsync<SearchWorkstationDefinitionsQuery, List<Workstation>>(
            new SearchWorkstationDefinitionsQuery(), ctx.CancellationToken);

        return await Task.FromResult(new ApiResult(200,
            workstations.OrderBy(w => w.Name).Select(ToDto).ToArray()));
    }

    /// <summary>
    /// Bulk import workstation definitions from JSON.
    /// POST /api/worldengine/workstations/import
    /// </summary>
    [HttpPost("/api/worldengine/workstations/import")]
    public static async Task<ApiResult> Import(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? body = null;
        if (ctx.Request != null)
        {
            using StreamReader reader = new StreamReader(ctx.Request.InputStream);
            body = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "Request body must be a JSON array of workstation definitions"));
        }

        List<WorkstationDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<WorkstationDto>>(body, ImportOptions);
        }
        catch (JsonException)
        {
            try
            {
                WorkstationDto? single = JsonSerializer.Deserialize<WorkstationDto>(body, ImportOptions);
                dtos = single != null ? new List<WorkstationDto> { single } : null;
            }
            catch (JsonException ex)
            {
                return new ApiResult(400, new ErrorResponse("Parse error", ex.Message));
            }
        }

        if (dtos == null || dtos.Count == 0)
        {
            return new ApiResult(400, new ErrorResponse("Bad request",
                "No valid workstation definitions found in request body"));
        }

        int failed = 0;
        List<string> errors = new();
        List<(string Tag, UpsertWorkstationCommand Command)> valid = new();

        foreach (WorkstationDto dto in dtos)
        {
            string? validationError = ValidateDto(dto);
            if (validationError != null)
            {
                failed++;
                errors.Add($"{dto.Tag ?? "unknown"}: {validationError}");
                continue;
            }

            try
            {
                valid.Add((dto.Tag, new UpsertWorkstationCommand { Workstation = FromDto(dto) }));
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{dto.Tag ?? "unknown"}: {ex.Message}");
            }
        }

        BatchCommandResult batch = await facade.ExecuteBatchAsync(
            valid.Select(v => v.Command),
            BatchExecutionOptions.ContinueOnFailure(),
            ctx.CancellationToken);

        int succeeded = 0;
        for (int i = 0; i < batch.Results.Count; i++)
        {
            if (batch.Results[i].Success)
            {
                succeeded++;
            }
            else
            {
                failed++;
                errors.Add($"{valid[i].Tag}: {batch.Results[i].ErrorMessage}");
            }
        }

        return new ApiResult(200, new
        {
            succeeded,
            failed,
            total = dtos.Count,
            errors
        });
    }

    private static string? ValidateDto(WorkstationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Tag)) return "Tag is required";
        if (string.IsNullOrWhiteSpace(dto.Name)) return "Name is required";
        return null;
    }

    private static object ToDto(Workstation workstation)
    {
        return new
        {
            Tag = workstation.Tag.Value,
            workstation.Name,
            workstation.Description,
            workstation.PlaceableResRef,
            workstation.AppearanceId,
            SupportedIndustries = workstation.SupportedIndustries.Select(t => t.Value).ToArray()
        };
    }

    private static Workstation FromDto(WorkstationDto dto)
    {
        return new Workstation
        {
            Tag = new WorkstationTag(dto.Tag),
            Name = dto.Name,
            Description = dto.Description,
            PlaceableResRef = dto.PlaceableResRef,
            AppearanceId = dto.AppearanceId,
            SupportedIndustries = dto.SupportedIndustries?
                .Select(t => new IndustryTag(t)).ToList() ?? []
        };
    }

    // ==================== DTO classes for request/response ====================

    private record WorkstationDto
    {
        public string Tag { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string? PlaceableResRef { get; init; }
        public int? AppearanceId { get; init; }
        public List<string>? SupportedIndustries { get; init; }
    }
}
