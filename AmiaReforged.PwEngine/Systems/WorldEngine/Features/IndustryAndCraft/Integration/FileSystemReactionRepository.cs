using System.Text.Json;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration.JsonModels;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration;

public sealed class FileSystemReactionRepository : IReactionRepository
{
    private readonly string _gameResourcesDirectory;
    private readonly IReactionPreconditionFactory _preconditionFactory;
    private readonly IReactionModifierFactory _modifierFactory;
    private readonly ILogger<FileSystemReactionRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    private IReadOnlyList<ReactionDefinition>? _cachedReactions;
    private DateTime _lastLoadTime = DateTime.MinValue;

    public FileSystemReactionRepository(
        IReactionPreconditionFactory preconditionFactory,
        IReactionModifierFactory modifierFactory,
        ILogger<FileSystemReactionRepository> logger)
    {
        _gameResourcesDirectory = Environment.GetEnvironmentVariable("GAME_RESOURCES_DIRECTORY")
            ?? throw new InvalidOperationException("GAME_RESOURCES_DIRECTORY environment variable is not set");

        _preconditionFactory = preconditionFactory ?? throw new ArgumentNullException(nameof(preconditionFactory));
        _modifierFactory = modifierFactory ?? throw new ArgumentNullException(nameof(modifierFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        ValidateGameResourcesDirectory();
    }

    public async Task<IReadOnlyList<ReactionDefinition>> LoadAllReactionsAsync(CancellationToken cancellationToken = default)
    {
        await _loadSemaphore.WaitAsync(cancellationToken);
        try
        {
            string reactionsPath = Path.Combine(_gameResourcesDirectory, "reactions");

            if (!Directory.Exists(reactionsPath))
            {
                _logger.LogWarning("Reactions directory not found at {Path}", reactionsPath);
                return Array.Empty<ReactionDefinition>();
            }

            // Check if we need to reload
            DateTime lastWriteTime = Directory.GetLastWriteTimeUtc(reactionsPath);
            if (_cachedReactions != null && lastWriteTime <= _lastLoadTime)
            {
                _logger.LogDebug("Using cached reactions");
                return _cachedReactions;
            }

            _logger.LogInformation("Loading reactions from {Path}", reactionsPath);
            List<ReactionDefinition> reactions = new List<ReactionDefinition>();

            string[] jsonFiles = Directory.GetFiles(reactionsPath, "*.json", SearchOption.AllDirectories);

            foreach (string file in jsonFiles)
            {
                try
                {
                    IEnumerable<ReactionDefinition> reactionDefinitions = await LoadReactionsFromFileAsync(file, cancellationToken);
                    reactions.AddRange(reactionDefinitions);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load reactions from file {File}", file);
                    // Continue processing other files
                }
            }

            _cachedReactions = reactions.AsReadOnly();
            _lastLoadTime = DateTime.UtcNow;

            _logger.LogInformation("Loaded {Count} reactions from {FileCount} files", reactions.Count, jsonFiles.Length);
            return _cachedReactions;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public async Task<ReactionDefinition?> GetReactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ReactionDefinition> reactions = await LoadAllReactionsAsync(cancellationToken);
        return reactions.FirstOrDefault(r => r.Id == id);
    }

    public async Task<IReadOnlyList<ReactionDefinition>> GetReactionsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Array.Empty<ReactionDefinition>();

        IReadOnlyList<ReactionDefinition> reactions = await LoadAllReactionsAsync(cancellationToken);
        return reactions.Where(r => r.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList().AsReadOnly();
    }

    private async Task<IEnumerable<ReactionDefinition>> LoadReactionsFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading reactions from {File}", filePath);

        string jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);

        // Try to parse as single reaction first, then as array
        try
        {
            ReactionJson? singleReaction = JsonSerializer.Deserialize<ReactionJson>(jsonContent, _jsonOptions);
            if (singleReaction != null)
            {
                return new[] { ConvertToReactionDefinition(singleReaction) };
            }
        }
        catch (JsonException)
        {
            // Not a single reaction, try as array
        }

        ReactionJson[]? reactionArray = JsonSerializer.Deserialize<ReactionJson[]>(jsonContent, _jsonOptions);
        if (reactionArray == null)
        {
            _logger.LogWarning("Could not parse reactions from {File}", filePath);
            return Enumerable.Empty<ReactionDefinition>();
        }

        return reactionArray.Select(ConvertToReactionDefinition);
    }

    private ReactionDefinition ConvertToReactionDefinition(ReactionJson json)
    {
        Guid id = Guid.Parse(json.Id);
        IEnumerable<Quantity> inputs = json.Inputs.Select(i => new Quantity(ItemTag.From(i.Item), i.Amount));
        IEnumerable<Quantity> outputs = json.Outputs.Select(o => new Quantity(ItemTag.From(o.Item), o.Amount));
        TimeSpan baseDuration = TimeSpan.Parse(json.BaseDuration);

        IEnumerable<IReactionPrecondition> preconditions = json.Preconditions?
            .Select(p => _preconditionFactory.Create(p.Type, p.Parameters ?? new Dictionary<string, object>()))
            .Where(p => p != null)
            .Cast<IReactionPrecondition>() ?? Enumerable.Empty<IReactionPrecondition>();

        IEnumerable<IReactionModifier> modifiers = json.Modifiers?
            .Select(m => _modifierFactory.Create(m.Type, m.Parameters ?? new Dictionary<string, object>()))
            .Where(m => m != null)
            .Cast<IReactionModifier>() ?? Enumerable.Empty<IReactionModifier>();

        return new ReactionDefinition(
            id,
            json.Name,
            inputs,
            outputs,
            baseDuration,
            json.BaseSuccessChance,
            preconditions,
            modifiers
        );
    }

    private void ValidateGameResourcesDirectory()
    {
        if (!Directory.Exists(_gameResourcesDirectory))
        {
            throw new DirectoryNotFoundException($"Game resources directory not found: {_gameResourcesDirectory}");
        }
    }
}
