using System.Numerics;
using AmiaReforged.Core.Models.Sailing;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(HorizonContactService))]
public sealed class HorizonContactService
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

   
    private const string MerchantAreaResRef =
    "ocean_01";

private const float MerchantX =
    120.0f;

private const float MerchantY =
    100.0f;
private readonly OceanContactService _oceanContactService;

    public sealed class SailingEncounter
{
    public string Id { get; init; } = "";
    public EncounterType Type { get; init; }

    public string AreaResRef { get; init; } = "";
    public float X { get; set; }
    public float Y { get; set; }

    public bool Visible { get; set; } = true;
}  

   public HorizonContactService(
    OceanContactService oceanContactService)
{
    _oceanContactService =
        oceanContactService;

    Log.Info(
        "Horizon Contact Service initialized.");
}

    public void UpdateContacts(
    ShipState ship)
{
OceanContact? pirate =
    _oceanContactService
        .GetContacts(ship.AreaResRef)
        .FirstOrDefault(c =>
            c.Type == EncounterType.Pirate);

if (pirate == null)
{
    return;
}


        float x =
    GetHorizonX(
        ship,
        pirate.AreaResRef,
        pirate.X,
        pirate.Y);

float distance =
    GetDistance(
        ship.X,
        ship.Y,
        pirate.X,
        pirate.Y);

Log.Info(
    $"Horizon update: " +
    $"Ship={ship.ShipName}, " +
    $"Heading={ship.Heading}, " +
    $"Pirate={pirate.Id}, " +
    $"Distance={distance:0.0}, " +
    $"X={x:0.0}");
  
}

   private static float GetHorizonX(
    ShipState ship,
    string targetArea,
    float targetX,
    float targetY)
{
    if (!string.Equals(
            ship.AreaResRef,
            targetArea,
            StringComparison.OrdinalIgnoreCase))
    {
        return -100f;
    }

    float deltaX =
        targetX - ship.X;

    float deltaY =
        targetY - ship.Y;

    float worldAngle =
        MathF.Atan2(
            deltaY,
            deltaX);

    float shipAngle =
        ship.Heading switch
        {
            Heading.East => 0f,
            Heading.NorthEast => MathF.PI / 4f,
            Heading.North => MathF.PI / 2f,
            Heading.NorthWest => 3f * MathF.PI / 4f,
            Heading.West => MathF.PI,
            Heading.SouthWest => 5f * MathF.PI / 4f,
            Heading.South => 3f * MathF.PI / 2f,
            Heading.SouthEast => 7f * MathF.PI / 4f,
            _ => 0f,
        };

    float relative =
        worldAngle - shipAngle;

    while (relative > MathF.PI)
    {
        relative -=
            2f * MathF.PI;
    }

    while (relative < -MathF.PI)
    {
        relative +=
            2f * MathF.PI;
    }

    relative =
        Math.Clamp(
            relative,
            -MathF.PI / 2f,
            MathF.PI / 2f);

    return 10f +
        (relative / (MathF.PI / 2f)) * 8f;
}
private static float GetDistance(
    float x1,
    float y1,
    float x2,
    float y2)
{
    float deltaX =
        x2 - x1;

    float deltaY =
        y2 - y1;

    return MathF.Sqrt(
        deltaX * deltaX +
        deltaY * deltaY);
}
public string BuildHorizonString(
    ShipState ship)
{
    if (!string.Equals(
            ship.AreaResRef,
            MerchantAreaResRef,
            StringComparison.OrdinalIgnoreCase))
    {
        return "No contacts";
    }

    float x =
        GetHorizonX(
            ship,
            MerchantAreaResRef,
            MerchantX,
            MerchantY);

    int slot =
        Math.Clamp(
            (int)Math.Round((x - 2f) / 2f),
            0,
            8);

    char[] strip =
        "─────────".ToCharArray();

    strip[slot] = '▲';

    return $"Port {new string(strip)} Starboard";
}

}