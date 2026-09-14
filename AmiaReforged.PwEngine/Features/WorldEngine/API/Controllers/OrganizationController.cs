using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using Anvil;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for managing organizations and their members.
/// Supports CRUD operations for the admin panel.
/// </summary>
public class OrganizationController
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// List all organizations with optional type filter, search, and pagination.
    /// GET /api/worldengine/organizations?search=&amp;type=&amp;page=1&amp;pageSize=50
    /// </summary>
    [HttpGet("/api/worldengine/organizations")]
    public static async Task<ApiResult> GetAll(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        string? search = ctx.GetQueryParam("search");
        string? typeFilter = ctx.GetQueryParam("type");
        int page = int.TryParse(ctx.GetQueryParam("page"), out int p) ? Math.Max(1, p) : 1;
        int pageSize = int.TryParse(ctx.GetQueryParam("pageSize"), out int ps) ? Math.Clamp(ps, 1, 200) : 50;

        List<IOrganization> orgs = await facade.QueryAsync<GetAllOrganizationsQuery, List<IOrganization>>(
            new GetAllOrganizationsQuery(), ctx.CancellationToken);

        if (!string.IsNullOrWhiteSpace(typeFilter) && Enum.TryParse<OrganizationType>(typeFilter, true, out OrganizationType orgType))
        {
            orgs = orgs.Where(o => o.Type == orgType).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            orgs = orgs.Where(o =>
                o.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (o.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        int totalCount = orgs.Count;
        List<IOrganization> paged = orgs
            .OrderBy(o => o.Name, StringComparer.OrdinalIgnoreCase)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return await Task.FromResult(new ApiResult(200, new
        {
            items = paged.Select(ToDto),
            totalCount,
            page,
            pageSize
        }));
    }

    /// <summary>
    /// Get a single organization by id.
    /// GET /api/worldengine/organizations/{id}
    /// </summary>
    [HttpGet("/api/worldengine/organizations/{id}")]
    public static async Task<ApiResult> GetById(RouteContext ctx)
    {
        string idStr = ctx.GetRouteValue("id");
        if (!Guid.TryParse(idStr, out Guid id))
        {
            return await Task.FromResult(new ApiResult(400, new ErrorResponse(
                "Bad request", "Invalid organization ID format")));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        IOrganization? org = await facade.QueryAsync<GetOrganizationDetailsQuery, IOrganization?>(
            new GetOrganizationDetailsQuery { OrganizationId = OrganizationId.From(id) }, ctx.CancellationToken);
        if (org == null)
        {
            return await Task.FromResult(new ApiResult(404, new ErrorResponse(
                "Not found", $"No organization with id '{id}'")));
        }

        return await Task.FromResult(new ApiResult(200, ToDto(org)));
    }

    /// <summary>
    /// Create a new organization.
    /// POST /api/worldengine/organizations
    /// </summary>
    [HttpPost("/api/worldengine/organizations")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        CreateOrganizationDto? dto = await ctx.ReadJsonBodyAsync<CreateOrganizationDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return new ApiResult(400, new ErrorResponse("Validation failed", "Name is required"));
        }

        if (!Enum.TryParse<OrganizationType>(dto.Type, true, out OrganizationType orgType))
        {
            return new ApiResult(400, new ErrorResponse("Validation failed",
                $"Invalid organization type '{dto.Type}'. Valid types: {string.Join(", ", Enum.GetNames<OrganizationType>())}"));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        OrganizationId? parentId = dto.ParentOrganizationId.HasValue
            ? OrganizationId.From(dto.ParentOrganizationId.Value)
            : null;

        CommandResult result = await facade.ExecuteAsync(new CreateOrganizationCommand
        {
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Type = orgType,
            ParentOrganizationId = parentId
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already exists", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        if (result.Data?.TryGetValue("OrganizationId", out object? idObj) != true
            || idObj is not OrganizationId newId)
        {
            return new ApiResult(500, new ErrorResponse("Internal server error",
                "Organization was created but its id was not returned"));
        }

        IOrganization? org = await facade.QueryAsync<GetOrganizationDetailsQuery, IOrganization?>(
            new GetOrganizationDetailsQuery { OrganizationId = newId }, ctx.CancellationToken);
        if (org == null)
        {
            return new ApiResult(500, new ErrorResponse("Internal server error",
                "Organization was created but could not be loaded"));
        }

        return new ApiResult(201, ToDto(org));
    }

    /// <summary>
    /// Update an existing organization.
    /// PUT /api/worldengine/organizations/{id}
    /// </summary>
    [HttpPut("/api/worldengine/organizations/{id}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        string idStr = ctx.GetRouteValue("id");
        if (!Guid.TryParse(idStr, out Guid id))
        {
            return await Task.FromResult(new ApiResult(400, new ErrorResponse(
                "Bad request", "Invalid organization ID format")));
        }

        UpdateOrganizationDto? dto = await ctx.ReadJsonBodyAsync<UpdateOrganizationDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new UpdateOrganizationCommand
        {
            OrganizationId = OrganizationId.From(id),
            Name = dto.Name,
            Description = dto.Description
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage));
        }

        IOrganization? org = await facade.QueryAsync<GetOrganizationDetailsQuery, IOrganization?>(
            new GetOrganizationDetailsQuery { OrganizationId = OrganizationId.From(id) }, ctx.CancellationToken);
        if (org == null)
        {
            return new ApiResult(500, new ErrorResponse("Internal server error",
                "Organization was updated but could not be loaded"));
        }

        return new ApiResult(200, ToDto(org));
    }

    /// <summary>
    /// Delete (disband) an organization.
    /// DELETE /api/worldengine/organizations/{id}
    /// </summary>
    [HttpDelete("/api/worldengine/organizations/{id}")]
    public static async Task<ApiResult> Delete(RouteContext ctx)
    {
        string idStr = ctx.GetRouteValue("id");
        if (!Guid.TryParse(idStr, out Guid id))
        {
            return await Task.FromResult(new ApiResult(400, new ErrorResponse(
                "Bad request", "Invalid organization ID format")));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new DisbandOrganizationCommand
        {
            OrganizationId = OrganizationId.From(id)
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
            return await Task.FromResult(new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage)));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Disbanded" }));
    }

    // ==================== Member Operations ====================

    /// <summary>
    /// Get all members of an organization.
    /// GET /api/worldengine/organizations/{id}/members?activeOnly=true
    /// </summary>
    [HttpGet("/api/worldengine/organizations/{id}/members")]
    public static async Task<ApiResult> GetMembers(RouteContext ctx)
    {
        string idStr = ctx.GetRouteValue("id");
        if (!Guid.TryParse(idStr, out Guid id))
        {
            return await Task.FromResult(new ApiResult(400, new ErrorResponse(
                "Bad request", "Invalid organization ID format")));
        }

        bool activeOnly = !bool.TryParse(ctx.GetQueryParam("activeOnly"), out bool ao) || ao;

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        List<OrganizationMember> members = await facade.QueryAsync<GetOrganizationMembersQuery, List<OrganizationMember>>(
            new GetOrganizationMembersQuery
            {
                OrganizationId = OrganizationId.From(id),
                ActiveOnly = activeOnly
            }, ctx.CancellationToken);

        return await Task.FromResult(new ApiResult(200, members.Select(ToMemberDto).ToArray()));
    }

    /// <summary>
    /// Add a member to an organization.
    /// POST /api/worldengine/organizations/{id}/members
    /// </summary>
    [HttpPost("/api/worldengine/organizations/{id}/members")]
    public static async Task<ApiResult> AddMember(RouteContext ctx)
    {
        string idStr = ctx.GetRouteValue("id");
        if (!Guid.TryParse(idStr, out Guid id))
        {
            return await Task.FromResult(new ApiResult(400, new ErrorResponse(
                "Bad request", "Invalid organization ID format")));
        }

        AddMemberDto? dto = await ctx.ReadJsonBodyAsync<AddMemberDto>();
        if (dto == null)
        {
            return new ApiResult(400, new ErrorResponse("Bad request", "Request body is required"));
        }

        OrganizationId orgId = OrganizationId.From(id);
        CharacterId characterId = new CharacterId(dto.CharacterId);

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        Enum.TryParse<OrganizationRank>(dto.Rank, true, out OrganizationRank rank);

        CommandResult result = await facade.ExecuteAsync(new AddMemberCommand
        {
            OrganizationId = orgId,
            CharacterId = characterId,
            InitialRank = rank == default ? OrganizationRank.Recruit : rank
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool conflict = result.ErrorMessage?.Contains("already an active member", StringComparison.OrdinalIgnoreCase) == true;
            return new ApiResult(conflict ? 409 : 400,
                new ErrorResponse(conflict ? "Conflict" : "Command failed", result.ErrorMessage));
        }

        if (result.Data?.TryGetValue("MembershipId", out object? idObj) != true
            || idObj is not Guid membershipId)
        {
            return new ApiResult(500, new ErrorResponse("Internal server error",
                "Member was added but its id was not returned"));
        }

        List<OrganizationMember> members = await facade.QueryAsync<GetOrganizationMembersQuery, List<OrganizationMember>>(
            new GetOrganizationMembersQuery { OrganizationId = orgId, ActiveOnly = false }, ctx.CancellationToken);
        OrganizationMember? member = members.FirstOrDefault(m => m.Id == membershipId);
        if (member == null)
        {
            return new ApiResult(500, new ErrorResponse("Internal server error",
                "Member was added but could not be loaded"));
        }

        return new ApiResult(201, ToMemberDto(member));
    }

    /// <summary>
    /// Remove a member from an organization.
    /// DELETE /api/worldengine/organizations/{id}/members/{characterId}
    /// </summary>
    [HttpDelete("/api/worldengine/organizations/{id}/members/{characterId}")]
    public static async Task<ApiResult> RemoveMember(RouteContext ctx)
    {
        string idStr = ctx.GetRouteValue("id");
        string charIdStr = ctx.GetRouteValue("characterId");

        if (!Guid.TryParse(idStr, out Guid id) || !Guid.TryParse(charIdStr, out Guid charId))
        {
            return await Task.FromResult(new ApiResult(400, new ErrorResponse(
                "Bad request", "Invalid ID format")));
        }

        OrganizationId orgId = OrganizationId.From(id);
        CharacterId characterId = new CharacterId(charId);

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        CommandResult result = await facade.ExecuteAsync(new RemoveMemberCommand
        {
            OrganizationId = orgId,
            CharacterId = characterId,
            RemovedBy = characterId
        }, ctx.CancellationToken);

        if (!result.Success)
        {
            bool notFound = result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
            return await Task.FromResult(new ApiResult(notFound ? 404 : 400, new ErrorResponse(
                notFound ? "Not found" : "Command failed", result.ErrorMessage)));
        }

        return await Task.FromResult(new ApiResult(204, new { message = "Removed" }));
    }

    // ==================== Helpers ====================

    private static object ToDto(IOrganization org)
    {
        return new
        {
            Id = org.Id.Value,
            org.Name,
            org.Description,
            Type = org.Type.ToString(),
            ParentOrganizationId = org.ParentOrganization?.Value
        };
    }

    private static object ToMemberDto(OrganizationMember member)
    {
        return new
        {
            member.Id,
            CharacterId = member.CharacterId.Value,
            OrganizationId = member.OrganizationId.Value,
            Rank = member.Rank.ToString(),
            Status = member.Status.ToString(),
            member.JoinedDate,
            member.DepartedDate,
            member.Notes,
            Roles = member.Roles.Select(r => r.Value).ToArray()
        };
    }

    // ==================== DTO classes ====================

    private record CreateOrganizationDto
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public string Type { get; init; } = string.Empty;
        public Guid? ParentOrganizationId { get; init; }
    }

    private record UpdateOrganizationDto
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
    }

    private record AddMemberDto
    {
        public Guid CharacterId { get; init; }
        public string? Rank { get; init; }
    }
}
