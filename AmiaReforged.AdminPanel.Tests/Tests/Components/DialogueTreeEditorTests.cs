using System.Net;
using System.Text;
using System.Text.Json;
using AmiaReforged.AdminPanel.Components.Pages.WorldEngine;
using AmiaReforged.AdminPanel.Models;
using AmiaReforged.AdminPanel.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.AdminPanel.Tests.Tests.Components;

[TestFixture]
public class DialogueTreeEditorTests
{
    private Bunit.TestContext _context = null!;
    private FakeApi _handler = null!;
    private readonly Guid _endpointId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _context = new(); _handler = new();
        Mock<IHttpClientFactory> factory = new(); factory.Setup(f => f.CreateClient("WorldEngine")).Returns(new HttpClient(_handler));
        Mock<IWorldEngineEndpointService> endpoints = new();
        endpoints.Setup(e => e.GetEndpointAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new WorldEngineEndpoint { Id = _endpointId, Name = "Development", BaseUrl = "http://test", ApiKey = "test" });
        _context.Services.AddSingleton(endpoints.Object);
        _context.Services.AddSingleton(new DialogueApiService(factory.Object, endpoints.Object));
        _context.Services.AddSingleton(new QuestApiService(factory.Object, endpoints.Object));
        _context.Services.AddSingleton(new LoreApiService(factory.Object, endpoints.Object));
        _context.Services.AddSingleton(new ItemApiService(factory.Object, endpoints.Object));
        _context.Services.AddSingleton(new OrganizationApiService(factory.Object, endpoints.Object));
    }
    [TearDown] public void TearDown() => _context.Dispose();

    private async Task<IRenderedComponent<DialogueTreeEditor>> EditorAsync()
    {
        IRenderedComponent<DialogueTreeEditor> cut = _context.RenderComponent<DialogueTreeEditor>();
        await cut.InvokeAsync(() => { cut.Instance.SelectEndpoint(_endpointId); return cut.Instance.LoadListAsync(); });
        return cut;
    }
    private static AngleSharp.Dom.IElement Button(IRenderedFragment cut, string text) => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Test]
    public async Task NewDialogueStartsWithPlayableGreetingAndCanPreview()
    {
        var cut = await EditorAsync(); Button(cut, "New dialogue").Click();
        cut.Find("input").Change("greeting");
        Button(cut, "Preview").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Conversation preview").And.Contain("Hello.")));
        Assert.That(_handler.Writes, Is.EqualTo(0));
    }

    [Test]
    public async Task FlowMarksTheDefaultGreetingAndSelectedLinkedDestination()
    {
        var cut = await EditorAsync(); Button(cut, "New dialogue").Click();
        Assert.That(cut.FindAll(".badge").Any(b => b.TextContent == "Default"), Is.True);
        Button(cut, "Continue to new line").Click();
        Assert.That(cut.FindAll(".dialogue-flow-node > button.bg-primary").Single().TextContent, Does.Contain("NPC line"));
        Assert.That(cut.FindAll(".badge").Any(b => b.TextContent == "Default"), Is.True);
    }

    [Test]
    public async Task BrokenReplyIsRejectedBeforeAnyApiWrite()
    {
        var cut = await EditorAsync(); Button(cut, "New dialogue").Click(); cut.Find("input").Change("greeting");
        Button(cut, "Add reply").Click(); Button(cut, "Save & Apply").Click();
        Assert.That(cut.Markup, Does.Contain("non-existent node"));
        Assert.That(_handler.Writes, Is.EqualTo(0));
    }

    [Test]
    public async Task SaveDisplaysApplicationAcknowledgementAndNpcCount()
    {
        var cut = await EditorAsync(); Button(cut, "New dialogue").Click(); cut.Find("input").Change("greeting");
        Button(cut, "Save & Apply").Click();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Saved revision applied").And.Contain("2 matching NPC(s)")));
        Assert.That(_handler.Writes, Is.EqualTo(1));
    }

    [Test]
    public async Task EndpointSwitchDiscardsThePreviousServersDraft()
    {
        var cut = await EditorAsync(); Button(cut, "New dialogue").Click();
        await cut.InvokeAsync(() => cut.Instance.SelectEndpoint(Guid.NewGuid()));
        cut.Render();
        Assert.That(cut.FindAll("button").Any(b => b.TextContent == "Save & Apply"), Is.False);
        Assert.That(_handler.Writes, Is.EqualTo(0));
    }

    [Test]
    public void RemovingANodePreservesSharedDestinationsAndRemovesOnlyItsLinks()
    {
        DialogueNodeDto root = DialogueAuthoring.CreateNode("Root"), branch = DialogueAuthoring.CreateNode("NpcText"), shared = DialogueAuthoring.CreateNode("NpcText");
        DialogueAuthoring.AddReply(root).TargetNodeId = branch.Id;
        DialogueAuthoring.AddReply(root).TargetNodeId = shared.Id;
        DialogueAuthoring.AddReply(branch).TargetNodeId = shared.Id;
        shared.ParentNodeId = branch.Id;
        DialogueTreeDto tree = new() { RootNodeId = root.Id, Nodes = [root, branch, shared] };
        DialogueAuthoring.RemoveNode(tree, branch);
        Assert.That(tree.Nodes, Does.Contain(shared));
        Assert.That(root.Choices.Single().TargetNodeId, Is.EqualTo(shared.Id));
        Assert.That(shared.ParentNodeId, Is.Null);
    }

    [Test]
    public void OutlineUsesChoiceLinksAndTerminatesCycles()
    {
        DialogueNodeDto root = DialogueAuthoring.CreateNode("Root"), destination = DialogueAuthoring.CreateNode("NpcText");
        root.Text = "Greeting"; destination.Text = "Linked text";
        DialogueAuthoring.AddReply(root).TargetNodeId = destination.Id;
        DialogueAuthoring.AddReply(destination).TargetNodeId = root.Id;
        var cut = _context.RenderComponent<DialogueFlowOutline>(p => p.Add(c => c.Node, root).Add(c => c.Nodes, [root, destination]));
        Assert.That(cut.Markup, Does.Contain("Linked text").And.Contain("Linked destination"));
    }

    private sealed class FakeApi : HttpMessageHandler
    {
        public int Writes { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            object body;
            if (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put)
            {
                Writes++;
                body = JsonSerializer.Deserialize<DialogueTreeDto>(await request.Content!.ReadAsStringAsync(cancellationToken))!;
            }
            else if (request.RequestUri!.AbsolutePath.EndsWith("/runtime")) body = new DialogueRuntimeStatusDto { State = "Applied", MatchedNpcCount = 2 };
            else body = new PagedResult<DialogueTreeDto>();
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        }
    }
}
