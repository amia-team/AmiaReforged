using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Represents a user interface view for displaying and managing the ledger
/// of mythals in a crafting context.
/// </summary>
public sealed class MythalLedgerView : ScryView<MythalLedgerPresenter>
{
    /// <summary>
    /// Represents the data-binding key for the count of divine mythals in the context of the UI ledger view.
    /// This variable is used to dynamically bind and display the count of divine mythals
    /// within the MythalLedgerView interface.
    /// </summary>
    public NuiBind<string> DivineMythalCount = new(key: "divine_mythals");

    /// <summary>
    /// Represents a data binding for the count of flawless mythals in the Mythal Ledger view.
    /// This binding is linked to the UI component displaying the number of flawless mythals
    /// and is dynamically updated to reflect changes in the associated model data.
    /// </summary>
    public NuiBind<string> FlawlessMythalCount = new(key: "flawless_mythals");

    /// <summary>
    /// Represents a bindable UI element for displaying the count of Greater Mythals
    /// in the Mythal Ledger. This value is tied to the backend model and dynamically
    /// updates based on the current state of the Mythal Forge system.
    /// </summary>
    public NuiBind<string> GreaterMythalCount = new(key: "greater_mythals");

    /// <summary>
    /// Represents the binding for the count of Intermediate-graded Mythals displayed in the Mythal Ledger UI.
    /// This value is dynamically updated to reflect the current count of Intermediate Mythals available
    /// in the associated <see cref="MythalForgeModel"/>.
    /// </summary>
    public NuiBind<string> IntermediateMythalCount = new(key: "intermediate_mythals");

    /// <summary>
    /// Represents the bind key for displaying the count of Lesser Mythals in the Mythal Forge UI.
    /// </summary>
    /// <remarks>
    /// This bind is associated with the "lesser_mythals" key and is used to dynamically update the UI
    /// with the current number of Lesser Mythals available in the Mythal Forge system.
    /// </remarks>
    public NuiBind<string> LesserMythalCount = new(key: "lesser_mythals");

    /// <summary>
    /// Represents the bound value corresponding to the number of minor mythals in the Mythal Ledger UI.
    /// </summary>
    /// <remarks>
    /// This variable is used to bind and display the count of minor mythals in the Mythal Ledger view.
    /// It is updated dynamically to reflect the current state of minor mythals in the associated data model.
    /// </remarks>
    public NuiBind<string> MinorMythalCount = new(key: "minor_mythals");

    /// <summary>
    /// Represents a reference to the parent object of type MythalForgePresenter.
    /// This variable is typically used to establish a relationship between the parent object
    /// and the current MythalLedgerView instance, enabling interaction and data exchange.
    /// </summary>
    public MythalForgePresenter Parent;

    /// <summary>
    /// Represents a bindable user interface element that displays the count of "Perfect" Mythal objects in the Mythal Ledger.
    /// This binding is dynamically updated through the <see cref="MythalLedgerPresenter"/> to reflect changes in the data
    /// model for Perfect Mythals. It is utilized within the NUI layout of the <see cref="MythalLedgerView"/> to present the
    /// count visually within the application's crafting system.
    /// </summary>
    public NuiBind<string> PerfectMythalCount = new(key: "perfect_mythals");


    /// <summary>
    /// Represents the Ledger UI view for managing and displaying Mythal-related counts and information
    /// within the crafting system of the Mythal Forge.
    /// </summary>
    /// <remarks>
    /// This view is responsible for binding data related to various Mythal types (such as Divine, Flawless,
    /// Greater, Intermediate, Lesser, Minor, and Perfect Mythals) for display and interaction in the UI.
    /// It acts as a bridge between the <see cref="MythalLedgerPresenter"/> and the UI layer, ensuring
    /// proper rendering and functionality.
    /// The UI binds use the given keys to update or retrieve information on the respective types of Mythals.
    /// </remarks>
    public MythalLedgerView(MythalForgePresenter parent, NwPlayer player)
    {
        Parent = parent;
        Presenter = new MythalLedgerPresenter(parent, player, this);
    }

    /// Gets or sets the presenter associated with the current view.
    /// This property represents the instance of the `MythalLedgerPresenter`
    /// assigned to manage the interaction logic and data binding for the `MythalLedgerView`.
    /// It is protected to restrict direct modification outside the view implementation.
    public override MythalLedgerPresenter Presenter { get; protected set; }

    /// <summary>
    /// Generates the root layout for the Mythal Ledger view.
    /// The layout consists of rows displaying counts of different levels of mythals
    /// including Minor, Lesser, Intermediate, Greater, Flawless, Perfect, and Divine.
    /// </summary>
    /// <returns>
    /// A <see cref="NuiLayout"/> representing the structure of the Mythal Ledger view.
    /// </returns>
    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Minor:"),
                        new NuiLabel(MinorMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Lesser:"),
                        new NuiLabel(LesserMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Intermediate:"),
                        new NuiLabel(IntermediateMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Greater:"),
                        new NuiLabel(GreaterMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Flawless:"),
                        new NuiLabel(FlawlessMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Perfect:"),
                        new NuiLabel(PerfectMythalCount)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel(label: "Divine:"),
                        new NuiLabel(DivineMythalCount)
                    }
                }
            }
        };
}
