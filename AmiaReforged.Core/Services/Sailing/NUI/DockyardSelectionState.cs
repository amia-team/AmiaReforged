using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

public enum DockyardMode { Stored, Rent, ActiveRental }
public enum DockyardAction { Retrieve, Rent, RetrieveRental, ReturnRental, Market }

/// <summary>A window-local dropdown value identifies a captured subject, never a fleet enumeration index.</summary>
public sealed record DockyardChoice(int Value, string ShipId, string Name, ShipState? Ship,
    RentalVesselOption? Offer = null, ShipRentalRecord? Agreement = null)
{
    public string? RentalId { get; } = Agreement?.RentalId;
}

public sealed class DockyardSelectionState(string port, string account)
{
    private readonly Dictionary<DockyardMode, DockyardChoice[]> _choices = new();
    private int _nextValue;
    private bool _initialized;
    public string Port { get; } = port;
    public string Account { get; } = account;
    public DockyardMode Mode { get; private set; }
    public DockyardChoice? Selected { get; private set; }
    public int SelectedValue => Selected?.Value ?? -1;
    public bool Closed { get; private set; }
    public IReadOnlyList<DockyardChoice> Choices => _choices.GetValueOrDefault(Mode) ?? [];

    public void Refresh(ShipRegistryService registry, DockyardShipList stored,
        IEnumerable<RentalVesselOption> offers, IEnumerable<ShipRentalRecord> agreements)
    {
        if (Closed) return;
        var previous = Selected;
        // Values are never reused within a token, including after refresh or mode changes.
        // Old watch/click messages therefore cannot address a newly reordered dropdown.
        _choices[DockyardMode.Stored] = stored.Rows.Select(r =>
            new DockyardChoice(checked(++_nextValue), r.ShipId, r.Name, r.Ship)).ToArray();
        _choices[DockyardMode.Rent] = offers.Where(o => o.PortId == Port).Select(o =>
            new DockyardChoice(checked(++_nextValue), o.ShipId, registry.GetShip(o.ShipId)?.ShipName ?? o.ShipName,
                registry.GetShip(o.ShipId), MaritimeMarketEngine.Copy(o))).ToArray();
        _choices[DockyardMode.ActiveRental] = agreements.Where(a => a.PortId == Port && a.RenterAccount == Account && MaritimeMarketEngine.IsOpen(a.Status))
            .OrderBy(a => a.ShipId, StringComparer.Ordinal).ThenBy(a => a.RentalId, StringComparer.Ordinal)
            .Select(a => new DockyardChoice(checked(++_nextValue), a.ShipId, registry.GetShip(a.ShipId)?.ShipName ?? a.ShipId,
                registry.GetShip(a.ShipId), Agreement: a)).ToArray();
        Selected = previous == null ? null : Choices.SingleOrDefault(c => SameSubject(c, previous));
        if (!_initialized) SelectDefault();
        _initialized = true;
    }

    private static bool SameSubject(DockyardChoice current, DockyardChoice prior) =>
        current.ShipId == prior.ShipId && ReferenceEquals(current.Ship, prior.Ship) &&
        current.RentalId == prior.RentalId;

    private void SelectDefault() => Selected = Choices.FirstOrDefault(c => c.Ship != null) ?? Choices.FirstOrDefault();

    public bool ChangeMode(int value)
    {
        if (Closed || !Enum.IsDefined(typeof(DockyardMode), value)) return false;
        var mode = (DockyardMode)value;
        if (Mode == mode) return true;
        Mode = mode; SelectDefault(); return true;
    }

    public bool Select(int value)
    {
        if (Closed) return false;
        if (value == -1) { Selected = null; return true; }
        var choice = Choices.SingleOrDefault(c => c.Value == value);
        if (choice == null) return false;
        Selected = choice; return true;
    }

    public bool TryResolve(ShipRegistryService registry, out ShipState? ship)
    {
        ship = null;
        if (Closed || Selected is not { Ship: { } captured } selected ||
            captured.ShipId != selected.ShipId || !ReferenceEquals(registry.GetShip(selected.ShipId), captured)) return false;
        ship = captured; return true;
    }

    public string EmptyMessage => Mode switch
    {
        DockyardMode.Stored => "No stored communal ships here. Rental cutters are in RENT SHIP mode.",
        DockyardMode.Rent => "No rental offers are configured at this port.",
        _ => "You have no active or pending rental agreements at this port."
    };

    public void Close() { Closed = true; Selected = null; _choices.Clear(); }
}

public sealed record DockyardActionState(bool Visible, bool Enabled, string Reason);

/// <summary>Read-only projection of current backend eligibility for the selected captured subject.</summary>
public sealed class DockyardSelectedVessel
{
    public string[] Lines { get; private init; } = [];
    public Dictionary<DockyardAction, DockyardActionState> Actions { get; } = new();
    public DockyardActionState this[DockyardAction action] => Actions.GetValueOrDefault(action) ?? new(false, false, "");

    public static DockyardSelectedVessel Capture(DockyardSelectionState state, ShipRegistryService registry,
        ShipStorageService storage, Func<ShipState, bool> canRetrieve, bool checkoutEnabled,
        Func<ShipState, bool> resourcesAvailable, Func<ShipState, bool> hasOccupants,
        Func<ShipState, bool> canTrade, bool agreementCurrent = true, bool offerCurrent = true,
        ShipRentalRecord? currentAgreement = null)
    {
        var choice = state.Selected;
        bool canonical = state.TryResolve(registry, out var ship);
        string unavailable = choice == null ? "Select a vessel above." : choice.Ship == null ?
            "Vessel not registered. Await fleet restoration, then REFRESH." : "Selection is no longer available. REFRESH the dockyard.";
        var rental = currentAgreement ?? choice?.Agreement;
        bool stored = canonical && storage.IsStoredAtPort(ship!, state.Port) && !ship!.IsInitializing && !ship.IsDocking && !ship.IsEconomyBusy;
        bool permitted = canonical && canRetrieve(ship!);
        bool communal = canonical && ship!.Rental == null && ship.RentalPoolId == null && !CutterRentalFleet.IsCutter(ship);
        bool retrieval = stored && permitted;
        bool leaseMatches = canonical && agreementCurrent && choice?.Agreement is { } lease &&
            lease.ShipId == ship!.ShipId && lease.RenterAccount == state.Account && choice.RentalId == ship.Rental?.RentalId &&
            ShipAccessService.Owns(ship, state.Account);
        var offer = choice?.Offer is { } slot ? DockyardRentalOffer.Create(slot, canonical ? ship : null,
            checkoutEnabled, canonical && checkoutEnabled && resourcesAvailable(ship!), canonical && hasOccupants(ship!)) : null;
        bool returning = leaseMatches && retrieval && ship!.Cargo.All(c => c.Quantity == 0) && !hasOccupants(ship);
        var agreement = rental is { } record ? DockyardRentalAgreement.Create(record, canonical ? ship : null,
            leaseMatches && retrieval, returning) : null;
        var view = new DockyardSelectedVessel
        {
            Lines =
            [
                choice == null ? "No vessel selected" : $"Ship: {(canonical ? ship!.ShipName : choice.Name)}",
                $"Class: {(canonical ? CutterRentalFleet.IsCutter(ship!) ? "Cutter" : string.IsNullOrWhiteSpace(ship!.SpritePrefix) ? ship.ShipType.ToString() :
                    System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(ship.SpritePrefix) : choice?.Offer?.VesselClass ?? "Unavailable")}",
                canonical ? $"Hull: {ship!.Hull} / {ship.MaximumHull}" : choice?.Offer is { } spec ? $"Hull: unavailable / {spec.MaximumHull}" : "Hull: unavailable",
                canonical ? $"Cargo: {ship!.Cargo.Sum(c => (long)c.Quantity)} aboard / {ship.CargoCapacity} capacity" :
                    choice?.Offer is { } cargo ? $"Cargo: unavailable / {cargo.CargoCapacity} capacity" : "Cargo: unavailable",
                canonical ? $"Weapons: {(!ship!.AllowsWeapons ? "UNARMED" : string.IsNullOrWhiteSpace(ship.WeaponResRef) ?
                    "None equipped / weapons permitted" : "Equipped / weapons permitted")}" : choice?.Offer != null ? "Weapons: UNARMED" : "Weapons: unavailable",
                canonical ? ship!.IsStored ? $"Storage port: {ship.StoredPortId}" : $"Location: {ship.AreaResRef}" : "Location: unavailable",
                $"Availability: {(choice == null ? unavailable : !canonical ? unavailable : state.Mode == DockyardMode.Rent ? offer!.Availability + " / " + offer.Reason :
                    state.Mode == DockyardMode.ActiveRental ? agreementCurrent && leaseMatches ? agreement!.State : "Agreement unavailable; refresh required" :
                    !communal ? "No longer a communal vessel" : retrieval ? "STORED / AVAILABLE FOR RETRIEVAL" : "Stored / busy or access unavailable")}",
                offer != null ? $"{offer.Fee} / {offer.Deposit} / {offer.Damage}" : agreement != null ?
                    $"{agreement.Fee} / {agreement.Deposit} / Refund owed: {rental!.GoldOwed} gp" : "Rental terms: not applicable to communal retrieval",
                agreement != null ? $"Agreement: {agreement.State}" : offer != null ? "Agreement: new voyage checkout" : "Ownership: COMMUNAL",
                agreement != null ? $"{agreement.ReturnStatus}: {agreement.ReturnReason} {agreement.ReturnDetail}" : "Return: use ACTIVE RENTAL for an existing agreement"
            ]
        };
        // Empty modes must not imply communal ownership or show another mode's rental terms.
        if (choice == null) Array.Clear(view.Lines, 1, view.Lines.Length - 1);
        void Add(DockyardAction action, bool visible, bool enabled, string reason) => view.Actions[action] =
            new(visible, enabled, enabled ? "" : choice == null || !canonical ? unavailable : reason);
        Add(DockyardAction.Retrieve, state.Mode == DockyardMode.Stored, communal && retrieval, "Ship must be available, stored here and authorized.");
        Add(DockyardAction.Rent, state.Mode == DockyardMode.Rent, canonical && offerCurrent && offer is { CanRent: true },
            !offerCurrent ? "Rental configuration changed. REFRESH before checkout." : offer == null ? unavailable : $"{offer.Availability}: {offer.Reason} {offer.Detail}");
        Add(DockyardAction.RetrieveRental, state.Mode == DockyardMode.ActiveRental, leaseMatches && retrieval,
            "Your current rental must be available, stored here and authorized.");
        Add(DockyardAction.ReturnRental, state.Mode == DockyardMode.ActiveRental, returning,
            agreementCurrent && leaseMatches ? $"{agreement!.ReturnReason} {agreement.ReturnDetail}" : "This rental agreement is no longer current or authorized.");
        Add(DockyardAction.Market, state.Mode != DockyardMode.Rent, retrieval && (communal || leaseMatches) && !ship!.IsDisabled && canTrade(ship),
            "Cargo requires an available stored ship, authorized access and a port market.");
        return view;
    }

    public static string ActionId(DockyardAction action, int value) => action switch
    {
        DockyardAction.Retrieve => "dockyard_retrieve", DockyardAction.Rent => "dockyard_rent",
        DockyardAction.RetrieveRental => "dockyard_lease_retrieve", DockyardAction.ReturnRental => "dockyard_return",
        _ => "dockyard_market"
    } + ":" + value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public static bool TryAction(string id, DockyardSelectionState state, out DockyardAction action)
    {
        action = default;
        if (state.Closed || state.Selected == null) return false;
        foreach (DockyardAction candidate in Enum.GetValues<DockyardAction>())
            if (id == ActionId(candidate, state.SelectedValue)) { action = candidate; return true; }
        return false;
    }
}
