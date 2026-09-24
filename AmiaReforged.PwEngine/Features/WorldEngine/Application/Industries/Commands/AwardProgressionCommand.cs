using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to award progression points to a character.
/// 
/// Progression points accumulate toward the economy-knowledge-point threshold and
/// roll over into economy KP once the cost curve is met. Earning is subject to a
/// soft cap (tedium multiplier) and a hard cap (blocked). This command provides an
/// independent dispatch boundary so any activity that grants progression points
/// (crafting, harvesting, Codex rewards, ...) routes through the command dispatcher
/// and gets the generic <c>CommandExecutedEvent</c> and logging for free.
/// </summary>
public record AwardProgressionCommand : ICommand
{
    /// <summary>
    /// Character receiving the progression points.
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Number of progression points to award. Must be greater than zero.
    /// </summary>
    public int Points { get; init; }
}

/// <summary>
/// Handles awarding progression points through the command dispatcher.
/// 
/// Delegates the accumulation, rollover, curve and cap logic to
/// <see cref="IKnowledgeProgressionService"/> and returns the resulting
/// <c>ProgressionResult</c> as command data so callers can surface KP totals to the
/// client. On success the dispatcher publishes the generic
/// <c>CommandExecutedEvent&lt;AwardProgressionCommand&gt;</c>.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<AwardProgressionCommand>))]
public class AwardProgressionHandler : ICommandHandler<AwardProgressionCommand>
{
    private readonly IKnowledgeProgressionService _progressionService;
    private readonly ICommandDispatcher _commandDispatcher;

    public AwardProgressionHandler(
        IKnowledgeProgressionService progressionService,
        ICommandDispatcher commandDispatcher)
    {
        _progressionService = progressionService;
        _commandDispatcher = commandDispatcher;
    }

    public async Task<CommandResult> HandleAsync(AwardProgressionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Points <= 0)
        {
            return CommandResult.Fail("Cannot award zero or negative progression points.");
        }

        ProgressionResult progressionResult =
            _progressionService.AwardProgressionPoints(command.CharacterId, command.Points);

        Dictionary<string, object> data = new()
        {
            ["success"] = progressionResult.Success,
            ["knowledgePointsEarned"] = progressionResult.KnowledgePointsEarned,
            ["newTotalKnowledgePoints"] = progressionResult.NewTotalKnowledgePoints,
            ["newEconomyKnowledgePointTotal"] = progressionResult.NewEconomyKnowledgePointTotal,
            ["progressionPointsRemaining"] = progressionResult.ProgressionPointsRemaining,
            ["progressionPointsRequired"] = progressionResult.ProgressionPointsRequired,
            ["isAtSoftCap"] = progressionResult.IsAtSoftCap,
            ["isAtHardCap"] = progressionResult.IsAtHardCap,
            ["message"] = progressionResult.Message ?? string.Empty
        };

        return progressionResult.Success
            ? CommandResult.OkWithData(data)
            : CommandResult.Fail(progressionResult.Message ?? string.Empty, data);
    }
}
