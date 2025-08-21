using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class ToolModifierTests
{
    [Test]
    public void Apply_WhenActorDoesNotHaveTool_DoesNothing()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("HAMMER"))
        {
            SuccessChanceDelta = 0.2,
            DurationMultiplier = 0.8,
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("NAILS"), 1.5 } }
        };

        IReactionActor actor = CreateMockActor(tools: []);
        Computation computation = new Computation
        {
            SuccessChance = 0.5,
            Duration = TimeSpan.FromMinutes(30)
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert - nothing should have changed
        Assert.That(computation.SuccessChance, Is.EqualTo(0.5));
        Assert.That(computation.Duration, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(computation.OutputMultipliers, Is.Empty);
    }

    [Test]
    public void Apply_WithMatchingTool_AppliesModifications()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("SAW"))
        {
            SuccessChanceDelta = 0.15,
            DurationMultiplier = 0.75,
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("BOARDS"), 1.25 } }
        };

        ToolInstance tool = new ToolInstance(ToolTag.From("SAW"), quality: 50); // Quality 50 = neutral
        IReactionActor actor = CreateMockActor(tools: [tool]);

        Computation computation = new Computation
        {
            SuccessChance = 0.6,
            Duration = TimeSpan.FromMinutes(20)
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        // Quality factor at 50 should be 1.0, so no scaling
        Assert.That(computation.SuccessChance, Is.EqualTo(0.75)); // 0.6 + 0.15
        Assert.That(computation.Duration, Is.EqualTo(TimeSpan.FromMinutes(15))); // 20 * 0.75
        Assert.That(computation.OutputMultipliers[ItemTag.From("BOARDS")], Is.EqualTo(1.25));
    }

    [Test]
    public void Apply_WithHighQualityTool_ScalesModificationsUp()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("MASTERWORK_CHISEL"))
        {
            SuccessChanceDelta = 0.1,
            DurationMultiplier = 0.8
        };

        ToolInstance highQualityTool = new ToolInstance(ToolTag.From("MASTERWORK_CHISEL"), quality: 90); // High quality
        IReactionActor actor = CreateMockActor(tools: [highQualityTool]);

        Computation computation = new Computation
        {
            SuccessChance = 0.5,
            Duration = TimeSpan.FromMinutes(60)
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        // Quality factor: 1.0 + (90-50)/100 = 1.4
        double expectedSuccessChance = 0.5 + (0.1 * 1.4); // 0.64
        Assert.That(computation.SuccessChance, Is.EqualTo(expectedSuccessChance).Within(0.001));

        // Duration: 60 * (0.8 / 1.4) = 60 * 0.571... ≈ 34.3 minutes
        double expectedDurationMinutes = 60 * (0.8 / 1.4);
        Assert.That(computation.Duration.TotalMinutes, Is.EqualTo(expectedDurationMinutes).Within(0.1));
    }

    [Test]
    public void Apply_WithLowQualityTool_ScalesModificationsDown()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("RUSTY_HAMMER"))
        {
            SuccessChanceDelta = 0.2,
            DurationMultiplier = 0.9
        };

        ToolInstance lowQualityTool = new ToolInstance(ToolTag.From("RUSTY_HAMMER"), quality: 10); // Low quality
        IReactionActor actor = CreateMockActor(tools: [lowQualityTool]);

        Computation computation = new Computation
        {
            SuccessChance = 0.7,
            Duration = TimeSpan.FromMinutes(40)
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        // Quality factor: 1.0 + (10-50)/100 = 0.6 (clamped to minimum 0.6 based on the -0.4 clamp)
        double qualityFactor = 0.6;
        double expectedSuccessChance = 0.7 + (0.2 * qualityFactor); // 0.82
        Assert.That(computation.SuccessChance, Is.EqualTo(expectedSuccessChance).Within(0.001));

        // Duration: 40 * (0.9 / 0.6) = 60 minutes
        double expectedDurationMinutes = 40 * (0.9 / qualityFactor);
        Assert.That(computation.Duration.TotalMinutes, Is.EqualTo(expectedDurationMinutes).Within(0.1));
    }

    [Test]
    public void Apply_WithOutputMultipliers_AppliesCorrectly()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("PRECISION_TOOL"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double>
            {
                { ItemTag.From("WIDGETS"), 1.2 },
                { ItemTag.From("SCREWS"), 0.8 }
            }
        };

        ToolInstance tool = new ToolInstance(ToolTag.From("PRECISION_TOOL"), quality: 70);
        IReactionActor actor = CreateMockActor(tools: [tool]);

        Computation computation = new Computation();

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        Assert.That(computation.OutputMultipliers[ItemTag.From("WIDGETS")], Is.EqualTo(1.2));
        Assert.That(computation.OutputMultipliers[ItemTag.From("SCREWS")], Is.EqualTo(0.8));
    }

    [Test]
    public void Apply_WithExistingOutputMultiplier_MultipliesValues()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("ENHANCED_DRILL"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("HOLES"), 1.5 } }
        };

        ToolInstance tool = new ToolInstance(ToolTag.From("ENHANCED_DRILL"), quality: 60);
        IReactionActor actor = CreateMockActor(tools: [tool]);

        Computation computation = new Computation
        {
            OutputMultipliers =
            {
                [ItemTag.From("HOLES")] = 2.0 // Pre-existing multiplier
            }
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        Assert.That(computation.OutputMultipliers[ItemTag.From("HOLES")], Is.EqualTo(3.0)); // 2.0 * 1.5
    }

    [Test]
    public void Apply_WithSuccessChanceClamping_ClampsToValidRange()
    {
        // Arrange - modifier that would push success chance above 1.0
        ToolModifier modifier = new ToolModifier(ToolTag.From("OVERPOWERED_TOOL"))
        {
            SuccessChanceDelta = 0.8
        };

        ToolInstance excellentTool = new ToolInstance(ToolTag.From("OVERPOWERED_TOOL"), quality: 100);
        IReactionActor actor = CreateMockActor(tools: [excellentTool]);

        Computation computation = new Computation
        {
            SuccessChance = 0.7 // Starting high
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        // Quality factor: 1.0 + (100-50)/100 = 1.5, but clamped to 1.5 max
        // Success chance: 0.7 + (0.8 * 1.5) = 1.9, clamped to 1.0
        Assert.That(computation.SuccessChance, Is.EqualTo(1.0));
    }

    [Test]
    public void Apply_WithMultipleTools_OnlyAppliesForMatchingTool()
    {
        // Arrange
        ToolModifier modifier = new ToolModifier(ToolTag.From("SPECIFIC_WRENCH"))
        {
            SuccessChanceDelta = 0.3
        };

        List<ToolInstance> tools =
        [
            new(ToolTag.From("HAMMER"), 75),
            new(ToolTag.From("SPECIFIC_WRENCH"), 80),
            new(ToolTag.From("SCREWDRIVER"), 65)
        ];
        IReactionActor actor = CreateMockActor(tools: tools);

        Computation computation = new Computation
        {
            SuccessChance = 0.4
        };

        // Act
        modifier.Apply(new ReactionContext(), actor, computation);

        // Assert
        // Should only use the SPECIFIC_WRENCH (quality 80)
        // Quality factor: 1.0 + (80-50)/100 = 1.3
        double expectedSuccessChance = 0.4 + (0.3 * 1.3); // 0.79
        Assert.That(computation.SuccessChance, Is.EqualTo(expectedSuccessChance).Within(0.001));
    }

    private IReactionActor CreateMockActor(List<ToolInstance> tools)
    {
        Mock<IReactionActor> mock = new Mock<IReactionActor>();
        mock.Setup(a => a.Tools).Returns([..tools]);
        mock.Setup(a => a.Knowledge).Returns(ImmutableHashSet<KnowledgeKey>.Empty);
        mock.Setup(a => a.ActorId).Returns(Guid.NewGuid());
        return mock.Object;
    }
}
