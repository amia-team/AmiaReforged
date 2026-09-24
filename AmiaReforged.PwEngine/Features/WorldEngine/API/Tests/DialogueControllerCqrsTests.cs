using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Queries;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Tests;

/// <summary>
/// Verifies the dialogue controller is a pure CQRS/HTTP boundary. It dispatches the
/// dialogue-tree commands through <see cref="IWorldEngineFacade"/> and maps success/failure to
/// the expected HTTP status. NPC synchronization is owned by the event bus
/// (<see cref="DialogueNpcSynchronizationHandler"/>); the controller must never resolve or invoke
/// <see cref="DialogueNpcHook"/> directly. These tests construct no NPC hook and exercise only the
/// controller's CQRS responsibility.
///
/// Note: request bodies require a full <see cref="HttpListenerRequest"/> stack, which is not
/// faked in this harness (see <see cref="EchoControllerTests"/>). The body-free paths below are
/// sufficient to prove dispatch, status mapping, and the absence of any NPC-side-effect wiring.
/// </summary>
[TestFixture]
public class DialogueControllerCqrsTests
{
    private Mock<IWorldEngineFacade> _facadeMock = null!;
    private IServiceProvider _services = null!;

    private sealed class StubProvider : IServiceProvider
    {
        private readonly IWorldEngineFacade _facade;
        public StubProvider(IWorldEngineFacade facade) => _facade = facade;
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IWorldEngineFacade) ? _facade : null;
    }

    [SetUp]
    public void SetUp()
    {
        _facadeMock = new Mock<IWorldEngineFacade>();
        _services = new StubProvider(_facadeMock.Object);
    }

    private RouteContext Context(string method, string path, Dictionary<string, string>? routeValues = null) =>
        new RouteContext(null!, routeValues ?? new Dictionary<string, string>(), CancellationToken.None)
        {
            Services = _services
        };

    // ────────────────────────────────────────────────────────────────────
    //  Create — dispatch + status mapping (no NPC hook)
    // ────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WhenNoBody_Returns400AndDoesNotDispatch()
    {
        // A missing body is rejected before any command dispatch (the facade guard is only reached
        // once a body has been read); without a faked request body this path is the observable one.
        ApiResult? result = await Controllers.DialogueController.Create(Context("POST", "/api/worldengine/dialogue"));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(400));
        _facadeMock.Verify(f => f.ExecuteAsync(It.IsAny<CreateDialogueTreeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Create_WhenNoBody_DoesNotDispatchCommand()
    {
        _ = Controllers.DialogueController.Create(Context("POST", "/api/worldengine/dialogue"));

        _facadeMock.Verify(f => f.ExecuteAsync(It.IsAny<CreateDialogueTreeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Create_WhenSucceeds_DoesNotResolveDialogueNpcHook()
    {
        // The controller must never reference the NPC hook; these tests construct no hook.
        Assert.That(typeof(Controllers.DialogueController), Is.Not.SameAs(typeof(AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.DialogueNpcHook)));

        _ = Controllers.DialogueController.Create(Context("POST", "/api/worldengine/dialogue"));
    }

    // ────────────────────────────────────────────────────────────────────
    //  Update — dispatch + status mapping (no NPC hook)
    // ────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_WhenNoBody_Returns400AndDoesNotDispatch()
    {
        // A missing body is rejected before any command dispatch (the facade guard is only reached
        // once a body has been read); without a faked request body this path is the observable one.
        ApiResult? result = await Controllers.DialogueController.Update(Context("PUT", "/api/worldengine/dialogue/dt_1"));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(400));
        _facadeMock.Verify(f => f.ExecuteAsync(It.IsAny<UpdateDialogueTreeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Update_WhenNoBody_DoesNotDispatchCommand()
    {
        _ = Controllers.DialogueController.Update(Context("PUT", "/api/worldengine/dialogue/dt_1"));

        _facadeMock.Verify(f => f.ExecuteAsync(It.IsAny<UpdateDialogueTreeCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Update_WhenSucceeds_DoesNotResolveDialogueNpcHook()
    {
        // The controller must never reference the NPC hook; these tests construct no hook.
        Assert.That(typeof(Controllers.DialogueController), Is.Not.SameAs(typeof(AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.DialogueNpcHook)));

        _ = Controllers.DialogueController.Update(Context("PUT", "/api/worldengine/dialogue/dt_1"));
    }

    // ────────────────────────────────────────────────────────────────────
    //  Delete — full CQRS dispatch + status mapping (no NPC hook)
    // ────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Delete_WhenCommandSucceeds_Returns204()
    {
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<DeleteDialogueTreeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        ApiResult? result = await Controllers.DialogueController.Delete(Context("DELETE", "/api/worldengine/dialogue/dt_1"));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(204));
        _facadeMock.Verify(f => f.ExecuteAsync(
            It.IsAny<DeleteDialogueTreeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Delete_WhenCommandFails_Returns404()
    {
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<DeleteDialogueTreeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Fail("No dialogue tree with ID 'dt_1'"));

        ApiResult? result = await Controllers.DialogueController.Delete(Context("DELETE", "/api/worldengine/dialogue/dt_1"));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(404));
        _facadeMock.Verify(f => f.ExecuteAsync(
            It.IsAny<DeleteDialogueTreeCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Delete_WhenFacadeUnavailable_Returns503()
    {
        // A context with no service provider so ResolveFacade returns null.
        RouteContext ctx = Context("DELETE", "/api/worldengine/dialogue/dt_1");
        ctx.Services = null;
        ApiResult? result = await Controllers.DialogueController.Delete(ctx);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task Delete_WhenSucceeds_DoesNotResolveDialogueNpcHook()
    {
        _facadeMock
            .Setup(f => f.ExecuteAsync(
                It.IsAny<DeleteDialogueTreeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // The controller must never reference the NPC hook; these tests construct no hook.
        Assert.That(typeof(Controllers.DialogueController), Is.Not.SameAs(typeof(AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.DialogueNpcHook)));

        ApiResult? result = await Controllers.DialogueController.Delete(Context("DELETE", "/api/worldengine/dialogue/dt_1"));
        Assert.That(result!.StatusCode, Is.EqualTo(204));
    }
}
