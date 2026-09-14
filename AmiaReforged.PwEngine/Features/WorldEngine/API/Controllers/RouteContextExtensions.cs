using AmiaReforged.PwEngine.Features.WorldEngine.API;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// Shared helpers for controllers: facade resolution and result mapping.
/// Controllers must go through <see cref="IWorldEngineFacade"/> (which dispatches
/// via the central command/query dispatchers) instead of repositories or handlers.
/// </summary>
public static class RouteContextExtensions
{
    /// <summary>
    /// Resolves the WorldEngine facade, preferring the request's service provider
    /// (set in tests and, when wired, by the HTTP server) and falling back to
    /// Anvil's service locator (the existing controller convention in production).
    /// Returns null when no facade is available; callers should return
    /// <see cref="FacadeUnavailable"/> (503) in that case.
    /// </summary>
    public static IWorldEngineFacade? ResolveFacade(this RouteContext ctx)
    {
        try
        {
            IWorldEngineFacade? fromServices =
                ctx.Services?.GetService(typeof(IWorldEngineFacade)) as IWorldEngineFacade;
            if (fromServices is not null) return fromServices;

            return AnvilCore.GetService<IWorldEngineFacade>();
        }
        catch
        {
            return null;
        }
    }

    public static ApiResult FacadeUnavailable() => new(503,
        new ErrorResponse("Service unavailable", "WorldEngine facade is not available"));
}

/// <summary>
/// Maps <see cref="CommandResult"/> to <see cref="ApiResult"/> with a uniform shape.
/// </summary>
public static class CommandResultMapping
{
    public static ApiResult ToApiResult(
        this CommandResult result,
        int successStatus = 200,
        object? successData = null,
        int failureStatus = 400)
    {
        if (result.Success)
            return new ApiResult(successStatus, successData ?? new { message = "OK" });

        return new ApiResult(failureStatus,
            new ErrorResponse("Command failed", result.ErrorMessage ?? "Unknown error"));
    }
}
