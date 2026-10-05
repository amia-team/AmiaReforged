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
