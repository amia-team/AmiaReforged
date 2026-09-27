using System.Net;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine.Editors;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class TraitEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public TraitEditorTests()
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

        var traits = new TraitApiService(factory.Object, endpointService.Object);
        traits.SelectEndpoint(_endpointId);
        var glyphs = new GlyphApiService(factory.Object, endpointService.Object);
        glyphs.SelectEndpoint(_endpointId);

        Services.AddSingleton(traits);
        Services.AddSingleton(glyphs);
        Services.AddSingleton(new Mock<ILogger<TraitEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.ResponseContent = "{}";
    }

    private static TraitDefinitionDto SampleItem() => new()
    {
        Tag = "trait_brave",
        Name = "Brave",
        Description = "A brave soul",
        PointCost = 2,
        Category = "Personality",
        DeathBehavior = "Persist",
        RequiresUnlock = false,
        DmOnly = false,
        Effects =
        [
            new TraitEffectDto { EffectType = 1, Target = "Persuade", Magnitude = 2, Description = "+2 to Persuade" }
        ],
        AllowedRaces = ["Human", "Elf"],
        ForbiddenClasses = ["Monk"],
        PrerequisiteTraits = ["trait_basic_magic"],
        ConflictingTraits = ["trait_cowardly"]
    };

    [Test]
    public void CreateMode_ShowsEditableTag_AndEffects()
    {
        IRenderedComponent<TraitEditor> cut = RenderComponent<TraitEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem())
            .Add(p => p.IsCreating, true));

        AngleSharp.Dom.IElement tag = cut.Find("input[placeholder=\"e.g. trait_brave\"]");
        tag.HasAttribute("disabled").Should().BeFalse();
        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Effects");
        cut.Find(".we-editor__entity-editor").TextContent.Should().Contain("Glyph Scripts");
    }

    [Test]
    public void EditMode_LocksTag()
    {
        IRenderedComponent<TraitEditor> cut = RenderComponent<TraitEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem())
            .Add(p => p.IsCreating, false));

        cut.Find("input[placeholder=\"e.g. trait_brave\"]").HasAttribute("disabled").Should().BeTrue();
    }

    [Test]
    public void CreateMode_ValidSave_FiresOnSaved_AndPosts()
    {
        TraitDefinitionDto sample = SampleItem();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        TraitDefinitionDto? saved = null;

        IRenderedComponent<TraitEditor> cut = RenderComponent<TraitEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.IsCreating, true)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<TraitDefinitionDto>(this, d => saved = d)));

        cut.Find("form").Submit();

        saved.Should().NotBeNull();
        saved!.Tag.Should().Be("trait_brave");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
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
