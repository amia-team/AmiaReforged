using AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Handlers;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph.Tests;

[TestFixture]
public class GetAreaGraphQueryHandlerTests
{
    private FakeAreaGraphCacheService _cache = null!;
    private GetAreaGraphQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        // Hand-written fake: overrides the cache boundary method so the handler test
        // exercises the real cache contract without needing to build the graph.
        _cache = new FakeAreaGraphCacheService();
        _handler = new GetAreaGraphQueryHandler(_cache);
    }

    [Test]
    public async Task Handler_ReturnsGraphThroughCacheBoundary()
    {
        AreaGraphData graph = new AreaGraphData();
        _cache.Graph = graph;

        AreaGraphData result = await _handler.HandleAsync(new GetAreaGraphQuery());

        Assert.That(result, Is.SameAs(graph));
        Assert.That(_cache.LastForceRefresh, Is.False,
            "Ordinary read must go through the cache boundary without forcing a rebuild");
    }

    [Test]
    public async Task Handler_NeverForcesRefresh()
    {
        AreaGraphData graph = new AreaGraphData();
        _cache.Graph = graph;

        await _handler.HandleAsync(new GetAreaGraphQuery());

        Assert.That(_cache.LastForceRefresh, Is.False);
        Assert.That(_cache.RefreshCount, Is.Zero, "The read query must never force a refresh");
    }

    /// <summary>
    /// Test double for <see cref="AreaGraphCacheService"/>: returns a canned graph
    /// from the same cache boundary the real service exposes, and records how the
    /// handler invoked it.
    /// </summary>
    private sealed class FakeAreaGraphCacheService : AreaGraphCacheService
    {
        public AreaGraphData Graph { get; set; } = new();
        public bool LastForceRefresh { get; private set; }
        public int RefreshCount { get; private set; }

        public override Task<AreaGraphData> GetOrBuildAsync(bool forceRefresh = false)
        {
            LastForceRefresh = forceRefresh;
            return Task.FromResult(Graph);
        }

        public override Task<AreaGraphData> RefreshAsync()
        {
            RefreshCount++;
            return Task.FromResult(Graph);
        }
    }
}
