using System.Globalization;
using AmiaReforged.Core.Models.Sailing;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>An immutable display/identity snapshot for one dockyard window token.</summary>
public sealed record DockyardShipRow(ShipState Ship, string ShipId, string Name, string Vessel,
    string Status, bool CanRetrieve, bool CanTrade);

public sealed class DockyardShipList
{
    public const string AvailableStatus = "STORED / AVAILABLE FOR RETRIEVAL";
    public IReadOnlyList<DockyardShipRow> Rows { get; }
    public int AvailableCount => Rows.Count(r => r.CanRetrieve);
    public string Summary => Rows.Count == 0
        ? "No stored ships are available for retrieval here. Rental offers are listed separately."
        : $"{AvailableCount} available for retrieval; {Rows.Count - AvailableCount} unavailable. Refresh to see newly docked ships.";

    private DockyardShipList(DockyardShipRow[] rows) => Rows = Array.AsReadOnly(rows);

    public static DockyardShipList Create(ShipRegistryService registry, ShipStorageService storage, string port,
        Func<ShipState, bool> canRetrieve)
    {
        // An unleased pool vessel is a rental offer, never a communal retrieval option.
        var rows = storage.GetStoredShips(registry.GetShips(), port)
            .Where(s => s.RentalPoolId == null || s.Rental != null)
            .OrderBy(s => s.ShipName, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.ShipId, StringComparer.Ordinal)
            .Select(s =>
            {
                bool permitted = canRetrieve(s), available = permitted && !s.IsEconomyBusy;
                string category = s.Rental == null ? "Communal" : permitted ? "Your rental" : "Other rental";
                string vessel = CutterRentalFleet.IsCutter(s) ? CutterRentalFleet.DefinitionId : string.IsNullOrWhiteSpace(s.SpritePrefix) ? s.ShipType.ToString() :
                    CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.SpritePrefix);
                string status = !permitted ? "STORED / UNAVAILABLE TO YOU" : s.IsEconomyBusy ? "STORED / BUSY" : AvailableStatus;
                return new DockyardShipRow(s, s.ShipId, s.ShipName, $"{vessel} / {category}", status,
                    available, available && !s.IsDisabled);
            }).ToArray();
        return new(rows);
    }

    public bool TryResolve(int index, ShipRegistryService registry, ShipStorageService storage, string port,
        Func<ShipState, bool> canRetrieve, out ShipState? ship)
    {
        ship = null;
        if (index < 0 || index >= Rows.Count) return false;
        DockyardShipRow row = Rows[index];
        // Resolve the rendered snapshot, never an index into a new fleet enumeration.
        if (!row.CanRetrieve || row.ShipId != row.Ship.ShipId || !registry.IsCurrent(row.Ship) ||
            !storage.IsStoredAtPort(row.Ship, port) || row.Ship.IsInitializing || row.Ship.IsDocking ||
            row.Ship.IsEconomyBusy || !canRetrieve(row.Ship)) return false;
        ship = row.Ship;
        return true;
    }
}
