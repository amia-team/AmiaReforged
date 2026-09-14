using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Global knowledge-progression curve configuration.
/// </summary>
public record ProgressionConfig
{
    public int BaseCost { get; init; } = 100;
    public float ScalingFactor { get; init; } = 1.15f;
    public string CurveType { get; init; } = "Exponential";
    public int SoftCap { get; init; } = 100;
    public int HardCap { get; init; } = 150;
    public float SoftCapPenaltyMultiplier { get; init; } = 3.0f;
}

/// <summary>
/// Updates the global progression curve configuration.
/// </summary>
public record UpdateProgressionConfigCommand : ICommand
{
    public required ProgressionConfig Config { get; init; }
}

/// <summary>
/// Reads the global progression curve configuration.
/// </summary>
public record GetProgressionConfigQuery : IQuery<ProgressionConfig>;

[ServiceBinding(typeof(ICommandHandler<UpdateProgressionConfigCommand>))]
public sealed class UpdateProgressionConfigHandler : ICommandHandler<UpdateProgressionConfigCommand>
{
    private readonly IWorldConfigProvider _config;

    public UpdateProgressionConfigHandler(IWorldConfigProvider config)
    {
        _config = config;
    }

    public Task<CommandResult> HandleAsync(UpdateProgressionConfigCommand command, CancellationToken cancellationToken = default)
    {
        ProgressionConfig cfg = command.Config;
        _config.SetInt(WorldConstants.KnowledgeProgressionBaseCost, cfg.BaseCost);
        _config.SetFloat(WorldConstants.KnowledgeProgressionScalingFactor, cfg.ScalingFactor);
        _config.SetString(WorldConstants.KnowledgeProgressionCurveType, cfg.CurveType);
        _config.SetInt(WorldConstants.KnowledgePointDefaultSoftCap, cfg.SoftCap);
        _config.SetInt(WorldConstants.KnowledgePointDefaultHardCap, cfg.HardCap);
        _config.SetFloat(WorldConstants.KnowledgeSoftCapPenaltyMultiplier, cfg.SoftCapPenaltyMultiplier);
        return Task.FromResult(CommandResult.Ok());
    }
}

[ServiceBinding(typeof(IQueryHandler<GetProgressionConfigQuery, ProgressionConfig>))]
public sealed class GetProgressionConfigHandler : IQueryHandler<GetProgressionConfigQuery, ProgressionConfig>
{
    private readonly IWorldConfigProvider _config;

    public GetProgressionConfigHandler(IWorldConfigProvider config)
    {
        _config = config;
    }

    public Task<ProgressionConfig> HandleAsync(GetProgressionConfigQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProgressionConfig
        {
            BaseCost = _config.GetInt(WorldConstants.KnowledgeProgressionBaseCost) ?? 100,
            ScalingFactor = _config.GetFloat(WorldConstants.KnowledgeProgressionScalingFactor) ?? 1.15f,
            CurveType = _config.GetString(WorldConstants.KnowledgeProgressionCurveType) ?? "Exponential",
            SoftCap = _config.GetInt(WorldConstants.KnowledgePointDefaultSoftCap) ?? 100,
            HardCap = _config.GetInt(WorldConstants.KnowledgePointDefaultHardCap) ?? 150,
            SoftCapPenaltyMultiplier = _config.GetFloat(WorldConstants.KnowledgeSoftCapPenaltyMultiplier) ?? 3.0f
        });
    }
}
