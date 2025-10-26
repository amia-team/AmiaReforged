namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

public record OperationResult(bool IsSuccess, string? ErrorMessage)
{
    public static OperationResult Success() => new(true, null);
    public static OperationResult Failure(string error) => new(false, error);
}