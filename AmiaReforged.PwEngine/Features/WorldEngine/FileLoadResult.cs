namespace AmiaReforged.PwEngine.Features.WorldEngine;

public record FileLoadResult(ResultType Type, string? Message = null, string? FileName = null);
