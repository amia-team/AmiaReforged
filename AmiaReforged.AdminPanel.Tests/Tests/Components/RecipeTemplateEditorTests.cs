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
public class RecipeTemplateEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public RecipeTemplateEditorTests()
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

        var api = new RecipeTemplateApiService(factory.Object, endpointService.Object);
        api.SelectEndpoint(_endpointId);
        Services.AddSingleton(api);

        var industryApi = new IndustryApiService(factory.Object, endpointService.Object);
        industryApi.SelectEndpoint(_endpointId);
        Services.AddSingleton(industryApi);

        var workstationApi = new WorkstationApiService(factory.Object, endpointService.Object);
        workstationApi.SelectEndpoint(_endpointId);
        Services.AddSingleton(workstationApi);

        Services.AddSingleton(new Mock<ILogger<RecipeTemplateEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.LastContent = null;
        _handler.ResponseContent = "{}";
    }

    private static RecipeTemplateDefinitionDto SampleItem() => new()
    {
        Tag = "plank_from_log",
        Name = "Planks from Logs",
        IndustryTag = "woodworking",
        RequiredKnowledge = ["basic_woodworking"],
        Ingredients = [new TemplateIngredientDto { RequiredCategory = "Wood", RequiredForm = "Log", Quantity = 1 }],
        Products = [new TemplateProductDto { OutputForm = "Plank", Quantity = 4 }],
        RequiredTools = [new ToolRequirementDto { RequiredForm = "Axe" }],
    };

    [Test]
    public void RendersAllSections()
    {
        IRenderedComponent<RecipeTemplateEditor> cut = RenderComponent<RecipeTemplateEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem()));

        string text = cut.Find(".we-editor__entity-editor").TextContent;
        text.Should().Contain("Ingredients");
        text.Should().Contain("Products");
        text.Should().Contain("Required Tools");
    }

    [Test]
    public void ValidSave_FiresOnSaved()
    {
        RecipeTemplateDefinitionDto sample = SampleItem();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        RecipeTemplateDefinitionDto? saved = null;

        IRenderedComponent<RecipeTemplateEditor> cut = RenderComponent<RecipeTemplateEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<RecipeTemplateDefinitionDto>(this, d => saved = d)));

        cut.Find("form").Submit();

        saved.Should().NotBeNull();
        saved!.Tag.Should().Be("plank_from_log");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
    }

    [Test]
    public void Save_CleansEmptyToolRows()
    {
        RecipeTemplateDefinitionDto sample = SampleItem();
        sample.RequiredTools.Add(new ToolRequirementDto());
        _handler.ResponseContent = JsonSerializer.Serialize(sample);

        IRenderedComponent<RecipeTemplateEditor> cut = RenderComponent<RecipeTemplateEditor>(parameters => parameters
            .Add(p => p.Item, sample));

        cut.Find("form").Submit();

        _handler.LastContent.Should().NotBeNull();
        _handler.LastContent.Should().NotContain("\"RequiredForm\":null");
    }

    [Test]
    public void SaveAndInvalidate_CallsInvalidateEndpoint()
    {
        _handler.ResponseContent = JsonSerializer.Serialize(SampleItem());

        IRenderedComponent<RecipeTemplateEditor> cut = RenderComponent<RecipeTemplateEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem()));

        cut.Find(".btn-outline-warning").Click();

        _handler.RequestUris.Should().Contain(u => u.Contains("invalidate"));
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public string ResponseContent { get; set; } = "{}";
        public HttpRequestMessage? LastRequest { get; set; }
        public string? LastContent { get; set; }
        public List<string> RequestUris { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            RequestUris.Add(request.RequestUri?.ToString() ?? "");
            if (request.Content != null)
                LastContent = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseContent, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }
}
