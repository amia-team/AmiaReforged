namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

public record FileLoadResult(ResultType Type, string? Message = null, string? FileName = null, Exception? Exception = null);
