using System.Net;
using System.Text;
using System.Text.Json;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Services;

[TestFixture]
public class ItemApiServiceTests
{
    private TestHttpMessageHandler _handler = null!;
    private ItemApiService _service = null!;
    private HttpRequestMessage? _capturedRequest = null!;
    private string? _capturedBody = null;
    private HttpResponseMessage _nextResponse = null!;

    private static readonly WorldEngineEndpoint TestEndpoint = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        BaseUrl = "http://localhost:8080",
        ApiKey = "test-key"
    };

    [SetUp]
    public void SetUp()
    {
        _handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(_handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        var endpointService = new Mock<IWorldEngineEndpointService>();
        endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestEndpoint);

        _service = new ItemApiService(factory.Object, endpointService.Object);
        _service.SelectEndpoint(TestEndpoint.Id);

        _nextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        _handler.Handler = async (req, _) =>
        {
            _capturedRequest = req;
            if (req.Content != null)
                _capturedBody = await req.Content.ReadAsStringAsync();
            return _nextResponse;
        };
    }

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
        _nextResponse.Dispose();
    }

    // ==================== GetAllAsync ====================

    [Test]
    public async Task GetAllAsync_SendsGetToCorrectUrl()
    {
        _nextResponse.Content = new StringContent(
            JsonSerializer.Serialize(new PagedResult<ItemBlueprintDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 50
            }));

        await _service.GetAllAsync();

        _capturedRequest!.Method.Should().Be(HttpMethod.Get);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Be(
            "/api/worldengine/items?page=1&pageSize=50");
    }

    [Test]
    public async Task GetAllAsync_AppendsSearchParam()
    {
        _nextResponse.Content = new StringContent(
            JsonSerializer.Serialize(new PagedResult<ItemBlueprintDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 50
            }));

        await _service.GetAllAsync(search: "sword");

        _capturedRequest!.RequestUri!.PathAndQuery.Should().Contain("search=sword");
    }

    [Test]
    public async Task GetAllAsync_SetsApiKeyHeader()
    {
        _nextResponse.Content = new StringContent(
            JsonSerializer.Serialize(new PagedResult<ItemBlueprintDto>
            {
                Items = [], TotalCount = 0, Page = 1, PageSize = 50
            }));

        await _service.GetAllAsync();

        _capturedRequest!.Headers.GetValues("X-API-Key").Should().ContainSingle("test-key");
    }

    // ==================== GetByTagAsync ====================

    [Test]
    public async Task GetByTagAsync_SendsGetToTagUrl()
    {
        await _service.GetByTagAsync("my_sword");

        _capturedRequest!.Method.Should().Be(HttpMethod.Get);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Be(
            "/api/worldengine/items/my_sword");
    }

    // ==================== CreateAsync ====================

    [Test]
    public async Task CreateAsync_SendsPostWithSerializedBody()
    {
        var dto = new ItemBlueprintDto { ItemTag = "new_item", Name = "New Item" };

        await _service.CreateAsync(dto);

        _capturedRequest!.Method.Should().Be(HttpMethod.Post);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Be("/api/worldengine/items");
        _capturedBody.Should().Contain("new_item");
    }

    // ==================== UpdateAsync ====================

    [Test]
    public async Task UpdateAsync_SendsPutToTagUrl()
    {
        var dto = new ItemBlueprintDto { ItemTag = "existing", Name = "Updated" };

        await _service.UpdateAsync("existing", dto);

        _capturedRequest!.Method.Should().Be(HttpMethod.Put);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Be(
            "/api/worldengine/items/existing");
    }

    // ==================== DeleteAsync ====================

    [Test]
    public async Task DeleteAsync_SendsDeleteToTagUrl()
    {
        await _service.DeleteAsync("to_delete");

        _capturedRequest!.Method.Should().Be(HttpMethod.Delete);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Be(
            "/api/worldengine/items/to_delete");
    }

    // ==================== ImportJsonAsync ====================

    [Test]
    public async Task ImportJsonAsync_SendsPostWithRawJsonBody()
    {
        string json = "{\"tag\":\"imported\"}";

        await _service.ImportJsonAsync(json);

        _capturedRequest!.Method.Should().Be(HttpMethod.Post);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Be(
            "/api/worldengine/items/import");
        _capturedBody.Should().Be(json);
    }

    // ==================== ExportJsonAsync ====================

    [Test]
    public async Task ExportJsonAsync_ReturnsPrettyPrintedJson()
    {
        string rawJson = JsonSerializer.Serialize(new { tag = "test", value = 42 });
        _nextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(rawJson, Encoding.UTF8, "application/json")
        };

        string result = await _service.ExportJsonAsync();

        result.Should().Contain("tag");
        result.Should().Contain("test");
    }

    [Test]
    public async Task ExportJsonAsync_AppendsSearchParam()
    {
        _nextResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        await _service.ExportJsonAsync(search: "filter");

        _capturedRequest!.RequestUri!.PathAndQuery.Should().Contain("search=filter");
    }

    // ==================== Helper types ====================

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Handler { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Handler?.Invoke(request, cancellationToken)
                ?? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
