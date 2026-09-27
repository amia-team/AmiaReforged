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
public class ResourceNodeEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public ResourceNodeEditorTests()
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

        var nodes = new ResourceNodeApiService(factory.Object, endpointService.Object);
        nodes.SelectEndpoint(_endpointId);

        Services.AddSingleton(nodes);
        Services.AddSingleton(new Mock<ILogger<ResourceNodeEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.ResponseContent = "{}";
    }

    private static ResourceNodeDefinitionDto SampleNode() => new()
    {
        Tag = "oak_tree",
        Name = "Oak Tree",
        Description = "A majestic oak",
        Type = "Tree",
        Uses = 50,
        BaseHarvestRounds = 3,
        PlcAppearance = 1,
        MinQuality = "Average",
        Outputs =
        [
            new HarvestOutputDto { ItemDefinitionTag = "log_oak", Quantity = 2, Chance = 100 }
        ]
    };

    [Test]
    public void CreateMode_ShowsEditableTag_AndTypeFilter()
    {
        IRenderedComponent<ResourceNodeEditor> cut = RenderComponent<ResourceNodeEditor>(parameters => parameters
            .Add(p => p.Item, SampleNode())
            .Add(p => p.IsCreating, true));

        AngleSharp.Dom.IElement tag = cut.Find("input[placeholder=\"e.g. oak_tree\"]");
        tag.HasAttribute("disabled").Should().BeFalse();
        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Type");
        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Harvest Requirement");
    }

    [Test]
    public void EditMode_LocksTag()
    {
        IRenderedComponent<ResourceNodeEditor> cut = RenderComponent<ResourceNodeEditor>(parameters => parameters
            .Add(p => p.Item, SampleNode())
            .Add(p => p.IsCreating, false));

        cut.Find("input[placeholder=\"e.g. oak_tree\"]").HasAttribute("disabled").Should().BeTrue();
    }

    [Test]
    public void CreateMode_ValidSave_FiresOnSaved_AndPosts()
    {
        ResourceNodeDefinitionDto sample = SampleNode();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        ResourceNodeDefinitionDto? saved = null;

        IRenderedComponent<ResourceNodeEditor> cut = RenderComponent<ResourceNodeEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.IsCreating, true)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<ResourceNodeDefinitionDto>(this, d => saved = d)));

        cut.Find("form").Submit();

        saved.Should().NotBeNull();
        saved!.Tag.Should().Be("oak_tree");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
    }

    [Test]
    public async Task EditMode_Delete_Confirms_AndDeletes()
    {
        ResourceNodeDefinitionDto sample = SampleNode();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        string? deletedTag = null;

        IRenderedComponent<ResourceNodeEditor> cut = RenderComponent<ResourceNodeEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.IsCreating, false)
            .Add(p => p.OnDeleted, EventCallback.Factory.Create<string>(this, t => deletedTag = t)));

        cut.Find(".btn-outline-danger").Click();
        await cut.InvokeAsync(() => cut.Find(".btn-danger").Click());

        deletedTag.Should().Be("oak_tree");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
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
