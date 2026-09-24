using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to grant a single level-up knowledge point to a character.
/// 
/// Level-up KP are free bonuses granted directly per character level (1-30). Unlike
/// economy-earned KP they do NOT flow through the progression-point cost curve, so this
/// command only increments the level-up counter and leaves the economy curve untouched.
/// The grant is capped at 30 to bound the free bonus; a character already at the cap is
/// rejected without mutation.
/// </summary>
public sealed record GrantLevelUpKnowledgePointCommand(
    CharacterId CharacterId) : ICommand;

/// <summary>
/// Handles <see cref="GrantLevelUpKnowledgePointCommand"/> through the command dispatcher.
/// 
/// Increments the character's level-up knowledge-point counter (capped at 30) and persists
/// the change. On success the dispatcher publishes the generic
/// <c>CommandExecutedEvent&lt;GrantLevelUpKnowledgePointCommand&gt;</c>. The economy
/// progression curve and counters are intentionally not touched — level-up KP are a
/// deliberate bypass of the economy curve.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<GrantLevelUpKnowledgePointCommand>))]
public class GrantLevelUpKnowledgePointHandler : ICommandHandler<GrantLevelUpKnowledgePointCommand>
{
    private const int MaxLevelUpKnowledgePoints = 30;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IKnowledgeProgressionRepository _progressionRepository;

    public GrantLevelUpKnowledgePointHandler(
        IKnowledgeProgressionRepository progressionRepository)
    {
        _progressionRepository = progressionRepository;
    }

    public async Task<CommandResult> HandleAsync(GrantLevelUpKnowledgePointCommand command,
        CancellationToken cancellationToken = default)
    {
        KnowledgeProgression progression = _progressionRepository.GetOrCreate(command.CharacterId.Value);

        if (progression.LevelUpKnowledgePoints >= MaxLevelUpKnowledgePoints)
        {
            Log.Warn($"Character {command.CharacterId.Value} already has max level-up KP ({MaxLevelUpKnowledgePoints}). Ignoring grant.");
            return CommandResult.Fail(
                $"Character {command.CharacterId.Value} already has the maximum level-up knowledge points.");
        }

        progression.LevelUpKnowledgePoints++;
        _progressionRepository.Update(progression);

        Log.Info($"Character {command.CharacterId.Value} granted level-up KP. " +
                 $"Total level-up KP: {progression.LevelUpKnowledgePoints}, " +
                 $"Total KP: {progression.TotalKnowledgePoints}");

        // Level-up KP are a deliberate bypass of the economy curve: the economy KP
        // total and accumulated progression points are intentionally not modified.
        return CommandResult.OkWithData(new Dictionary<string, object>
        {
            ["levelUpKnowledgePoints"] = progression.LevelUpKnowledgePoints,
            ["totalKnowledgePoints"] = progression.TotalKnowledgePoints,
            ["economyKnowledgePoints"] = progression.EconomyEarnedKnowledgePoints,
            ["accumulatedProgressionPoints"] = progression.AccumulatedProgressionPoints
        });
    }
}
