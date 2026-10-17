namespace AmiaReforged.Core.Models.Sailing;

public class ShipBoardingRequest
{
    /// <summary>
    /// The ship requesting to board.
    /// </summary>
    public required ShipState RequestingShip { get; set; }

    /// <summary>
    /// The ship being requested for boarding.
    /// </summary>
    public required ShipState TargetShip { get; set; }

    /// <summary>
    /// The player requesting boarding.
    /// </summary>
    public required string RequestingPlayerName { get; set; }

    /// <summary>
    /// The helmsman who must accept or reject the request.
    /// </summary>
    public required string TargetPlayerName { get; set; }

    /// <summary>
    /// The time the request was created.
    /// </summary>
    public DateTime RequestedAt { get; set; }
}