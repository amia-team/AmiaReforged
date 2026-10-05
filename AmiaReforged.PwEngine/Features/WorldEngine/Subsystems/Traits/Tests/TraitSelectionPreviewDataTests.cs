using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui.Prototype;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

[TestFixture]
public class TraitSelectionPreviewDataTests
{
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(8, 1)]
    [TestCase(9, 2)]
    [TestCase(17, 3)]
    [TestCase(32, 4)]
    public void NumericScenariosProduceRealPageBoundaries(int count, int pages)
    {
        TraitSelectionPreviewData data = TraitSelectionPreviewData.Create("rows", count);
        Assert.That(data.Traits.Count, Is.EqualTo(count));
        Assert.That(data.PageCount, Is.EqualTo(pages));
        Assert.That(data.Page, Is.Zero);
        if (count == 0)
        {
            Assert.That(data.ViewedIndex, Is.EqualTo(-1));
            Assert.That(data.ShowSelect || data.ShowRemove, Is.False);
        }
        else Assert.That(data.ViewedIndex, Is.InRange(0, count - 1));
    }

    [Test]
    public void LastPageContainsOneRowAndViewsThatRow()
    {
        TraitSelectionPreviewData data = TraitSelectionPreviewData.Create("last");
        Assert.That(data.Page, Is.EqualTo(2));
        Assert.That(data.PageCount, Is.EqualTo(3));
        Assert.That(data.ViewedIndex, Is.EqualTo(16));
        Assert.That(data.Traits.Count - data.Page * 8, Is.EqualTo(1));
    }

    [Test]
    public void ReferenceSampleViewsADifferentTraitFromTheAcquiredTrait()
    {
        TraitSelectionPreviewData data = TraitSelectionPreviewData.Create("standard");
        Assert.That(data.Traits[0].Acquired, Is.True);
        Assert.That(data.Traits[1].Acquired, Is.False);
        Assert.That(data.ViewedIndex, Is.EqualTo(1));
        Assert.That(data.DetailTitle, Is.EqualTo("Cowardly"));
        Assert.That(data.Budget.AvailablePoints, Is.EqualTo(1));
        Assert.That(data.ShowSelect, Is.True);
        Assert.That(data.ShowRemove, Is.False);
    }

    [TestCase("remove", false, true)]
    [TestCase("confirmed", false, false)]
    [TestCase("empty", false, false)]
    public void AcquiredConfirmedAndEmptyCasesKeepActionsMutuallyExclusive(string scenario, bool select, bool remove)
    {
        TraitSelectionPreviewData data = TraitSelectionPreviewData.Create(scenario);
        Assert.That(data.ShowSelect, Is.EqualTo(select));
        Assert.That(data.ShowRemove, Is.EqualTo(remove));
    }

    [TestCase("debt", -2)]
    [TestCase("zero", 0)]
    public void BudgetSamplesStillShowSelectAtZeroOrNegativeBalance(string scenario, int balance)
    {
        TraitSelectionPreviewData data = TraitSelectionPreviewData.Create(scenario);
        Assert.That(data.Budget.AvailablePoints, Is.EqualTo(balance));
        Assert.That(data.ShowSelect, Is.True);
        Assert.That(data.Enabled, Is.True);
    }

    [Test]
    public void LongContentHasAccessibleReviewSentinelAndCategorySampleMatchesFilter()
    {
        TraitSelectionPreviewData longData = TraitSelectionPreviewData.Create("long");
        Assert.That(longData.DetailBody.Length, Is.GreaterThan(3000));
        Assert.That(longData.DetailBody, Does.EndWith("END OF LONG TRAIT SAMPLE"));
        TraitSelectionPreviewData category = TraitSelectionPreviewData.Create("category");
        Assert.That(category.ActiveCategory, Is.EqualTo("mental"));
        Assert.That(category.Traits.All(trait => trait.Category == "Mental"), Is.True);
        Assert.That(TraitSelectionPreviewData.Create("disabled").Enabled, Is.False);
        Assert.That(TraitSelectionPreviewData.Create("error").Enabled, Is.False);
    }
}
