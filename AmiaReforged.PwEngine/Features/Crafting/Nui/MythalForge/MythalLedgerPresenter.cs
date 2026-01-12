using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Represents a presenter for the Mythal Ledger window in the Mythal Forge system.
/// This presenter manages the lifecycle and interaction of the Mythal Ledger user interface,
/// including its initialization, creation, and closure.
/// </summary>
public class MythalLedgerPresenter : ScryPresenter<MythalLedgerView>
{
    /// <summary>
    /// Represents the parent presenter instance of type <see cref="MythalForgePresenter"/>.
    /// This variable provides access to the parent context, enabling interactions
    /// and communication between the <see cref="MythalLedgerPresenter"/> and its parent,
    /// such as subscribing to events or invoking parent methods.
    /// </summary>
    private readonly MythalForgePresenter _parent;

    /// <summary>
    /// Represents the player associated with the Mythal Ledger system.
    /// </summary>
    /// <remarks>
    /// This variable holds the instance of the <see cref="NwPlayer"/> that interacts
    /// with the Mythal Ledger window. It is used for sending messages, creating the Nui window,
    /// and other player-specific operations within the Mythal Ledger context.
    /// </remarks>
    private readonly NwPlayer _player;

    /// <summary>
    /// Represents the token used to manage a NuiWindow instance associated with the presenter.
    /// </summary>
    /// <remarks>
    /// This token is utilized to control operations on the NuiWindow, such as creation and closure.
    /// It allows the presenter to interface with the corresponding window and manage its lifecycle.
    /// </remarks>
    private NuiWindowToken _token;

    /// <summary>
    /// Represents a Nullable NuiWindow instance used to display the "Mythal Ledger" UI to the player.
    /// This window is responsible for managing the interface defined by the <see cref="MythalLedgerView"/> layout.
    /// </summary>
    /// <remarks>
    /// The window is initialized with a specific title and geometry settings, and it is set as non-closable and non-resizable.
    /// It is created and managed dynamically during the lifecycle of the <see cref="MythalLedgerPresenter"/> instance.
    /// </remarks>
    private NuiWindow? _window;

    /// <summary>
    /// Represents the presenter responsible for managing and coordinating interactions
    /// between the <see cref="MythalLedgerView"/> and its associated model or parent presenter.
    /// </summary>
    public MythalLedgerPresenter(MythalForgePresenter parent, NwPlayer player, MythalLedgerView toolView)
    {
        View = toolView;
        _parent = parent;
        _player = player;

        parent.ViewUpdated += UpdateLedger;
        parent.ForgeClosing += HandleClose;
    }

    /// <summary>
    /// Gets the associated view object that represents the user interface and
    /// provides bindings or UI elements specific to the current presenter.
    /// </summary>
    /// <remarks>
    /// This property is a key part of the Model-View-Presenter (MVP) pattern.
    /// It allows the presenter to interact with the user interface (view)
    /// in a decoupled and testable manner.
    /// </remarks>
    /// <value>
    /// An instance of <c>MythalLedgerView</c>.
    /// </value>
    public override MythalLedgerView View { get; }

    /// <summary>
    /// Handles the closing action triggered by the parent presenter.
    /// </summary>
    /// <param name="sender">The instance of <see cref="MythalForgePresenter" /> that triggered the event.</param>
    /// <param name="e">The event data associated with the close action.</param>
    private void HandleClose(MythalForgePresenter sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Handles the ledger update process and ensures the binding updates
    /// are applied based on the provided forge presenter model.
    /// </summary>
    /// <param name="sender">
    /// The instance of <c>MythalForgePresenter</c> that triggered the event.
    /// Contains the relevant context for the ledger update operation.
    /// </param>
    /// <param name="e">
    /// The event arguments associated with the event trigger. Typically
    /// holds additional context or metadata about the triggered event.
    /// </param>
    private void UpdateLedger(MythalForgePresenter sender, EventArgs e)
    {
        UpdateLedgerBindings(sender.Model);
    }

    /// <summary>
    /// Updates the ledger bindings to synchronize the UI with the values from the given model.
    /// </summary>
    /// <param name="senderModel">The <see cref="MythalForgeModel"/> containing the data needed
    /// to update the ledger view bindings.</param>
    private void UpdateLedgerBindings(MythalForgeModel senderModel)
    {
        Token().SetBindValue(View.MinorMythalCount, senderModel.MythalCategoryModel.MinorMythals.ToString());
        Token().SetBindValue(View.LesserMythalCount, senderModel.MythalCategoryModel.LesserMythals.ToString());
        Token().SetBindValue(View.IntermediateMythalCount, senderModel.MythalCategoryModel.IntermediateMythals.ToString());
        Token().SetBindValue(View.GreaterMythalCount, senderModel.MythalCategoryModel.GreaterMythals.ToString());
        Token().SetBindValue(View.FlawlessMythalCount, senderModel.MythalCategoryModel.FlawlessMythals.ToString());
        Token().SetBindValue(View.PerfectMythalCount, senderModel.MythalCategoryModel.PerfectMythals.ToString());
        Token().SetBindValue(View.DivineMythalCount, senderModel.MythalCategoryModel.DivineMythals.ToString());
    }

    /// <summary>
    /// Retrieves the current NuiWindowToken instance associated with this presenter.
    /// </summary>
    /// <returns>
    /// The <see cref="NuiWindowToken"/> instance representing the token for the current Nui window.
    /// </returns>
    public override NuiWindowToken Token() => _token;

    /// <summary>
    /// Performs the initialization required before the window can be created or displayed.
    /// This method is called to set up the properties of the NuiWindow,
    /// such as its layout, title, ID, and visual characteristics such as geometry, closability,
    /// and resizability.
    /// </summary>
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), title: "Mythal Ledger")
        {
            Id = "mythal_ledger",
            Geometry = new NuiRect(1600, 500, 200, 250),
            Closable = false,
            Resizable = true
        };
    }

    /// <summary>
    /// Creates and initializes the Mythal Ledger window for the player.
    /// If the window initialization fails, an appropriate error message is sent to the player.
    /// </summary>
    /// <remarks>
    /// This method ensures that the Mythal Ledger window is properly set up and associates it with the player's session.
    /// It first invokes a pre-initialization method if the window instance is uninitialized.
    /// If the window creation still fails, the player is notified to report the issue via a server message.
    /// </remarks>
    public override void Create()
    {
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage("Failed to create Mythal Ledger window." +
                                      " Please screenshot this and file a bug report on the Discord.");
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Failed to create Mythal Ledger window token.");
            return;
        }

        // Initialize the ledger with current values
        if (_parent?.Model != null)
        {
            UpdateLedgerBindings(_parent.Model);
        }
    }

    /// <summary>
    /// Closes the associated window represented by this presenter.
    /// </summary>
    /// <remarks>
    /// This method is responsible for ensuring the proper closure of the NuiWindowToken instance
    /// and any associated resources or behaviors tied to this presenter.
    /// Typically used when the related parent or system signals the window to terminate.
    /// </remarks>
    public override void Close()
    {
        _token.Close();
    }
}
