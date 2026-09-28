using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Services;

[TestFixture]
public class GlyphApiServiceTests
{
    private GlyphApiService _service = null!;
    private Handler _handler = null!;
    private HttpClient _client = null!;
    private readonly Guid _first = Guid.NewGuid(), _second = Guid.NewGuid();

    [SetUp] public void Setup()
    {
        _handler = new(); _client = new(_handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(_client);
        var endpoints = new Mock<IWorldEngineEndpointService>();
        endpoints.Setup(e => e.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new WorldEngineEndpoint
            { Id = id, Name = "test", BaseUrl = id == _first ? "http://first.test" : "http://second.test", ApiKey = "test-key" });
        _service = new(factory.Object, endpoints.Object);
        _service.SelectEndpoint(_first);
    }
    [TearDown] public void Cleanup() => _client.Dispose();

    [Test] public async Task Metadata_is_typed_cached_and_invalidated_on_endpoint_change()
    {
        var first = await _service.GetLanguageMetadataAsync();
        Assert.That(first!.Functions.Single().Parameters.Single().DefaultValue, Is.EqualTo("10"));
        Assert.That(first.Contexts.Single().Fields.Single().Setter, Is.EqualTo("set_progress"));
        _service.SelectEndpoint(_first);
        Assert.That(await _service.GetLanguageMetadataAsync(), Is.SameAs(first));
        Assert.That(_handler.Hosts.Count, Is.EqualTo(1));
        // Exercise virtual dispatch through the shared service base too.
        ((ApiServiceBase)_service).SelectEndpoint(_second);
        Assert.That(await _service.GetLanguageMetadataAsync(), Is.Not.SameAs(first));
        _service.SelectEndpoint(_first);
        await _service.GetLanguageMetadataAsync();
        Assert.That(_handler.Hosts, Is.EqualTo(new[] { "first.test", "second.test", "first.test" }));
    }

    [Test] public async Task Endpoint_change_discards_inflight_result_without_clearing_new_cache()
    {
        var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.Respond = request => request.RequestUri!.Host == "first.test" ? pending.Task : Task.FromResult(Response());
        Task<GlyphLanguageMetadataDto?> old = _service.GetLanguageMetadataAsync();
        Task<GlyphLanguageMetadataDto?> duplicate = _service.GetLanguageMetadataAsync();
        Assert.That(_handler.Hosts.Count, Is.EqualTo(1));
        _service.SelectEndpoint(_second);
        var current = await _service.GetLanguageMetadataAsync();
        pending.SetResult(Response());
        Assert.ThrowsAsync<OperationCanceledException>(async () => await old);
        Assert.ThrowsAsync<OperationCanceledException>(async () => await duplicate);
        Assert.That(await _service.GetLanguageMetadataAsync(), Is.SameAs(current));
        Assert.That(_handler.Hosts.Count, Is.EqualTo(2));
    }

    [Test] public async Task Failed_request_is_retryable_and_deselection_drops_cached_metadata()
    {
        _handler.Respond = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        { Content = new StringContent("{}") });
        Assert.ThrowsAsync<WorldEngineApiException>(async () => await _service.GetLanguageMetadataAsync());
        _handler.Respond = _ => Task.FromResult(Response());
        Assert.That(await _service.GetLanguageMetadataAsync(), Is.Not.Null);
        _service.SelectEndpoint(null);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.GetLanguageMetadataAsync());
        _service.SelectEndpoint(_first);
        Assert.That(await _service.GetLanguageMetadataAsync(), Is.Not.Null);
        Assert.That(_handler.Hosts.Count, Is.EqualTo(3));
    }

    private static HttpResponseMessage Response() => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(new GlyphLanguageMetadataDto(1,
            [new("heal", "heal", "Heal a creature", "Void", "Action",
                [new("amount", "Amount", "Int", false, "10")], null, null, null, null, [new("interaction", "tick")])],
            [new("interaction", "InteractionPipeline", "Interaction", ["attempted", "started", "tick", "completed"])],
            [new("interaction", "tick", [new("progress", "Int", "Progress", "progress", "set_progress")])],
            [new("metadata", "metadata", "set_metadata")])))
    };
    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Hosts { get; } = [];
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> Respond = _ => Task.FromResult(Response());
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/api/worldengine/glyphs/language-metadata"));
            Assert.That(request.Headers.GetValues("X-API-Key").Single(), Is.EqualTo("test-key"));
            Hosts.Add(request.RequestUri.Host);
            return Respond(request);
        }
    }
}
