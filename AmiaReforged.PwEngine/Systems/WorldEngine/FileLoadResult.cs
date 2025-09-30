namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

public record FileLoadResult(ResultType Type, string? Message = null, string? FileName = null);