namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// Example controller showing parameterized routes and different HTTP methods.
/// This demonstrates the routing capabilities.
/// </summary>
public class ExampleBankingController
{
    /// <summary>
    /// Get treasury balance by ID
    /// GET /api/worldengine/treasuries/{id}/balance
    /// </summary>
    [HttpGet("/api/worldengine/treasuries/{id}/balance")]
    public static async Task<ApiResult> GetTreasuryBalance(RouteContext ctx)
    {
        var treasuryId = ctx.GetRouteValue("id");

        // TODO: Integrate with actual CQRS/MediatR infrastructure
        // var query = new GetTreasuryBalanceQuery(new TreasuryId(Guid.Parse(treasuryId)));
        // var balance = await mediator.Send(query);

        // Mock response for now
        return await Task.FromResult(new ApiResult(200, new
        {
            treasuryId,
            goldAmount = 1000,
            silverAmount = 500,
            lastUpdated = DateTime.UtcNow
        }));
    }

    /// <summary>
    /// Apply interest to a treasury
    /// POST /api/worldengine/banking/apply-interest
    /// Body: { "treasuryId": "...", "interestAmount": 50 }
    /// </summary>
    [HttpPost("/api/worldengine/banking/apply-interest")]
    public static async Task<ApiResult> ApplyInterest(RouteContext ctx)
    {
        var request = await ctx.ReadJsonBodyAsync<ApplyInterestRequest>();

        if (request == null || string.IsNullOrEmpty(request.TreasuryId))
        {
            return new ApiResult(400, new ErrorResponse(
                "Bad request",
                "Missing required fields: treasuryId, interestAmount"));
        }

        // TODO: Integrate with CQRS
        // var command = new ApplyInterestCommand(
        //     new TreasuryId(Guid.Parse(request.TreasuryId)),
        //     new GoldAmount(request.InterestAmount));
        // await mediator.Send(command);

        var correlationId = Guid.NewGuid();

        return new ApiResult(202, new
        {
            message = "Interest application accepted",
            correlationId,
            treasuryId = request.TreasuryId,
            amount = request.InterestAmount
        });
    }

    /// <summary>
    /// Transfer gold between treasuries
    /// POST /api/worldengine/banking/transfer
    /// </summary>
    [HttpPost("/api/worldengine/banking/transfer")]
    public static async Task<ApiResult> TransferGold(RouteContext ctx)
    {
        var request = await ctx.ReadJsonBodyAsync<TransferGoldRequest>();

        if (request == null ||
            string.IsNullOrEmpty(request.FromTreasuryId) ||
            string.IsNullOrEmpty(request.ToTreasuryId))
        {
            return new ApiResult(400, new ErrorResponse(
                "Bad request",
                "Missing required fields: fromTreasuryId, toTreasuryId, amount"));
        }

        // TODO: Integrate with CQRS
        // var command = new TransferGoldCommand(...);
        // await mediator.Send(command);

        var correlationId = Guid.NewGuid();

        return new ApiResult(202, new
        {
            message = "Transfer accepted",
            correlationId,
            from = request.FromTreasuryId,
            to = request.ToTreasuryId,
            amount = request.Amount
        });
    }

    // DTOs for this controller
    private record ApplyInterestRequest(string TreasuryId, int InterestAmount);
    private record TransferGoldRequest(string FromTreasuryId, string ToTreasuryId, int Amount);
}

