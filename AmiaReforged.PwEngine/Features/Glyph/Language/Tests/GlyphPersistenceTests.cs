using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlyphPersistenceTests
{
    [Test] public async Task Published_history_roundtrips_independently_of_the_draft()
    {
        var options = new DbContextOptionsBuilder<PwEngineContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var factory = new Mock<IDbContextFactory<PwEngineContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => new PwEngineContext(options));
        var repository = new GlyphRepository(factory.Object);
        var runtime = new GlyphBootstrap(new GlyphNodeDefinitionRegistry());
        var program = runtime.Compiler.Compile("glyph a : interaction {}").Executable!;
        var definition = new GlyphDefinition
        {
            Id = Guid.NewGuid(), Name = "test", EventType = "InteractionPipeline", Category = "Interaction",
            SourceText = program.SourceText, IsActive = true
        };
        await runtime.Programs.ActivateAsync(definition.Id, program);
        definition.PublishedVersionsJson = GlyphPublishedVersion.Serialize(runtime.Programs.GetVersions(definition.Id));
        await repository.CreateDefinitionAsync(definition);
        var draft = (await repository.GetDefinitionByIdAsync(definition.Id))!;
        draft.SourceText = "invalid draft";
        await repository.UpdateDefinitionAsync(draft);
        var loaded = (await repository.GetDefinitionByIdAsync(definition.Id))!;
        Assert.That(loaded.SourceText, Is.EqualTo("invalid draft"));
        var restored = new GlyphRuntimeRegistry();
        GlyphPublishedVersion.Restore(loaded, runtime.Compiler, restored);
        Assert.That(restored.GetActive(definition.Id)!.Executable.SourceHash, Is.EqualTo(program.SourceHash));
    }
}
