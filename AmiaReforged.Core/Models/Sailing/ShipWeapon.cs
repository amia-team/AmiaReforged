namespace AmiaReforged.Core.Models.Sailing;

public class ShipWeapon
{
    public string ResRef { get; init; } =
        string.Empty;

    public string DisplayName { get; init; } =
        string.Empty;

    public int Damage { get; init; }

    public TimeSpan Cooldown { get; init; }

    public float MaxRange { get; init; }

    public WeaponArc Arc { get; init; } =
        WeaponArc.Broadside;
}