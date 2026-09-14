using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class WorkstationEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public WorkstationEditorTests()
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

        var api = new WorkstationApiService(factory.Object, endpointService.Object);
        api.SelectEndpoint(_endpointId);
        Services.AddSingleton(api);
        Services.AddSingleton(new Mock<ILogger<WorkstationEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.ResponseContent = "{}";
    }

    private static WorkstationDefinitionDto SampleItem() => new()
    {
        Tag = "forge",
        Name = "Forge",
        Description = "A blacksmith forge",
        PlaceableResRef = "plc_forge",
        AppearanceId = 42,
        SupportedIndustries = ["blacksmithing"],
    };

    [Test]
    public void EditMode_LocksTag_AndShowsIndustries()
    {
        IRenderedComponent<WorkstationEditor> cut = RenderComponent<WorkstationEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem()));

        cut.Find("input[placeholder=\"workstation_tag\"]").HasAttribute("disabled").Should().BeTrue();
        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Supported Industries");
    }

    [Test]
    public void ValidSave_FiresOnSaved()
    {
        WorkstationDefinitionDto sample = SampleItem();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        WorkstationDefinitionDto? saved = null;

        IRenderedComponent<WorkstationEditor> cut = RenderComponent<WorkstationEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<WorkstationDefinitionDto>(this, d => saved = d)));

        cut.Find("form").Submit();

        saved.Should().NotBeNull();
        saved!.Tag.Should().Be("forge");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public string ResponseContent { get; set; } = "{}";
        public HttpRequestMessage? LastRequest { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseContent, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
