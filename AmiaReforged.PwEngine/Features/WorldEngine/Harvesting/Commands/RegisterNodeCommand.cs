using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Commands;

/// <summary>
/// Command to register a new resource node instance
/// </summary>
public sealed record RegisterNodeCommand(
    Guid? NodeInstanceId,
    string ResourceTag,
    string AreaResRef,
    float X,
    float Y,
    float Z,
    float Rotation,
    IPQuality Quality,
    int Uses) : ICommand;

