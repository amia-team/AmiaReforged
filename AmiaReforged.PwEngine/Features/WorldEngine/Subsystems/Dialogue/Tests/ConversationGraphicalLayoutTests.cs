using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class ConversationGraphicalLayoutTests
{
    [TestCase(1f)]
    [TestCase(1.25f)]
    [TestCase(1.5f)]
    [TestCase(2f)]
    public void ScalingPreservesPhysicalWindowAndAllArtworkFitsItsWidget(float scale)
    {
        ConversationGraphicalView view = new();
        view.SetScaleFactor(scale);
        NuiLayout root = view.RootLayout();
        Assert.That(root.Width!.Value * scale, Is.EqualTo(885).Within(0.01));
        Assert.That(root.Height!.Value * scale, Is.EqualTo(762).Within(0.01));

        foreach (NuiElement element in Walk(root))
        {
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
                Assert.That(bounds.X + bounds.Width, Is.LessThanOrEqualTo(element.Width!.Value + 0.01f), element.Id);
                Assert.That(bounds.Y + bounds.Height, Is.LessThanOrEqualTo(element.Height!.Value + 0.01f), element.Id);
            }
        }
    }

    [TestCase(1f)]
    [TestCase(1.5f)]
    [TestCase(2f)]
    public void FixedRowsAndColumnsExactlyReserveTheirDeclaredSpace(float scale)
    {
        ConversationGraphicalView view = new();
        view.SetScaleFactor(scale);
        foreach (NuiElement element in Walk(view.RootLayout()))
        {
            if (element is NuiRow row)
                Assert.That(row.Children.Sum(child => child.Width!.Value), Is.EqualTo(row.Width!.Value).Within(0.01), row.Id);
            if (element is NuiColumn column)
                Assert.That(column.Children.Sum(child => child.Height!.Value), Is.EqualTo(column.Height!.Value).Within(0.01), column.Id);
        }
    }

    [TestCase(1f)]
    [TestCase(1.25f)]
    [TestCase(1.5f)]
    [TestCase(2f)]
    public void PortraitCropsHugeTexturePaddingAndFillsTheFrameAtEveryGuiScale(float scale)
    {
        ConversationGraphicalView view = new();
        view.SetScaleFactor(scale);
        NuiImage portrait = (NuiImage)Walk(view.RootLayout()).Single(element => element.Id == "conv_portrait_image");
        NuiRect crop = ((NuiValue<NuiRect>)portrait.ImageRegion!).Value;
        Assert.That(crop.X, Is.Zero);
        Assert.That(crop.Y, Is.Zero);
        Assert.That(crop.Width, Is.EqualTo(256));
        Assert.That(crop.Height, Is.EqualTo(400));
        Assert.That(((NuiValue<NuiAspect>)portrait.ImageAspect!).Value, Is.EqualTo(NuiAspect.Stretch));
        Assert.That(portrait.Width!.Value * scale, Is.EqualTo(311 * 0.75f).Within(0.01));
        Assert.That(portrait.Height!.Value * scale, Is.EqualTo(443 * 0.75f).Within(0.01));
        Assert.That(portrait.ResRef, Is.SameAs(view.NpcPortrait));
    }

    [Test]
    public void HidingChoicesPaginationAndMoreNeverHidesTheirReservedSlots()
    {
        ConversationGraphicalView view = new();
        Dictionary<string, NuiElement> elements = Walk(view.RootLayout())
            .Where(element => !string.IsNullOrEmpty(element.Id)).ToDictionary(element => element.Id!);
        for (int i = 0; i < ConversationGraphicalView.MaxVisibleChoices; i++)
        {
            NuiGroup slot = (NuiGroup)elements[$"conv_choice_slot_{i}"];
            Assert.That(slot.Visible, Is.Not.InstanceOf<NuiBind<bool>>());
            Assert.That(slot.Width, Is.GreaterThan(0));
            Assert.That(slot.Height, Is.GreaterThan(0));
            Assert.That(slot.Element!.Visible, Is.SameAs(view.ChoiceVisible[i]));
        }
        NuiGroup pagination = (NuiGroup)elements["conv_text_pagination_slot"];
        Assert.That(pagination.Visible, Is.Not.InstanceOf<NuiBind<bool>>());
        Assert.That(pagination.Element!.Visible, Is.SameAs(view.ShowTextPagination));
        NuiGroup more = (NuiGroup)elements["conv_more_slot"];
        Assert.That(more.Visible, Is.Not.InstanceOf<NuiBind<bool>>());
        Assert.That(more.Element!.Visible, Is.SameAs(view.ShowMoreButton));
    }

    [Test]
    public void ActionImagesBindTheirEnabledStateToPresenterControls()
    {
        ConversationGraphicalView view = new();
        Dictionary<string, NuiImage> images = Walk(view.RootLayout()).OfType<NuiImage>()
            .ToDictionary(image => image.Id!);
        Assert.That(images["conv_close"].Enabled, Is.SameAs(view.ControlsEnabled));
        Assert.That(images["btn_goodbye"].Enabled, Is.SameAs(view.ControlsEnabled));
        Assert.That(images["btn_more"].Enabled, Is.SameAs(view.ChoicesEnabled));
        Assert.That(images["btn_prev_text"].Enabled, Is.SameAs(view.ShowPrevTextPage));
        Assert.That(images["btn_next_text"].Enabled, Is.SameAs(view.ShowNextTextPage));
        for (int i = 0; i < ConversationGraphicalView.MaxVisibleChoices; i++)
            Assert.That(images[$"btn_choice_{i}"].Enabled, Is.SameAs(view.ChoicesEnabled));
    }

    [Test]
    public void RebuildingRegistersOnlyTheTenActionImagesAndPreservesTopOrnament()
    {
        ConversationGraphicalView view = new();
        view.RootLayout();
        NuiLayout root = view.RootLayout();
        Assert.That(view.ImageActionIds, Is.EquivalentTo(new[]
        {
            "conv_close", "btn_prev_text", "btn_next_text", "btn_goodbye", "btn_more",
            "btn_choice_0", "btn_choice_1", "btn_choice_2", "btn_choice_3", "btn_choice_4"
        }));
        foreach (string id in view.ImageActionIds)
            Assert.That(Walk(root).Single(element => element.Id == id), Is.InstanceOf<NuiImage>());
        NuiGroup shell = (NuiGroup)Walk(root).Single(element => element.Id == "conv_shell");
        string[] resources = shell.DrawList!.OfType<NuiDrawListImage>()
            .Select(image => ((NuiValue<string>)image.ResRef).Value!).ToArray();
        Assert.That(resources, Does.Contain("ui_dlg_fr_top_m"));
        Assert.That(resources, Does.Not.Contain("ui_dlg_fr_top"));
    }

    private static IEnumerable<NuiElement> Walk(NuiElement root)
    {
        yield return root;
        IEnumerable<NuiElement> children = root switch
        {
            NuiGroup { Element: { } child } => [child],
            NuiRow row => row.Children,
            NuiColumn column => column.Children,
            _ => []
        };
        foreach (NuiElement child in children)
        foreach (NuiElement descendant in Walk(child))
            yield return descendant;
    }
}
