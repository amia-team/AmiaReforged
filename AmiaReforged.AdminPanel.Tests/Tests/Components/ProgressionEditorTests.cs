using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class ProgressionEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public ProgressionEditorTests()
    {
        var httpClient = new HttpClient(_handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        var endpointService = new Mock<IWorldEngineEndpointService>();
        endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint
            {
                Id = _endpointId,
                Name = "Test",
                BaseUrl = "http://localhost:8080",
                ApiKey = "test-key"
            });

        var api = new IndustryApiService(factory.Object, endpointService.Object);
        api.SelectEndpoint(_endpointId);
        Services.AddSingleton(api);
        Services.AddSingleton(new Mock<ILogger<ProgressionEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.ConfigJson = JsonSerializer.Serialize(new ProgressionConfigDto());
        _handler.ProfilesJson = "[]";
    }

    [Test]
    public void RendersCurveForm_Preview_AndProfiles()
    {
        IRenderedComponent<ProgressionEditor> cut = RenderComponent<ProgressionEditor>();

        cut.WaitForAssertion(() =>
        {
            cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Global Progression Curve");
            cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Cost Preview");
            cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Cap Profiles");
        });
    }

    [Test]
    public void SaveCurve_FiresUpdate()
    {
        IRenderedComponent<ProgressionEditor> cut = RenderComponent<ProgressionEditor>();
        cut.WaitForAssertion(() => cut.Find("form"));

        cut.FindAll("form")[0].Submit();

        cut.WaitForAssertion(() =>
            _handler.RequestUris.Should().Contain(u => u.Contains("progression")));
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public string ConfigJson { get; set; } = "{}";
        public string ProfilesJson { get; set; } = "[]";
        public HttpRequestMessage? LastRequest { get; set; }
        public List<string> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            string uri = request.RequestUri?.ToString() ?? "";
            RequestUris.Add(uri);
            string content = uri.Contains("cap-profiles")
                ? ProfilesJson
                : ConfigJson;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
