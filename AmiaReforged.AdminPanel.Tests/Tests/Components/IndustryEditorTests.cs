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
public class IndustryEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public IndustryEditorTests()
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
        Services.AddSingleton(new Mock<ILogger<IndustryEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.ResponseContent = "{}";
    }

    private static IndustryDefinitionDto SampleItem() => new()
    {
        Tag = "blacksmithing",
        Name = "Blacksmithing",
        Knowledge = [new IndustryKnowledgeDto { Tag = "smelting", Name = "Smelting", PointCost = 2 }],
        Recipes = [new IndustryRecipeDto { RecipeId = "iron_ingot", Name = "Iron Ingot" }],
    };

    [Test]
    public void RendersAllSections_WithCounts()
    {
        IRenderedComponent<IndustryEditor> cut = RenderComponent<IndustryEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem()));

        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Knowledge (1)");
        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Recipes (1)");
    }

    [Test]
    public void AddKnowledge_ExpandsNewRow()
    {
        IRenderedComponent<IndustryEditor> cut = RenderComponent<IndustryEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem()));

        cut.FindAll("h5")[1].QuerySelector("button")!.Click();

        cut.FindAll(".we-entity-form__card").Should().HaveCount(3);
    }

    [Test]
    public void ValidSave_FiresOnSaved()
    {
        IndustryDefinitionDto sample = SampleItem();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        IndustryDefinitionDto? saved = null;

        IRenderedComponent<IndustryEditor> cut = RenderComponent<IndustryEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<IndustryDefinitionDto>(this, d => saved = d)));

        cut.Find("form").Submit();

        saved.Should().NotBeNull();
        saved!.Tag.Should().Be("blacksmithing");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
    }

    [Test]
    public void RemoveKnowledge_DropsRow()
    {
        IRenderedComponent<IndustryEditor> cut = RenderComponent<IndustryEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem()));

        cut.FindAll(".we-entity-form__card")[0]
            .QuerySelector(".btn-outline-danger")!.Click();

        cut.FindAll(".we-entity-form__card").Should().HaveCount(1);
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
