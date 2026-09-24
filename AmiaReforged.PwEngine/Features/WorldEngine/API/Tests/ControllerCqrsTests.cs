using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using Moq;
using NLog;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// Verifies converted controllers route through <see cref="IWorldEngineFacade"/>
/// (central dispatch) instead of repositories, and map results to HTTP statuses.
/// Uses a stub <see cref="IServiceProvider"/> carrying a mocked facade —
/// no Anvil runtime required.
/// </summary>
[TestFixture]
public class ControllerCqrsTests
{
    private RouteTable _routeTable = null!;
    private Mock<IWorldEngineFacade> _facadeMock = null!;
    private IServiceProvider _services = null!;

    private sealed class StubProvider : IServiceProvider
    {
        private readonly IWorldEngineFacade _facade;
        public StubProvider(IWorldEngineFacade facade) => _facade = facade;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IWorldEngineFacade) ? _facade : null;
    }

    [SetUp]
    public void SetUp()
    {
        _routeTable = new RouteTable(LogManager.GetCurrentClassLogger());
        _facadeMock = new Mock<IWorldEngineFacade>();
        _services = new StubProvider(_facadeMock.Object);
    }

    private Task<ApiResult?> DispatchAsync(string method, string path) =>
        _routeTable.DispatchAsync(method, path, null!, CancellationToken.None, _services);

    private static Recipe MakeRecipe(string recipeId) => new()
    {
        RecipeId = new RecipeId(recipeId),
        Name = recipeId,
        Description = string.Empty,
        IndustryTag = new IndustryTag("smithing"),
        Ingredients = [],
        Products = []
    };

    private static ItemBlueprint Blueprint(string tag) => new(
        tag, tag, $"Name {tag}", $"Desc {tag}",
        [], Subsystems.Harvesting.ItemForm.None, 0,
        new AppearanceData(0, null, null), null);

    [Test]
    public async Task ItemController_GetAll_WhenCalled_DispatchesQueryAndReturnsPagedResult()
    {
        _routeTable.ScanType(typeof(Controllers.ItemController));
        _facadeMock
            .Setup(f => f.QueryAsync<GetAllItemDefinitionsQuery, List<ItemBlueprint>>(
                It.IsAny<GetAllItemDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ItemBlueprint> { Blueprint("a"), Blueprint("b"), Blueprint("c") });

        ApiResult? result = await DispatchAsync("GET", "/api/worldengine/items");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        _facadeMock.Verify(f => f.QueryAsync<GetAllItemDefinitionsQuery, List<ItemBlueprint>>(
            It.IsAny<GetAllItemDefinitionsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ItemController_GetByTag_WhenMissing_Returns404()
    {
        _routeTable.ScanType(typeof(Controllers.ItemController));
        _facadeMock
            .Setup(f => f.QueryAsync<GetItemDefinitionByTagQuery, ItemBlueprint?>(
                It.IsAny<GetItemDefinitionByTagQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ItemBlueprint?)null);

        ApiResult? result = await DispatchAsync("GET", "/api/worldengine/items/nope");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ItemController_GetExpanded_WhenCalled_DispatchesTemplateTagThroughQuery()
    {
        const string tag = "wpn_sword_template";
        _routeTable.ScanType(typeof(Controllers.ItemController));
        _facadeMock
            .Setup(f => f.QueryAsync<GetExpandedItemDefinitionsQuery, List<ItemBlueprint>>(
                It.IsAny<GetExpandedItemDefinitionsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ItemBlueprint> { Blueprint($"{tag}_wood") });

        ApiResult? result = await DispatchAsync("GET", $"/api/worldengine/items/{tag}/expanded");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        _facadeMock.Verify(f => f.QueryAsync<GetExpandedItemDefinitionsQuery, List<ItemBlueprint>>(
            It.Is<GetExpandedItemDefinitionsQuery>(q => q.TemplateTag == tag),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RecipeTemplateController_GetExpanded_WhenCalled_DispatchesTemplateTagThroughQuery()
    {
        const string tag = "wpn_sword_template";
        _routeTable.ScanType(typeof(Controllers.RecipeTemplateController));
        _facadeMock
            .Setup(f => f.QueryAsync<GetExpandedRecipesQuery, List<Recipe>>(
                It.IsAny<GetExpandedRecipesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Recipe> { MakeRecipe($"{tag}_wood") });

        ApiResult? result = await DispatchAsync("GET", $"/api/worldengine/recipe-templates/{tag}/expanded");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        _facadeMock.Verify(f => f.QueryAsync<GetExpandedRecipesQuery, List<Recipe>>(
            It.Is<GetExpandedRecipesQuery>(q => q.TemplateTag == tag),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RecipeTemplateController_GetExpanded_WhenMissing_Returns200WithEmptyRecipes()
    {
        const string tag = "nonexistent_template";
        _routeTable.ScanType(typeof(Controllers.RecipeTemplateController));
        _facadeMock
            .Setup(f => f.QueryAsync<GetExpandedRecipesQuery, List<Recipe>>(
                It.IsAny<GetExpandedRecipesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Recipe>());

        ApiResult? result = await DispatchAsync("GET", $"/api/worldengine/recipe-templates/{tag}/expanded");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task OrganizationController_GetMembers_WhenCalled_DispatchesQuery()
    {
        _routeTable.ScanType(typeof(Controllers.OrganizationController));
        Guid orgId = Guid.NewGuid();
        Guid charId = Guid.NewGuid();
        _facadeMock
            .Setup(f => f.QueryAsync<GetOrganizationMembersQuery, List<OrganizationMember>>(
                It.IsAny<GetOrganizationMembersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrganizationMember>
            {
                new() { Id = Guid.NewGuid(), CharacterId = new CharacterId(charId), OrganizationId = OrganizationId.From(orgId), Rank = OrganizationRank.Member, Status = MembershipStatus.Active, JoinedDate = DateTime.UtcNow }
            });

        ApiResult? result = await DispatchAsync("GET", $"/api/worldengine/organizations/{orgId}/members");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        _facadeMock.Verify(f => f.QueryAsync<GetOrganizationMembersQuery, List<OrganizationMember>>(
            It.Is<GetOrganizationMembersQuery>(q => q.OrganizationId == OrganizationId.From(orgId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OrganizationController_RemoveMember_WhenHandlerReportsNotFound_Returns404()
    {
        _routeTable.ScanType(typeof(Controllers.OrganizationController));
        Guid orgId = Guid.NewGuid();
        Guid charId = Guid.NewGuid();
        _facadeMock
            .Setup(f => f.ExecuteAsync(It.IsAny<RemoveMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Fail("Member not found in organization"));

        ApiResult? result = await DispatchAsync(
            "DELETE", $"/api/worldengine/organizations/{orgId}/members/{charId}");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task OrganizationController_RemoveMember_WhenHandlerSucceeds_Returns204()
    {
        _routeTable.ScanType(typeof(Controllers.OrganizationController));
        Guid orgId = Guid.NewGuid();
        Guid charId = Guid.NewGuid();
        _facadeMock
            .Setup(f => f.ExecuteAsync(It.IsAny<RemoveMemberCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        ApiResult? result = await DispatchAsync(
            "DELETE", $"/api/worldengine/organizations/{orgId}/members/{charId}");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(204));
        _facadeMock.Verify(f => f.ExecuteAsync(
            It.Is<RemoveMemberCommand>(c => c.RemovedBy == new CharacterId(charId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WorkstationController_Delete_WhenHandlerReportsMissing_Returns404()
    {
        _routeTable.ScanType(typeof(Controllers.WorkstationController));
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<Application.Industries.Commands.DeleteWorkstationCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Fail("No workstation with tag 'x'"));

        ApiResult? result = await DispatchAsync("DELETE", "/api/worldengine/workstations/x");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task AreaGraphController_GetGraph_WhenCalled_DispatchesQueryAndReturns200()
    {
        _routeTable.ScanType(typeof(Controllers.AreaGraphController));
        AreaGraphData graph = new AreaGraphData
        {
            Nodes = [new AreaNode("area_1", "Area One")],
            Edges = [new AreaEdge("area_1", "area_2", TransitionType.Door, "door")]
        };
        _facadeMock
            .Setup(f => f.QueryAsync<GetAreaGraphQuery, AreaGraphData>(
                It.IsAny<GetAreaGraphQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(graph);

        ApiResult? result = await DispatchAsync("GET", "/api/worldengine/areas/graph");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        _facadeMock.Verify(f => f.QueryAsync<GetAreaGraphQuery, AreaGraphData>(
            It.IsAny<GetAreaGraphQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
