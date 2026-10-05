using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

[TestFixture]
public class TraitSelectionImageActionTests
{
    private static readonly TraitSelectionPresenter.ActionAvailability Ready = new(false, false, true, false, 1, 24, true, true);

    [TestCase("btn_close")]
    [TestCase("trait_collapse")]
    [TestCase("btn_confirm")]
    [TestCase("cat_all")]
    [TestCase("cat_background")]
    [TestCase("cat_personality")]
    [TestCase("cat_physical")]
    [TestCase("cat_mental")]
    [TestCase("cat_social")]
    [TestCase("cat_supernatural")]
    [TestCase("cat_curse")]
    [TestCase("cat_blessing")]
    [TestCase("btn_trait_0")]
    [TestCase("btn_trait_1")]
    [TestCase("btn_trait_2")]
    [TestCase("btn_trait_3")]
    [TestCase("btn_trait_4")]
    [TestCase("btn_trait_5")]
    [TestCase("btn_trait_6")]
    [TestCase("btn_trait_7")]
    [TestCase("btn_prev_page")]
    [TestCase("btn_next_page")]
    [TestCase("btn_select_trait")]
    [TestCase("btn_deselect_trait")]
    public void MatchingReleaseDispatchesOnceDespiteBubblingAndClickEvents(string id)
    {
        TraitSelectionPresenter presenter = new(null!, null!);
        TraitSelectionPresenter.ImageContext context = new(Guid.NewGuid(), "all", 1, "brave", "brave", 1, false);
        bool enabled = TraitSelectionPresenter.CanActivateImageAction(id, Ready);
        Assert.That(enabled, Is.True);
        presenter.HandleImageEvent(NuiEventType.MouseDown, id, 0, true, enabled, context);
        presenter.HandleImageEvent(NuiEventType.MouseDown, "trait_shell", null, false, false, context);
        Assert.That(presenter.HandleImageEvent(NuiEventType.Click, id, null, true, enabled, context), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, id, 0, true, enabled, context), Is.True);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "trait_shell", null, false, false, context), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.Click, id, null, true, enabled, context), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, id, 0, true, enabled, context), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction(id, Ready with { Busy = true }), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction(id, Ready with { Closed = true }), Is.False);
    }

    [TestCase("character")]
    [TestCase("category")]
    [TestCase("page")]
    [TestCase("viewed")]
    [TestCase("row")]
    [TestCase("revision")]
    [TestCase("compact")]
    [TestCase("closed")]
    public void StaleReleaseCannotActOnReplacementContent(string change)
    {
        TraitSelectionPresenter presenter = new(null!, null!);
        TraitSelectionPresenter.ImageContext pressed = new(Guid.NewGuid(), "all", 0, "brave", "cowardly", 1, false);
        TraitSelectionPresenter.ImageContext? released = change switch
        {
            "character" => pressed with { CharacterId = Guid.NewGuid() }, "category" => pressed with { Category = "mental" },
            "page" => pressed with { Page = 1 }, "viewed" => pressed with { ViewedTag = "cowardly" },
            "row" => pressed with { RowTag = "replacement" }, "revision" => pressed with { Revision = 2 },
            "compact" => pressed with { Compact = true }, _ => null
        };
        presenter.HandleImageEvent(NuiEventType.MouseDown, "btn_trait_0", 0, true, true, pressed);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_trait_0", 0, true, true, released), Is.False);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_trait_0", 0, true, true, pressed), Is.False);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(null)]
    public void NonLeftOrMissingButtonAndReleaseElsewhereCannotActivate(int? button)
    {
        TraitSelectionPresenter presenter = new(null!, null!);
        TraitSelectionPresenter.ImageContext context = new(Guid.NewGuid(), "all", 0, null, "brave", 1, false);
        presenter.HandleImageEvent(NuiEventType.MouseDown, "btn_trait_0", button, true, true, context);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_trait_0", 0, true, true, context), Is.False);
        presenter.HandleImageEvent(NuiEventType.MouseDown, "btn_trait_0", 0, true, true, context);
        presenter.HandleImageEvent(NuiEventType.MouseUp, "trait_detail_text", null, false, false, context);
        Assert.That(presenter.HandleImageEvent(NuiEventType.MouseUp, "btn_trait_0", 0, true, true, context), Is.False);
    }

    [Test]
    public void MissingCharacterOrCompactLayoutLeavesOnlyHeaderActionsUsable()
    {
        foreach (var state in new[] { Ready with { HasCharacter = false }, Ready with { Compact = true } })
        {
            Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_close", state), Is.True);
            Assert.That(TraitSelectionPresenter.CanActivateImageAction("trait_collapse", state), Is.True);
            foreach (string id in new[] { "btn_confirm", "cat_all", "btn_trait_0", "btn_select_trait", "btn_deselect_trait", "btn_prev_page", "btn_next_page" })
                Assert.That(TraitSelectionPresenter.CanActivateImageAction(id, state), Is.False);
        }
    }

    [Test]
    public void UnavailablePagesActionsCategoriesAndRowsAreRejected()
    {
        Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_prev_page", Ready with { Page = 0 }), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_next_page", Ready with { Page = 2 }), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_select_trait", Ready with { CanSelect = false }), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_deselect_trait", Ready with { CanRemove = false }), Is.False);
        foreach (string id in new[] { "cat_fake", "trait_shell", "btn_trait_8", "btn_trait_-1", "btn_trait_bad" })
            Assert.That(TraitSelectionPresenter.CanActivateImageAction(id, Ready), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_trait_1", Ready with { Page = 2, Count = 17 }), Is.False);
        Assert.That(TraitSelectionPresenter.CanActivateImageAction("btn_trait_0", Ready with { Count = 0 }), Is.False);
    }

    [TestCase(9, 0, 0)]
    [TestCase(9, 1, 0)]
    [TestCase(9, 8, 0)]
    [TestCase(9, 9, 1)]
    [TestCase(9, 17, 2)]
    [TestCase(-1, 17, 0)]
    public void ShrinkingListsClampToAValidPage(int page, int count, int expected)
    {
        Assert.That(TraitSelectionPresenter.ClampPage(page, count), Is.EqualTo(expected));
    }

    [Test]
    public void ViewedIdentitySurvivesReorderingButOldRowCannotChooseReplacement()
    {
        Trait brave = Trait("brave"), cowardly = Trait("cowardly");
        string?[] displayed = ["brave", "cowardly", null, null, null, null, null, null];
        Assert.That(TraitSelectionPresenter.GetDisplayedTrait("btn_trait_0", displayed, new[] { brave, cowardly }, 0), Is.SameAs(brave));
        Assert.That(TraitSelectionPresenter.GetDisplayedTrait("btn_trait_0", displayed, new[] { cowardly, brave }, 0), Is.Null);
        Assert.That(TraitSelectionPresenter.FindViewedTrait(new[] { cowardly, brave }, "brave"), Is.SameAs(brave));
        Assert.That(TraitSelectionPresenter.FindViewedTrait(new[] { cowardly }, "brave"), Is.Null);
    }

    private static Trait Trait(string tag) => new() { Tag = tag, Name = tag, Description = "Description", PointCost = 1 };
}
