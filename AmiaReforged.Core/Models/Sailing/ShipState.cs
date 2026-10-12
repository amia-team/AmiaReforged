namespace AmiaReforged.Core.Models.Sailing;

public class ShipState
{
    // Definitions supply stable IDs. Unregistered runtime instances have distinct IDs.
    public string ShipId { get; init; } = Guid.NewGuid().ToString("N");
    public string? DefinitionId { get; set; }
    // Definition-owned capability; persistence and equipment changes cannot arm an unarmed fleet slot.
    public bool AllowsWeapons { get; init; } = true;
    public string? RepresentationTag { get; set; }
    public string? RepresentationResRef { get; set; }
    public bool IsInitializing { get; set; }
    public bool IsEconomyBusy { get; set; }
    public long RentalStorageRevision { get; set; }
    public string? RentalPoolId { get; set; }
    public ShipRentalRecord? Rental { get; set; }
    public long PendingMerchantArrivalSequence { get; set; }
    public long MerchantArrivalSequence { get; set; }
    public bool IsActiveWorld => !IsStored && !IsDocking && !IsInitializing;
    public bool IsDisabled => Hull == 0;

    private int _maximumHull = 100;
    private int _hull = 100;
    public int MaximumHull
    {
        get => _maximumHull;
        set
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            _maximumHull = value;
            Hull = _hull;
        }
    }
    /// <summary>
    /// The display name of the ship.
    /// </summary>
    public required string ShipName { get; set; }

    public required string DeckAreaResRef { get; set; }
    public string? CabinAreaResRef { get; init; }

    /// <summary>
    /// The PC key of the character currently at the helm.
    /// </summary>
    public string? HelmsmanPCKey { get; set; }

    /// <summary>
    /// The NWN area ResRef representing the ship's
    /// current location on the sailing map.
    /// </summary>
    public required string AreaResRef { get; set; }

    public ShipType ShipType { get; set; } = ShipType.Player;

    public string SpritePrefix { get; set; } = "sloop";
    public List<MerchantCargo> Cargo { get; set; } =
        new();
    /// <summary>
    /// Gold currently available to the merchant for purchases.
    /// </summary>
    public int MerchantGold { get; set; } = 1000;
    /// <summary>
    /// Goods this merchant is willing to trade.
    /// </summary>
    public List<string> TradeGoods { get; set; } =
        new();
    public int CargoCapacity { get; set; } = 50;
    public bool CanDock { get; set; }

    public string? NearbyIslandId { get; set; }

    /// <summary>
    /// The ship's X position on the abstract sailing map.
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// The ship's Y position on the abstract sailing map.
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// The ship's Z position on the abstract sailing map.
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// The direction the ship is currently facing.
    /// </summary>
    public Heading Heading { get; set; } = Heading.East;

    /// <summary>
    /// Indicates whether the ship is currently underway.
    /// </summary>
    public bool Underway { get; set; }

    /// <summary>
    /// The NWN area ResRef where the ship is navigating to.
    /// Null when no destination has been assigned.
    /// </summary>
    public string? DestinationAreaResRef { get; set; }

    /// <summary>
    /// The destination X coordinate on the abstract sailing map.
    /// </summary>
    public float DestinationX { get; set; }

    /// <summary>
    /// The destination Y coordinate on the abstract sailing map.
    /// </summary>
    public float DestinationY { get; set; }

    /// <summary>
    /// The destination Z coordinate on the abstract sailing map.
    /// </summary>
    public float DestinationZ { get; set; }

    /// <summary>
    /// Indicates whether the ship currently has an active
    /// navigation course.
    /// </summary>
    public bool IsNavigating { get; set; }

    /// <summary>
    /// The ship's current hull integrity.
    /// </summary>
    public int Hull
    {
        get => _hull;
        set
        {
            _hull = Math.Clamp(value, 0, MaximumHull);
            if (_hull == 0) Stop();
        }
    }

    public void Stop()
    {
        Underway = false;
        IsNavigating = false;
        DestinationAreaResRef = null;
    }

    public int ApplyHullDamage(int amount)
    {
        int damage = Math.Min(Math.Max(amount, 0), Hull);
        Hull -= damage;
        return damage;
    }

    public int RepairHull(int amount)
    {
        int repaired = (int)Math.Min(Math.Max((long)amount, 0), MaximumHull - Hull);
        Hull += repaired;
        return repaired;
    }



    /// <summary>
    /// The ResRef of the weapon currently equipped
    /// on the ship.
    /// </summary>
    public string WeaponResRef { get; set; } =
        "ship_cannon";

    /// <summary>
    /// The port where this ship is currently stored.
    /// Null when the ship is not stored at a port.
    /// </summary>
    public string? StoredPortId { get; set; }

    /// <summary>
    /// Indicates that the ship has been stored at a port
    /// and is no longer active on the sailing map.
    /// </summary>
    public bool IsStored { get; set; }
    /// <summary>
    /// The port the merchant is currently operating from.
    /// Null while the merchant is sailing between ports.
    /// </summary>
    public string? CurrentTradePortId { get; set; }

    /// <summary>
    /// True while the ship is performing a dock/undock transition.
    /// Prevents duplicate NUI and navigation updates during area changes.
    /// </summary>
    public bool IsDocking { get; set; }
    /// <summary>
    /// Indicates whether a merchant ship is currently stopped at a port.
    /// </summary>
    public bool IsInPort { get; set; }

    /// <summary>
    /// The time at which the merchant should depart its current port.
    /// </summary>
    public DateTime PortDepartureTime { get; set; }
}
