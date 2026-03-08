using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class NodeTagPatternTests
{
    #region Construction & Validation

    [Test]
    public void Constructor_WithEmptyString_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new NodeTagPattern(""));
    }

    [Test]
    public void Constructor_WithWhitespace_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new NodeTagPattern("   "));
    }

    [Test]
    public void Constructor_WithInvalidTypePrefix_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new NodeTagPattern("type:nonexistent"));
    }

    [Test]
    public void Constructor_WithExactTag_ShouldNotBeWildcard()
    {
        NodeTagPattern pattern = new("ore_vein_cassiterite");

        Assert.That(pattern.IsWildcard, Is.False);
        Assert.That(pattern.IsTypePattern, Is.False);
        Assert.That(pattern.Pattern, Is.EqualTo("ore_vein_cassiterite"));
    }

    [Test]
    public void Constructor_WithGlobWildcard_ShouldBeWildcard()
    {
        NodeTagPattern pattern = new("ore_vein_*");

        Assert.That(pattern.IsWildcard, Is.True);
        Assert.That(pattern.IsTypePattern, Is.False);
    }

    [Test]
    public void Constructor_WithTypePrefix_ShouldBeTypePattern()
    {
        NodeTagPattern pattern = new("type:ore");

        Assert.That(pattern.IsWildcard, Is.True);
        Assert.That(pattern.IsTypePattern, Is.True);
    }

    #endregion

    #region Exact Matching

    [Test]
    public void ExactMatch_WithIdenticalTag_ShouldReturnTrue()
    {
        NodeTagPattern pattern = new("ore_vein_cassiterite");

        Assert.That(pattern.Matches("ore_vein_cassiterite", ResourceType.Ore), Is.True);
    }

    [Test]
    public void ExactMatch_WithDifferentTag_ShouldReturnFalse()
    {
        NodeTagPattern pattern = new("ore_vein_cassiterite");

        Assert.That(pattern.Matches("ore_vein_hematite", ResourceType.Ore), Is.False);
    }

    [Test]
    public void ExactMatch_WithDifferentCase_ShouldReturnFalse()
    {
        // Exact match is case-sensitive (tags are always lowercase by convention)
        NodeTagPattern pattern = new("ore_vein_cassiterite");

        Assert.That(pattern.Matches("Ore_Vein_Cassiterite", ResourceType.Ore), Is.False);
    }

    #endregion

    #region Glob Wildcard Matching

    [Test]
    public void GlobTrailingStar_MatchesSuffix()
    {
        NodeTagPattern pattern = new("ore_vein_*");

        Assert.That(pattern.Matches("ore_vein_cassiterite", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("ore_vein_hematite", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("ore_vein_copper_native", ResourceType.Ore), Is.True);
    }

    [Test]
    public void GlobTrailingStar_DoesNotMatchUnrelatedPrefix()
    {
        NodeTagPattern pattern = new("ore_vein_*");

        Assert.That(pattern.Matches("tree_oak", ResourceType.Tree), Is.False);
        Assert.That(pattern.Matches("geode_amethyst", ResourceType.Geode), Is.False);
    }

    [Test]
    public void GlobMiddleStar_MatchesMiddleSegment()
    {
        NodeTagPattern pattern = new("ore_*_copper");

        Assert.That(pattern.Matches("ore_vein_copper", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("ore_nugget_copper", ResourceType.Ore), Is.True);
    }

    [Test]
    public void GlobMiddleStar_DoesNotMatchWrongSuffix()
    {
        NodeTagPattern pattern = new("ore_*_copper");

        Assert.That(pattern.Matches("ore_vein_iron", ResourceType.Ore), Is.False);
    }

    [Test]
    public void GlobLeadingStar_MatchesAnySuffix()
    {
        NodeTagPattern pattern = new("*_oak");

        Assert.That(pattern.Matches("tree_oak", ResourceType.Tree), Is.True);
        Assert.That(pattern.Matches("plank_oak", ResourceType.Tree), Is.True);
    }

    [Test]
    public void GlobLeadingStar_DoesNotMatchWrongSuffix()
    {
        NodeTagPattern pattern = new("*_oak");

        Assert.That(pattern.Matches("tree_birch", ResourceType.Tree), Is.False);
    }

    [Test]
    public void GlobMultipleStars_ShouldMatchCorrectly()
    {
        NodeTagPattern pattern = new("ore_*_copper_*");

        Assert.That(pattern.Matches("ore_vein_copper_native", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("ore_nugget_copper_raw", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("ore_vein_iron_rich", ResourceType.Ore), Is.False);
    }

    [Test]
    public void GlobSingleStar_MatchesEmptySegment()
    {
        // * should match zero or more characters
        NodeTagPattern pattern = new("ore_vein_*");

        Assert.That(pattern.Matches("ore_vein_", ResourceType.Ore), Is.True);
    }

    [Test]
    public void GlobStar_IsCaseInsensitive()
    {
        NodeTagPattern pattern = new("ore_vein_*");

        Assert.That(pattern.Matches("ORE_VEIN_CASSITERITE", ResourceType.Ore), Is.True);
    }

    [Test]
    public void GlobBroadStar_MatchesEverything()
    {
        NodeTagPattern pattern = new("*");

        Assert.That(pattern.Matches("ore_vein_cassiterite", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("tree_oak", ResourceType.Tree), Is.True);
        Assert.That(pattern.Matches("anything", ResourceType.Undefined), Is.True);
    }

    #endregion

    #region Type Pattern Matching

    [Test]
    public void TypePattern_MatchesCorrectResourceType()
    {
        NodeTagPattern pattern = new("type:ore");

        Assert.That(pattern.Matches("ore_vein_cassiterite", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("ore_vein_hematite", ResourceType.Ore), Is.True);
        Assert.That(pattern.Matches("any_tag_at_all", ResourceType.Ore), Is.True);
    }

    [Test]
    public void TypePattern_DoesNotMatchDifferentResourceType()
    {
        NodeTagPattern pattern = new("type:ore");

        Assert.That(pattern.Matches("tree_oak", ResourceType.Tree), Is.False);
        Assert.That(pattern.Matches("ore_vein_cassiterite", ResourceType.Tree), Is.False);
    }

    [Test]
    public void TypePattern_IsCaseInsensitive()
    {
        NodeTagPattern patternLower = new("type:ore");
        NodeTagPattern patternUpper = new("type:Ore");
        NodeTagPattern patternMixed = new("type:ORE");

        Assert.That(patternLower.Matches("x", ResourceType.Ore), Is.True);
        Assert.That(patternUpper.Matches("x", ResourceType.Ore), Is.True);
        Assert.That(patternMixed.Matches("x", ResourceType.Ore), Is.True);
    }

    [TestCase("type:tree", ResourceType.Tree)]
    [TestCase("type:geode", ResourceType.Geode)]
    [TestCase("type:boulder", ResourceType.Boulder)]
    [TestCase("type:flora", ResourceType.Flora)]
    public void TypePattern_WorksForAllResourceTypes(string pattern, ResourceType expectedType)
    {
        NodeTagPattern p = new(pattern);

        Assert.That(p.Matches("any_tag", expectedType), Is.True);
        Assert.That(p.Matches("any_tag", ResourceType.Undefined), Is.False);
    }

    #endregion

    #region Implicit Conversion

    [Test]
    public void ImplicitConversion_FromString_ShouldCreateNodeTagPattern()
    {
        NodeTagPattern pattern = "ore_vein_*";

        Assert.That(pattern.IsWildcard, Is.True);
        Assert.That(pattern.Pattern, Is.EqualTo("ore_vein_*"));
    }

    [Test]
    public void ImplicitConversion_ToString_ShouldReturnPattern()
    {
        NodeTagPattern pattern = new("ore_vein_cassiterite");
        string value = pattern;

        Assert.That(value, Is.EqualTo("ore_vein_cassiterite"));
    }

    [Test]
    public void ToString_ShouldReturnPattern()
    {
        NodeTagPattern pattern = new("type:ore");

        Assert.That(pattern.ToString(), Is.EqualTo("type:ore"));
    }

    #endregion

    #region Backward Compatibility

    [Test]
    public void KnowledgeHarvestEffect_CanBeConstructedWithString()
    {
        // The implicit conversion means existing code that passes a string still works
        KnowledgeHarvestEffect effect = new("ore_vein_cassiterite",
            Harvesting.HarvestStep.ItemYield, 1.0f,
            KnowledgeSubsystem.EffectOperation.Additive);

        Assert.That(effect.NodeTag.Pattern, Is.EqualTo("ore_vein_cassiterite"));
        Assert.That(effect.NodeTag.IsWildcard, Is.False);
    }

    [Test]
    public void KnowledgeHarvestEffect_CanBeConstructedWithWildcard()
    {
        KnowledgeHarvestEffect effect = new("ore_vein_*",
            Harvesting.HarvestStep.ItemYield, 1.0f,
            KnowledgeSubsystem.EffectOperation.Additive);

        Assert.That(effect.NodeTag.IsWildcard, Is.True);
        Assert.That(effect.NodeTag.Matches("ore_vein_cassiterite", ResourceType.Ore), Is.True);
    }

    #endregion
}
