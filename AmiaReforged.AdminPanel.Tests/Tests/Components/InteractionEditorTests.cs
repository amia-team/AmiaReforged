using System.Net;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class InteractionEditorTests : Bunit.TestContext
{
    public InteractionEditorTests()
    {
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("WorldEngine")).Returns(httpClient);

        var endpointService = new Mock<IWorldEngineEndpointService>();
        endpointService
            .Setup(s => s.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorldEngineEndpoint
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                BaseUrl = "http://localhost:8080",
                ApiKey = "test-key"
            });

        Services.AddSingleton<InteractionApiService>(new InteractionApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<GlyphApiService>(new GlyphApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<IndustryApiService>(new IndustryApiService(factory.Object, endpointService.Object));
        Services.AddSingleton<ILogger<InteractionEditor>>(new Mock<ILogger<InteractionEditor>>().Object);
        Services.AddSingleton<IJSRuntime>(new Mock<IJSRuntime>().Object);
    }

    // ==================== Rendering Basics ====================

    [Test]
    public void RendersNothing_ByDefault()
    {
        IRenderedComponent<InteractionEditor> cut = RenderComponent<InteractionEditor>(parameters => parameters
            .Add(p => p.OnClosed, EventCallback.Factory.Create<InteractionEditorResult>(this, _ => { })));

        cut.FindAll(".interaction-editor").Should().BeEmpty();
    }

    [Test]
    public void OnClosed_CallbackIsWired()
    {
        InteractionEditorResult? capturedResult = null;

        IRenderedComponent<InteractionEditor> cut = RenderComponent<InteractionEditor>(parameters => parameters
            .Add(p => p.OnClosed, EventCallback.Factory.Create<InteractionEditorResult>(this, r => capturedResult = r)));

        capturedResult.Should().BeNull();
    }

    [Test]
    public void AvailableIndustries_ParameterAcceptedList()
    {
        var industries = new List<IndustryDefinitionDto>
        {
            new() { Tag = "smithing", Name = "Smithing" },
            new() { Tag = "alchemy", Name = "Alchemy" },
        };

        IRenderedComponent<InteractionEditor> cut = RenderComponent<InteractionEditor>(parameters => parameters
            .Add(p => p.AvailableIndustries, industries)
            .Add(p => p.OnClosed, EventCallback.Factory.Create<InteractionEditorResult>(this, _ => { })));

        cut.FindAll(".interaction-editor").Should().BeEmpty();
    }

    // ==================== Callback Contracts ====================

    [Test]
    public void InteractionEditorResult_HasExpectedProperties()
    {
        var result = new InteractionEditorResult
        {
            Saved = true,
            InteractionTag = "ie_test_001",
            IsNew = true,
            Message = "Created."
        };

        result.Saved.Should().BeTrue();
        result.InteractionTag.Should().Be("ie_test_001");
        result.IsNew.Should().BeTrue();
        result.Message.Should().Be("Created.");
    }

    [Test]
    public void InteractionEditorResult_DefaultValues()
    {
        var result = new InteractionEditorResult();

        result.Saved.Should().BeFalse();
        result.InteractionTag.Should().BeNull();
        result.IsNew.Should().BeFalse();
        result.Message.Should().BeNull();
    }

    // ==================== Test Helpers ====================

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public string ResponseContent { get; set; } = "{}";
        public HttpRequestMessage? LastRequest { get; private set; }

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
