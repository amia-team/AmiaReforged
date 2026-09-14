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
public class CoinhouseEditorTests : Bunit.TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _endpointId = Guid.NewGuid();

    public CoinhouseEditorTests()
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

        var api = new CoinhouseApiService(factory.Object, endpointService.Object);
        api.SelectEndpoint(_endpointId);
        Services.AddSingleton(api);
        Services.AddSingleton(new Mock<ILogger<CoinhouseEditor>>().Object);
    }

    [SetUp]
    public void ResetHandler()
    {
        _handler.LastRequest = null;
        _handler.ResponseContent = "{}";
    }

    private static CoinhouseDto SampleItem() => new()
    {
        Id = 7,
        Tag = "cordor_bank",
        Settlement = 1,
        EngineId = Guid.NewGuid(),
        StoredGold = 5000,
        PersonaIdString = "Coinhouse:cordor_bank",
        AccountCount = 3,
        TotalDeposits = 9000,
        TotalCredits = 1000,
    };

    [Test]
    public void EditMode_LocksTag_AndShowsStats()
    {
        IRenderedComponent<CoinhouseEditor> cut = RenderComponent<CoinhouseEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem())
            .Add(p => p.IsCreating, false));

        cut.Find("input[placeholder=\"settlement_bank\"]").HasAttribute("disabled").Should().BeTrue();
        cut.Find(".we-entity-form__stats").TextContent.Should().Contain("Accounts:");
    }

    [Test]
    public void CreateMode_AllowsTagEdit_AndHidesStats()
    {
        IRenderedComponent<CoinhouseEditor> cut = RenderComponent<CoinhouseEditor>(parameters => parameters
            .Add(p => p.Item, new CoinhouseDto { Settlement = 1 })
            .Add(p => p.IsCreating, true));

        cut.Find("input[placeholder=\"settlement_bank\"]").HasAttribute("disabled").Should().BeFalse();
        cut.FindAll(".we-entity-form__stats").Should().BeEmpty();
    }

    [Test]
    public void InvalidEngineId_BlocksSave_WithError()
    {
        CoinhouseDto? saved = null;

        IRenderedComponent<CoinhouseEditor> cut = RenderComponent<CoinhouseEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem())
            .Add(p => p.OnSaved, EventCallback.Factory.Create<CoinhouseDto>(this, d => saved = d)));

        cut.Find("input[placeholder=\"auto-generated\"]").Change("not-a-guid");
        cut.Find("form").Submit();

        cut.Find(".alert-danger").TextContent.Should().Contain("valid GUID");
        _handler.LastRequest.Should().BeNull();
        saved.Should().BeNull();
    }

    [Test]
    public void ValidSave_FiresOnSaved()
    {
        CoinhouseDto sample = SampleItem();
        _handler.ResponseContent = JsonSerializer.Serialize(sample);
        CoinhouseDto? saved = null;

        IRenderedComponent<CoinhouseEditor> cut = RenderComponent<CoinhouseEditor>(parameters => parameters
            .Add(p => p.Item, sample)
            .Add(p => p.OnSaved, EventCallback.Factory.Create<CoinhouseDto>(this, d => saved = d)));

        cut.Find("form").Submit();

        saved.Should().NotBeNull();
        saved!.Tag.Should().Be("cordor_bank");
        _handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
    }

    [Test]
    public void DeleteFlow_ShowsAccountWarning_AndFiresOnDeleted()
    {
        _handler.ResponseContent = "{}";
        string? deleted = null;

        IRenderedComponent<CoinhouseEditor> cut = RenderComponent<CoinhouseEditor>(parameters => parameters
            .Add(p => p.Item, SampleItem())
            .Add(p => p.OnDeleted, EventCallback.Factory.Create<string>(this, t => deleted = t)));

        cut.Find(".btn-outline-danger").Click();
        cut.Find(".we-entity-form__delete-confirm").TextContent.Should().Contain("3 account(s)");
        cut.Find(".we-entity-form__delete-confirm .btn-danger").Click();

        deleted.Should().Be("cordor_bank");
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
