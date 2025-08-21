namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class PreconditionResult
{
    public bool Satisfied { get; }
    public string? ReasonCode { get; }
    public string? Message { get; }

    private PreconditionResult(bool satisfied, string? code, string? message)
    {
        Satisfied = satisfied;
        ReasonCode = code;
        Message = message;
    }

    public static PreconditionResult Ok() => new(true, null, null);
    public static PreconditionResult Fail(string code, string message) => new(false, code, message);
}