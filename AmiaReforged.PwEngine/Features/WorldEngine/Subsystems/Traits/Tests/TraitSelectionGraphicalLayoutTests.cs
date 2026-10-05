using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

[TestFixture]
public class TraitSelectionGraphicalLayoutTests
{
    [TestCase(1f, false)]
    [TestCase(1.25f, false)]
    [TestCase(1.5f, false)]
    [TestCase(2f, false)]
    [TestCase(1f, true)]
    [TestCase(1.25f, true)]
    [TestCase(1.5f, true)]
    [TestCase(2f, true)]
    public void ArtworkFitsItsWidgetsAndScalingPreservesPhysicalShell(float scale, bool compact)
    {
        TraitSelectionGraphicalView view = new();
        view.SetScaleFactor(scale);
        NuiLayout root = view.BuildLayout(compact);
        Assert.That(root.Width!.Value * scale, Is.EqualTo(940).Within(0.01));
        Assert.That(root.Height!.Value * scale,
            Is.EqualTo(compact ? TraitSelectionGraphicalView.BaseCompactWindowH : TraitSelectionGraphicalView.BaseWindowH).Within(0.01));
        foreach (NuiElement element in Walk(root))
        foreach (NuiDrawListItem item in element.DrawList ?? [])
        {
            NuiRect? rect = item switch
            {
                NuiDrawListImage image => ((NuiValue<NuiRect>)image.Rect).Value,
                NuiDrawListText text => ((NuiValue<NuiRect>)text.Rect).Value,
                _ => null
            };
            if (rect is not { } bounds) continue;
            Assert.That(bounds.X, Is.GreaterThanOrEqualTo(0), element.Id);
            Assert.That(bounds.Y, Is.GreaterThanOrEqualTo(0), element.Id);
            Assert.That(bounds.Width, Is.GreaterThan(0), element.Id);
            Assert.That(bounds.Height, Is.GreaterThan(0), element.Id);
            Assert.That(bounds.X + bounds.Width, Is.LessThanOrEqualTo(element.Width!.Value + 0.01), element.Id);
            Assert.That(bounds.Y + bounds.Height, Is.LessThanOrEqualTo(element.Height!.Value + 0.01), element.Id);
        }
    }

    [TestCase(1f, false)]
    [TestCase(1.25f, false)]
    [TestCase(1.5f, false)]
    [TestCase(2f, false)]
    [TestCase(1f, true)]
    [TestCase(1.25f, true)]
    [TestCase(1.5f, true)]
    [TestCase(2f, true)]
    public void FixedRowsAndColumnsReserveExactlyTheirDeclaredSpace(float scale, bool compact)
    {
        TraitSelectionGraphicalView view = new();
        view.SetScaleFactor(scale);
        foreach (NuiElement element in Walk(view.BuildLayout(compact)))
        {
            if (element is NuiRow row)
                Assert.That(row.Children.Sum(child => child.Width!.Value), Is.EqualTo(row.Width!.Value).Within(0.01), row.Id);
            if (element is NuiColumn column)
                Assert.That(column.Children.Sum(child => child.Height!.Value), Is.EqualTo(column.Height!.Value).Within(0.01), column.Id);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void HiddenRowsNavigationAndActionsRetainUnconditionalSlots(bool remove)
    {
        TraitSelectionGraphicalView view = new();
        Dictionary<string, NuiElement> elements = Named(view.BuildLayout(false, remove));
        for (int i = 0; i < TraitSelectionGraphicalView.EntriesPerPage; i++)
        {
            NuiGroup slot = (NuiGroup)elements[$"trait_row_slot_{i}"];
            Assert.That(slot.Visible, Is.Not.InstanceOf<NuiBind<bool>>());
            Assert.That(slot.Element!.Visible, Is.SameAs(view.EntryRowVisible[i]));
            NuiImage image = (NuiImage)slot.Element;
            Assert.That(image.ResRef, Is.SameAs(view.EntryTextures[i]));
            Assert.That(image.Enabled, Is.SameAs(view.ControlsEnabled));
        }
        foreach ((string id, NuiBind<bool> visibility) in new[]
                 { ("trait_prev_slot", view.ShowPrevPage), ("trait_next_slot", view.ShowNextPage),
                     ("trait_detail_action_slot", remove ? view.ShowDeselectButton : view.ShowSelectButton) })
        {
            NuiGroup slot = (NuiGroup)elements[id];
            Assert.That(slot.Visible, Is.Not.InstanceOf<NuiBind<bool>>());
            Assert.That(slot.Element!.Visible, Is.SameAs(visibility));
        }
    }

    [Test]
    public void OnlyDetailBodyScrollsAndAllCategoryCanvasesPreserveAspect()
    {
        TraitSelectionGraphicalView view = new();
        NuiLayout root = view.RootLayout();
        NuiText body = Walk(root).OfType<NuiText>().Single();
        Assert.That(body.Id, Is.EqualTo("trait_detail_text"));
        Assert.That(body.Scrollbars, Is.EqualTo(NuiScrollbars.Y));
        Assert.That(body.Text, Is.SameAs(view.DetailBody));
        foreach (NuiGroup group in Walk(root).OfType<NuiGroup>())
        {
            Assert.That(group.Border, Is.False);
            Assert.That(group.Scrollbars, Is.EqualTo(NuiScrollbars.None));
        }
        NuiImage[] categories = Walk(root).OfType<NuiImage>().Where(image => image.Id?.StartsWith("cat_") == true).ToArray();
        Assert.That(categories.Length, Is.EqualTo(9));
        foreach (NuiImage category in categories)
            Assert.That(category.Width!.Value / category.Height!.Value, Is.EqualTo(262f / 72f).Within(0.001));
    }

    [Test]
    public void SwitchingDetailActionUsesAFixedLayoutAndUnregistersTheOldImage()
    {
        TraitSelectionGraphicalView view = new();
        view.RootLayout();
        float width = view.DetailActionSlot.Width!.Value, height = view.DetailActionSlot.Height!.Value;
        foreach (bool remove in new[] { true, false })
        {
            NuiLayout layout = view.BuildDetailActionLayout(remove);
            Assert.That(layout.Width, Is.EqualTo(width));
            Assert.That(layout.Height, Is.EqualTo(height));
            NuiImage image = Walk(layout).OfType<NuiImage>().Single();
            Assert.That(image.Id, Is.EqualTo(remove ? "btn_deselect_trait" : "btn_select_trait"));
            Assert.That(image.Visible, Is.SameAs(remove ? view.ShowDeselectButton : view.ShowSelectButton));
            Assert.That(view.ImageActionIds.Count, Is.EqualTo(23));
            Assert.That(view.ImageActionIds, Does.Not.Contain(remove ? "btn_select_trait" : "btn_deselect_trait"));
        }
    }

    [Test]
    public void HeaderControlsHaveIndependentEnabledBindingsForMissingCharacterErrors()
    {
        TraitSelectionGraphicalView view = new();
        Dictionary<string, NuiElement> elements = Named(view.RootLayout());
        foreach (string id in new[] { "btn_close", "trait_collapse" })
            Assert.That(elements[id].Enabled, Is.SameAs(view.HeaderEnabled));
        foreach (string id in new[] { "btn_confirm", "btn_select_trait", "cat_all", "btn_trait_0" })
            Assert.That(elements[id].Enabled, Is.SameAs(view.ControlsEnabled));
    }

    [Test]
    public void CompactThenExpandedLayoutsPreserveBindsAndRestoreActionRegistration()
    {
        TraitSelectionGraphicalView view = new();
        Dictionary<string, NuiElement> before = Named(view.BuildLayout(false, true));
        Assert.That(view.ImageActionIds.Count, Is.EqualTo(23));
        Assert.That(view.ImageActionIds, Does.Contain("btn_deselect_trait"));
        NuiBind<string> names = view.EntryNames[0];
        NuiLayout compact = view.BuildLayout(true, true);
        Assert.That(view.ImageActionIds, Is.EquivalentTo(new[] { "trait_collapse", "btn_close" }));
        Assert.That(Named(compact), Does.Not.ContainKey("trait_list_panel"));
        Dictionary<string, NuiElement> restored = Named(view.BuildLayout(false, true));
        Assert.That(view.ImageActionIds.Count, Is.EqualTo(23));
        Assert.That(view.EntryNames[0], Is.SameAs(names));
        Assert.That(restored["trait_detail_action_slot"].Width, Is.EqualTo(before["trait_detail_action_slot"].Width));
        Assert.That(restored["trait_detail_action_slot"].Height, Is.EqualTo(before["trait_detail_action_slot"].Height));
        Assert.That(((NuiImage)restored["btn_trait_0"]).ResRef, Is.SameAs(view.EntryTextures[0]));
        Assert.That(view.ImageActionIds, Does.Not.Contain("btn_select_trait"));
    }

    private static Dictionary<string, NuiElement> Named(NuiElement root) => Walk(root)
        .Where(element => !string.IsNullOrEmpty(element.Id)).ToDictionary(element => element.Id!);

    private static IEnumerable<NuiElement> Walk(NuiElement root)
    {
        yield return root;
        IEnumerable<NuiElement> children = root switch
        {
            NuiGroup { Element: { } child } => [child], NuiRow row => row.Children,
            NuiColumn column => column.Children, _ => []
        };
        foreach (NuiElement child in children)
        foreach (NuiElement descendant in Walk(child)) yield return descendant;
    }
}
