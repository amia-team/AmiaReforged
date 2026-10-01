using System.Text.Json;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using AmiaReforged.PwEngine.Features.WorldEngine.API;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.API.Tests;

[TestFixture, NonParallelizable]
public sealed class GlyphModuleApiTests
{
    private GlyphBootstrap _runtime = null!;
    private Mock<IGlyphModuleRepository> _repository = null!;
    private readonly Dictionary<Guid, GlyphModule> _stored = [];
    private GlyphDefinition _definition = null!;
    private static GlyphModule Copy(GlyphModule m) => JsonSerializer.Deserialize<GlyphModule>(JsonSerializer.Serialize(m))!;
    [SetUp] public void Setup()
    {
        _stored.Clear(); _runtime = new(new GlyphNodeDefinitionRegistry()); _repository = new();
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(() => _stored.Values.Select(Copy).ToList());
        _repository.Setup(r => r.GetAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => _stored.TryGetValue(id, out var m) ? Copy(m) : null);
        _repository.Setup(r => r.SaveAsync(It.IsAny<GlyphModule>(), It.IsAny<bool>())).Callback<GlyphModule, bool>((m, _) => _stored[m.Id] = Copy(m)).Returns(Task.CompletedTask);
        GlyphController.Runtime = _runtime; GlyphModuleController.Repository = _repository.Object;
        _definition = new() { Id = Guid.NewGuid(), EventType = "InteractionPipeline", SourceText = "using helpers glyph t : interaction { attempted { fail TEXT } }" };
        var scripts = new Mock<IGlyphRepository>();
        scripts.Setup(r => r.GetDefinitionByIdAsync(_definition.Id)).ReturnsAsync(() => _definition);
        scripts.Setup(r => r.UpdateDefinitionAsync(It.IsAny<GlyphDefinition>())).Returns(Task.CompletedTask);
        GlyphController.Repository = scripts.Object;
    }
    [TearDown] public void Cleanup() { GlyphController.Runtime = null; GlyphController.Repository = null; GlyphModuleController.Repository = null; }
    private GlyphModule Draft(string name, string body)
    {
        GlyphModule module = new() { Name = name, SourceText = $"mod {name} {{ {body} }}" }; _stored[module.Id] = Copy(module); return module;
    }
    private async Task<ApiResult> Publish(GlyphModule module, string? source = null)
    {
        await GlyphModuleController.RefreshAsync();
        var revision = GlyphModuleRevision.Create(module.Name, source ?? module.SourceText);
        var check = _runtime.Compiler.CompileModule(revision);
        Assert.That(check.Success, Is.True, string.Join("\n", check.Diagnostics));
        return await GlyphModuleController.PublishAsync(module.Id, new(revision.SourceText, GlyphModuleController.Fingerprint(revision, check)));
    }
    [Test] public async Task Drafts_are_not_importable_and_publication_retains_immutable_history()
    {
        var module = Draft("helpers", "pub const TEXT = \"first\"");
        await GlyphModuleController.RefreshAsync();
        Assert.That(_runtime.Compiler.Compile(_definition.SourceText).Success, Is.False);
        Assert.That((await Publish(module)).StatusCode, Is.EqualTo(200));
        Guid first = _stored[module.Id].ActiveRevisionId!.Value;
        Assert.That((await Publish(module, "mod helpers { pub const TEXT = \"second\" }")).StatusCode, Is.EqualTo(200));
        Assert.That(_stored[module.Id].History().Count, Is.EqualTo(2));
        Assert.That(_runtime.Compiler.Modules.Snapshot.Find(first)!.SourceText, Is.EqualTo(module.SourceText));
    }
    [Test] public async Task Publication_requires_current_validation_and_preserves_head_on_persistence_failure()
    {
        var module = Draft("helpers", "pub const TEXT = \"first\"");
        Assert.That((await GlyphModuleController.PublishAsync(module.Id, new())).StatusCode, Is.EqualTo(409));
        await Publish(module); Guid head = _stored[module.Id].ActiveRevisionId!.Value;
        string history = _stored[module.Id].PublishedVersionsJson;
        _repository.Setup(r => r.SaveAsync(It.IsAny<GlyphModule>(), It.IsAny<bool>())).ThrowsAsync(new IOException("offline"));
        Assert.ThrowsAsync<IOException>(async () => await Publish(module, "mod helpers { pub const TEXT = \"second\" }"));
        Assert.That(_runtime.Compiler.Modules.Snapshot.Find("helpers")!.RevisionId, Is.EqualTo(head));
        Assert.That(_stored[module.Id].PublishedVersionsJson, Is.EqualTo(history));
    }
    [Test] public async Task Updating_a_dependency_requires_revalidation_before_module_publication()
    {
        var helper = Draft("helpers", "pub const TEXT = \"first\""); await Publish(helper);
        var consumer = Draft("consumer", "using helpers pub fn text(): String = TEXT");
        var revision = GlyphModuleRevision.Create(consumer.Name, consumer.SourceText);
        string hash = GlyphModuleController.Fingerprint(revision, _runtime.Compiler.CompileModule(revision));
        await Publish(helper, "mod helpers { pub const TEXT = \"second\" }");
        Assert.That((await GlyphModuleController.PublishAsync(consumer.Id, new(consumer.SourceText, hash))).StatusCode, Is.EqualTo(409));
        Assert.That(_stored[consumer.Id].ActiveRevisionId, Is.Null);
    }
    [Test] public async Task Script_activation_rejects_changed_dependencies_and_rollback_restore_keep_old_sources()
    {
        var helper = Draft("helpers", "pub const TEXT = \"first\""); await Publish(helper);
        var first = _runtime.Compiler.Compile(_definition.SourceText); Guid old = _stored[helper.Id].ActiveRevisionId!.Value;
        Assert.That((await GlyphController.ActivateAsync(_definition.Id, new(_definition.SourceText, ExpectedCompilationHash: first.Executable!.CompilationHash))).StatusCode, Is.EqualTo(200));
        var active = _runtime.Programs.GetActive(_definition.Id);
        await Publish(helper, "mod helpers { pub const TEXT = \"second\" }");
        Assert.That(_runtime.Programs.GetActive(_definition.Id), Is.SameAs(active));
        Assert.That((await GlyphController.ActivateAsync(_definition.Id, new(_definition.SourceText, ExpectedCompilationHash: first.Executable.CompilationHash))).StatusCode, Is.EqualTo(409));
        var second = _runtime.Compiler.Compile(_definition.SourceText);
        await GlyphController.ActivateAsync(_definition.Id, new(_definition.SourceText, ExpectedCompilationHash: second.Executable!.CompilationHash));
        var context = new RouteContext(null, new() { ["id"] = _definition.Id.ToString() }, CancellationToken.None);
        await GlyphController.Rollback(context);
        Assert.That(_runtime.Programs.GetActive(_definition.Id)!.Executable.DependencyLock.Single().RevisionId, Is.EqualTo(old));
        _runtime.Compiler.Modules.Replace(GlyphModuleSnapshot.Empty);
        GlyphRuntimeRegistry restored = new(); GlyphPublishedVersion.Restore(_definition, _runtime.Compiler, restored);
        Assert.That(restored.GetActive(_definition.Id)!.Executable.CompilationHash, Is.EqualTo(first.Executable.CompilationHash));
    }
    [Test] public async Task Archiving_preserves_pinned_dependencies_of_published_consumers()
    {
        var helper = Draft("helpers", "pub const TEXT = \"first\""); await Publish(helper);
        var consumer = Draft("consumer", "using helpers pub fn text(): String = TEXT"); await Publish(consumer);
        await GlyphModuleController.Archive(new(null, new() { ["id"] = helper.Id.ToString() }, CancellationToken.None));
        Assert.That(_runtime.Compiler.Modules.Snapshot.Find("helpers"), Is.Null);
        var result = _runtime.Compiler.Compile("using consumer glyph t : interaction { attempted { fail text() } }");
        Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));
    }
    [Test] public async Task Module_drafts_and_published_history_persist_separately_in_the_repository()
    {
        var options = new DbContextOptionsBuilder<PwEngineContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<PwEngineContext>>(); factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => new PwEngineContext(options));
        var repository = new GlyphModuleRepository(factory.Object);
        var module = Draft("helpers", "pub const TEXT = \"first\""); await Publish(module);
        var stored = Copy(_stored[module.Id]); await repository.SaveAsync(stored, true);
        stored.SourceText = "invalid draft"; await repository.SaveAsync(stored);
        var loaded = (await repository.GetAsync(stored.Id))!;
        Assert.That(loaded.SourceText, Is.EqualTo("invalid draft"));
        Assert.That(loaded.History().Single().Revision.SourceText, Is.EqualTo(module.SourceText));
    }
    [Test] public async Task Metadata_filters_private_declarations_and_keeps_origin_and_availability()
    {
        var helper = Draft("helpers", "const SECRET = \"secret\" fn hidden(): String = SECRET pub fn text(): String = hidden() pub struct Item { actor: Object }"); await Publish(helper);
        var metadata = GlyphModuleMetadata.Create(_runtime.Compiler, "using helpers glyph t : interaction {}");
        Assert.That(metadata.Functions.Select(f => f.Name), Does.Contain("text").And.Contain("helpers.text").And.Not.Contain("hidden").And.Not.Contain("helpers.hidden"));
        Assert.That(metadata.Constants, Is.Empty);
        Assert.That(metadata.SourceLocations["text"].SourceId, Does.StartWith("helpers@"));
        Assert.That(metadata.Aggregates.Select(a => a.Name), Does.Contain("helpers.Item"));
        Assert.That(metadata.Functions.First(f => f.Name == "text").AvailableIn.Any(s => s.Event == "interaction" && s.Stage == "attempted"), Is.True);
        var own = GlyphModuleMetadata.Create(_runtime.Compiler, helper.SourceText);
        Assert.That(own.Functions.Select(f => f.Name), Does.Contain("hidden"));
        Assert.That(own.Constants.Select(c => c.Name), Does.Contain("SECRET"));
    }
    [Test] public async Task Module_rollback_selects_the_previous_head_without_removing_retained_revisions()
    {
        var helper = Draft("helpers", "pub const TEXT = \"first\""); await Publish(helper);
        Guid first = _stored[helper.Id].ActiveRevisionId!.Value;
        await Publish(helper, "mod helpers { pub const TEXT = \"second\" }");
        Guid second = _stored[helper.Id].ActiveRevisionId!.Value;
        var result = await GlyphModuleController.Rollback(new(null, new() { ["id"] = helper.Id.ToString() }, CancellationToken.None));
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(_runtime.Compiler.Modules.Snapshot.Find("helpers")!.RevisionId, Is.EqualTo(first));
        Assert.That(_runtime.Compiler.Modules.Snapshot.Find(second), Is.Not.Null);
        Assert.That(_stored[helper.Id].History().Count, Is.EqualTo(2));
    }

}
