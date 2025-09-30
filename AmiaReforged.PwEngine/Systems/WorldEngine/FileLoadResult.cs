namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public record FileLoadResult(ResultType Type, string? Message = null, string? FileName = null);