using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Services;

[TestFixture]
public class ApiServiceBaseTests
{
    private TestApiService _service = null!;
    private TestHttpMessageHandler _handler = null!;
    private Mock<IWorldEngineEndpointService> _endpointService = null!;

    private static readonly WorldEngineEndpoint TestEndpoint = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test",
        BaseUrl = "http://localhost:8080",
        ApiKey = " test-key "
    };

    [SetUp]
    public void SetUp()
    {
        _handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(_handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        _endpointService = new Mock<IWorldEngineEndpointService>();
        _endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestEndpoint);

        _service = new TestApiService(factory.Object, _endpointService.Object);
        _service.SelectEndpoint(TestEndpoint.Id);
    }

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
    }

    // ==================== ResolveEndpointAsync ====================

    [Test]
    public async Task ResolveEndpoint_ThrowsWhenNoEndpointSelected()
    {
        _service.SelectEndpoint(null);

        var act = () => _service.ExposedResolveEndpointAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No WorldEngine endpoint selected*");
    }

    [Test]
    public async Task ResolveEndpoint_ThrowsWhenEndpointNotFound()
    {
        _endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorldEngineEndpoint?)null);

        var act = () => _service.ExposedResolveEndpointAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no longer exists*");
    }

    [Test]
    public async Task ResolveEndpoint_ThrowsWhenNoApiKey()
    {
        _endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint { Name = "NoKey", BaseUrl = "http://x/", ApiKey = null });

        var act = () => _service.ExposedResolveEndpointAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has no API key configured*");
    }

    [Test]
    public async Task ResolveEndpoint_ReturnsCorrectBaseUriAndTrimmedKey()
    {
        (Uri baseUri, string apiKey) = await _service.ExposedResolveEndpointAsync();

        baseUri.Should().Be(new Uri("http://localhost:8080/"));
        apiKey.Should().Be("test-key");
    }

    // ==================== CreateRequest ====================

    [Test]
    public void CreateRequest_SetsApiKeyHeaderAndCorrectUri()
    {
        var baseUri = new Uri("http://localhost:8080/");
        using HttpRequestMessage request = TestApiService.ExposedCreateRequest(
            HttpMethod.Get, baseUri, "/api/test", "mykey");

        request.RequestUri.Should().Be(new Uri("http://localhost:8080/api/test"));
        request.Headers.GetValues("X-API-Key").Should().ContainSingle("mykey");
    }

    // ==================== EnsureSuccessOrThrow ====================

    [Test]
    public async Task EnsureSuccessOrThrow_ThrowsWorldEngineApiExceptionOn4xx()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new ApiErrorResponse("Bad Request", "detail here")))
        };

        var act = () => _service.ExposedEnsureSuccessOrThrow(response);

        var ex = await act.Should().ThrowAsync<WorldEngineApiException>();
        ex.Which.StatusCode.Should().Be(400);
        ex.Which.ErrorTitle.Should().Be("Bad Request");
        ex.Which.Detail.Should().Be("detail here");
    }

    [Test]
    public async Task EnsureSuccessOrThrow_ThrowsWithRawBodyWhenJsonFails()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("plain text error")
        };

        var act = () => _service.ExposedEnsureSuccessOrThrow(response);

        var ex = await act.Should().ThrowAsync<WorldEngineApiException>();
        ex.Which.StatusCode.Should().Be(500);
        ex.Which.Detail.Should().Be("plain text error");
    }

    [Test]
    public async Task EnsureSuccessOrThrow_DoesNotThrowOnSuccess()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        var act = () => _service.ExposedEnsureSuccessOrThrow(response);

        await act.Should().NotThrowAsync();
    }

    // ==================== Helper types ====================

    private class TestApiService : ApiServiceBase
    {
        public TestApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
            : base(httpClientFactory, endpointService) { }

        public Task<(Uri BaseUri, string ApiKey)> ExposedResolveEndpointAsync() => ResolveEndpointAsync();

        public static HttpRequestMessage ExposedCreateRequest(
            HttpMethod method, Uri baseUri, string relativeUrl, string apiKey)
            => CreateRequest(method, baseUri, relativeUrl, apiKey);

        public Task ExposedEnsureSuccessOrThrow(HttpResponseMessage response)
            => EnsureSuccessOrThrow(response);
    }

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
