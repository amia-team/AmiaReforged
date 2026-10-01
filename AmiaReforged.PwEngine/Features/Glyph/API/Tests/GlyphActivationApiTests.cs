using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using Moq;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Features.Glyph.API.Tests;

[TestFixture, NonParallelizable]
public class GlyphActivationApiTests
{
    private GlyphBootstrap _runtime = null!;
    private Mock<IGlyphRepository> _repository = null!;
    private GlyphDefinition _definition = null!;
    private RouteContext Context => new(null, new() { ["id"] = _definition.Id.ToString() }, CancellationToken.None);
    [SetUp] public void Setup()
    {
        _runtime = new(new GlyphNodeDefinitionRegistry());
        _repository = new();
        _definition = new() { Id = Guid.NewGuid(), Name = "test", EventType = "InteractionPipeline", SourceText = "glyph a : interaction {}" };
        _repository.Setup(r => r.GetDefinitionByIdAsync(_definition.Id)).ReturnsAsync(() => _definition);
        _repository.Setup(r => r.UpdateDefinitionAsync(It.IsAny<GlyphDefinition>())).Returns(Task.CompletedTask);
        GlyphController.Repository = _repository.Object; GlyphController.Runtime = _runtime;
    }
    [TearDown] public void Cleanup() { GlyphController.Repository = null; GlyphController.Runtime = null; }
    private static bool Success(ApiResult result) => JsonSerializer.SerializeToElement(result.Data).GetProperty("Success").GetBoolean();
    [Test] public async Task Language_metadata_is_read_only_and_available_without_repository()
    {
        GlyphController.Repository = null;
        RouteTable routes = new(NLog.LogManager.GetCurrentClassLogger());
        routes.ScanAssembly(typeof(GlyphController).Assembly);
        var result = await routes.DispatchAsync("GET", "/api/worldengine/glyphs/language-metadata", null!, CancellationToken.None);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        var json = JsonSerializer.SerializeToElement(result.Data);
        Assert.That(json.GetProperty("LanguageVersion").GetInt32(), Is.EqualTo(Language.Compilation.GlyphLanguageVersion.Current));
        Assert.That(json.GetProperty("Functions").EnumerateArray().Any(f => f.GetProperty("Name").GetString() == "player.has_item"), Is.True);
        _repository.VerifyNoOtherCalls();
        GlyphController.Runtime = null;
        Assert.That((await GlyphController.LanguageMetadata(Context)).StatusCode, Is.EqualTo(503));
    }

    [Test] public async Task Compile_failure_preserves_last_published_version_and_database_history()
    {
        Assert.That(Success(await GlyphController.Activate(Context)), Is.True);
        GlyphProgramVersion active = _runtime.Programs.GetActive(_definition.Id)!;
        string history = _definition.PublishedVersionsJson;
        _definition.SourceText = "glyph broken : interaction { tick { unknown() } }";
        Assert.That(Success(await GlyphController.Activate(Context)), Is.False);
        Assert.That(_runtime.Programs.GetActive(_definition.Id), Is.SameAs(active));
        Assert.That(_definition.PublishedVersionsJson, Is.EqualTo(history));
        _repository.Verify(r => r.UpdateDefinitionAsync(It.IsAny<GlyphDefinition>()), Times.Once);
    }
    [Test] public async Task Database_failure_prevents_publication()
    {
        await GlyphController.Activate(Context);
        var before = _runtime.Programs.GetActive(_definition.Id);
        _definition.SourceText = "glyph a : interaction { tick { fail \"new\" } }";
        _repository.Setup(r => r.UpdateDefinitionAsync(It.IsAny<GlyphDefinition>())).ThrowsAsync(new IOException("offline"));
        Assert.ThrowsAsync<IOException>(async () => await GlyphController.Activate(Context));
        Assert.That(_runtime.Programs.GetActive(_definition.Id), Is.SameAs(before));
    }
    [Test] public async Task Rollback_persists_previous_source_and_new_version_identity()
    {
        await GlyphController.Activate(Context);
        string oldSource = _definition.SourceText;
        _definition.SourceText = "glyph a : interaction { tick { fail \"new\" } }";
        await GlyphController.Activate(Context);
        var before = _runtime.Programs.GetActive(_definition.Id)!;
        Assert.That(Success(await GlyphController.Rollback(Context)), Is.True);
        Assert.That(_definition.SourceText, Is.EqualTo(oldSource));
        Assert.That(_runtime.Programs.GetActive(_definition.Id)!.VersionId, Is.Not.EqualTo(before.VersionId));
        var stored = JsonSerializer.Deserialize<List<GlyphPublishedVersion>>(_definition.PublishedVersionsJson)!;
        Assert.That(stored.Count, Is.EqualTo(3));
    }
    [Test] public void Production_routes_and_contracts_expose_no_graph_authoring_payloads()
    {
        RouteTable table = new(NLog.LogManager.GetCurrentClassLogger()); table.ScanType(typeof(GlyphController));
        Assert.That(table.GetRoutes().Any(r => r.Pattern.Contains("glyph-catalog")), Is.False);
        Assert.That(typeof(GlyphController.UpdateGlyphRequest).GetProperties().Any(p => p.Name == "GraphJson"), Is.False);
        Assert.That(typeof(GlyphController.CreateGlyphRequest).GetProperties().Any(p => p.Name == "SourceText"), Is.True);
        Assert.That(typeof(GlyphNodeInstance).GetProperties().Any(p => p.Name is "PositionX" or "PositionY" or "Comment"), Is.False);
    }
}
