using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Tests;

[TestFixture]
public class KnowledgeProgressionQueryTests
{
    private static readonly Guid TestCharacterIdGuid = Guid.Parse("cccc3333-4444-4555-6666-777788889999");
    private static readonly CharacterId TestCharacterId = CharacterId.From(TestCharacterIdGuid);

    private InMemoryKnowledgeProgressionRepository _progressionRepository = null!;
    private InMemoryKnowledgeCapProfileRepository _capProfileRepository = null!;
    private TestWorldConfigProvider _configProvider = null!;
    private InMemoryEventBus _eventBus = null!;
    private KnowledgeProgressionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _progressionRepository = new InMemoryKnowledgeProgressionRepository();
        _capProfileRepository = new InMemoryKnowledgeCapProfileRepository();
        _configProvider = new TestWorldConfigProvider();
        _eventBus = new InMemoryEventBus();

        _service = new KnowledgeProgressionService(
            _progressionRepository, _capProfileRepository, _configProvider, _eventBus);
    }

    [Test]
    public void GetProgression_WhenMissing_ReturnsNullAndDoesNotInsert()
    {
        // When / Then
        KnowledgeProgression? progression = _service.GetProgression(TestCharacterId);

        progression.Should().BeNull();
        _progressionRepository.GetByCharacterId(TestCharacterIdGuid).Should().BeNull();
    }

    [Test]
    public void GetEffectiveSoftCap_WhenMissing_ReturnsConfiguredDefault()
    {
        // Default soft cap is 100.
        _service.GetEffectiveSoftCap(TestCharacterId).Should().Be(100);
        _progressionRepository.GetByCharacterId(TestCharacterIdGuid).Should().BeNull();
    }

    [Test]
    public void GetEffectiveHardCap_WhenMissing_ReturnsConfiguredDefault()
    {
        // Default hard cap is 150.
        _service.GetEffectiveHardCap(TestCharacterId).Should().Be(150);
        _progressionRepository.GetByCharacterId(TestCharacterIdGuid).Should().BeNull();
    }

    [Test]
    public void GetProgressionCostForNextPoint_WhenMissing_ReturnsFirstPointCost()
    {
        // No economy KP earned yet, so cost equals the first point cost (BaseCost = 100).
        _service.GetProgressionCostForNextPoint(TestCharacterId).Should().Be(100);
        _progressionRepository.GetByCharacterId(TestCharacterIdGuid).Should().BeNull();
    }

    [Test]
    public void GetProgression_WhenExisting_ReturnsStoredValues()
    {
        _progressionRepository.Add(new KnowledgeProgression
        {
            CharacterId = TestCharacterIdGuid,
            EconomyEarnedKnowledgePoints = 5,
            LevelUpKnowledgePoints = 2,
            AccumulatedProgressionPoints = 40
        });

        KnowledgeProgression? progression = _service.GetProgression(TestCharacterId);

        progression.Should().NotBeNull();
        progression!.EconomyEarnedKnowledgePoints.Should().Be(5);
        progression.LevelUpKnowledgePoints.Should().Be(2);
        progression.AccumulatedProgressionPoints.Should().Be(40);
    }

    private sealed class TestWorldConfigProvider : IWorldConfigProvider
    {
        private readonly Dictionary<string, object> _values = new()
        {
            [WorldConstants.KnowledgeProgressionBaseCost] = 100,
            [WorldConstants.KnowledgeProgressionCurveType] = "Exponential",
            [WorldConstants.KnowledgePointDefaultSoftCap] = 100,
            [WorldConstants.KnowledgePointDefaultHardCap] = 150
        };

        public bool GetBoolean(string key) => (bool)_values[key];
        public int? GetInt(string key) =>
            _values.TryGetValue(key, out object? value) && value is int i ? (int?)i : (int?)null;
        public float? GetFloat(string key) =>
            _values.TryGetValue(key, out object? value) && value is float f ? (float?)f : (float?)null;
        public string? GetString(string key) =>
            _values.TryGetValue(key, out object? value) && value is string s ? s : null;

        public void SetBoolean(string key, bool value) => _values[key] = value;
        public void SetInt(string key, int value) => _values[key] = value;
        public void SetFloat(string key, float value) => _values[key] = value;
        public void SetString(string key, string value) => _values[key] = value;
    }
}
