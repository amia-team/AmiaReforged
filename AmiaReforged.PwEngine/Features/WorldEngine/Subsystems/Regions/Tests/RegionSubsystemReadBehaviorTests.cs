using System.Threading;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Tests;

/// <summary>
/// Behavioral tests for the region facade read path
/// (`RegionSubsystem.GetRegionAsync` / `GetAllRegionsAsync`).
/// A real <see cref="QueryDispatcher"/> is wired to the real region query handlers and an
/// <see cref="InMemoryRegionRepository"/> so the full route (query → handler → projection) is exercised.
/// </summary>
[TestFixture]
public class RegionSubsystemReadBehaviorTests
{
    private static RegionSubsystem BuildSubsystem(InMemoryRegionRepository repo, out QueryDispatcher dispatcher)
    {
        IQueryDispatcher queryDispatcher = new QueryDispatcher(
            new IQueryHandlerMarker[]
            {
                new GetRegionDefinitionHandler(repo),
                new SearchRegionDefinitionsHandler(repo)
            });
        dispatcher = (QueryDispatcher)queryDispatcher;
        return new RegionSubsystem(repo, queryDispatcher);
    }

    private static RegionDefinition Region(string tag, string name, string? description = null, RegionType? type = null)
        => new()
        {
            Tag = new RegionTag(tag),
            Name = name,
            Description = description,
            Type = type,
            Areas = []
        };

    [Test]
    public async Task GetRegionAsync_KnownTag_ReturnsMappedProjection()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("wilderness_north", "North Wildlands", "Frozen hills", RegionType.Wilderness));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("wilderness_north", CancellationToken.None);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Tag, Is.EqualTo("wilderness_north"));
        Assert.That(info.Name, Is.EqualTo("North Wildlands"));
        Assert.That(info.Description, Is.EqualTo("Frozen hills"));
        Assert.That(info.Type, Is.EqualTo(RegionType.Wilderness));
    }

    [Test]
    public async Task GetRegionAsync_CaseInsensitive_TagLookup()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("Dungeon_Deep", "Deep Dungeon"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("dungeon_deep", CancellationToken.None);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Name, Is.EqualTo("Deep Dungeon"));
    }

    [Test]
    public async Task GetRegionAsync_UnknownTag_ReturnsNull()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("known", "Known Region"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("does_not_exist", CancellationToken.None);

        Assert.That(info, Is.Null);
    }

    [Test]
    public async Task GetAllRegionsAsync_IncludesRegisteredRegions()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One"));
        repo.Add(Region("r2", "Region Two"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        List<RegionInfo> all = await subsystem.GetAllRegionsAsync(CancellationToken.None);

        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all.Select(r => r.Tag), Contains.Item("r1"));
        Assert.That(all.Select(r => r.Tag), Contains.Item("r2"));
    }

    [Test]
    public async Task GetAllRegionsAsync_EmptyRepository_ReturnsEmptyList()
    {
        InMemoryRegionRepository repo = new();
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        List<RegionInfo> all = await subsystem.GetAllRegionsAsync(CancellationToken.None);

        Assert.That(all, Is.Not.Null);
        Assert.That(all, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetRegionAsync_UnsetDescriptionAndType_ProjecsContractDefaults()
    {
        InMemoryRegionRepository repo = new();
        // No Description, no Type — exercises the compatibility defaults from the 033 contract.
        repo.Add(Region("bare", "Bare Region"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("bare", CancellationToken.None);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Description, Is.EqualTo(string.Empty));
        Assert.That(info.Type, Is.EqualTo(RegionType.Special));
    }
}
