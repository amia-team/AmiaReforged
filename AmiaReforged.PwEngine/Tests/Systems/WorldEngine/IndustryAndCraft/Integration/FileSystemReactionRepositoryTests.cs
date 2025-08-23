
using System.Collections.Immutable;
using System.Text.Json;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Integration.JsonModels;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft.Integration;

[TestFixture]
public class FileSystemReactionRepositoryTests
{
    private Mock<IReactionPreconditionFactory> _mockPreconditionFactory = null!;
    private Mock<IReactionModifierFactory> _mockModifierFactory = null!;
    private Mock<ILogger<FileSystemReactionRepository>> _mockLogger = null!;
    private string _testDirectory = null!;
    private string _reactionsDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _mockPreconditionFactory = new Mock<IReactionPreconditionFactory>();
        _mockModifierFactory = new Mock<IReactionModifierFactory>();
        _mockLogger = new Mock<ILogger<FileSystemReactionRepository>>();

        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _reactionsDirectory = Path.Combine(_testDirectory, "reactions");

        Directory.CreateDirectory(_reactionsDirectory);
        Environment.SetEnvironmentVariable("GAME_RESOURCES_DIRECTORY", _testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("GAME_RESOURCES_DIRECTORY", null);
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void Constructor_WhenGameResourcesDirectoryNotSet_ThrowsInvalidOperationException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GAME_RESOURCES_DIRECTORY", null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new FileSystemReactionRepository(_mockPreconditionFactory.Object, _mockModifierFactory.Object, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WhenGameResourcesDirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GAME_RESOURCES_DIRECTORY", "/nonexistent/path");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            new FileSystemReactionRepository(_mockPreconditionFactory.Object, _mockModifierFactory.Object, _mockLogger.Object));
    }

    [Test]
    public async Task LoadAllReactionsAsync_WhenReactionsDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        Directory.Delete(_reactionsDirectory);
        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task LoadAllReactionsAsync_WithValidSingleReactionFile_ReturnsReaction()
    {
        // Arrange
        Guid reactionId = Guid.NewGuid();
        ReactionJson reactionJson = CreateValidReactionJson(reactionId);
        await File.WriteAllTextAsync(Path.Combine(_reactionsDirectory, "test.json"), JsonSerializer.Serialize(reactionJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        ReactionDefinition reaction = result[0];
        Assert.That(reaction.Id, Is.EqualTo(reactionId));
        Assert.That(reaction.Name, Is.EqualTo("Test Reaction"));
        Assert.That(reaction.BaseDuration, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(reaction.BaseSuccessChance, Is.EqualTo(0.85));
        Assert.That(reaction.Inputs.Length, Is.EqualTo(1));
        Assert.That(reaction.Outputs.Length, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAllReactionsAsync_WithValidArrayReactionFile_ReturnsReactions()
    {
        // Arrange
        Guid reaction1Id = Guid.NewGuid();
        Guid reaction2Id = Guid.NewGuid();
        ReactionJson[] reactionsJson =
        [
            CreateValidReactionJson(reaction1Id, "Reaction 1"),
            CreateValidReactionJson(reaction2Id, "Reaction 2")
        ];

        await File.WriteAllTextAsync(
            Path.Combine(_reactionsDirectory, "test.json"),
            JsonSerializer.Serialize(reactionsJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.Id), Does.Contain(reaction1Id));
        Assert.That(result.Select(r => r.Id), Does.Contain(reaction2Id));
    }

    [Test]
    public async Task LoadAllReactionsAsync_WithInvalidJson_ContinuesProcessingOtherFiles()
    {
        // Arrange
        await File.WriteAllTextAsync(Path.Combine(_reactionsDirectory, "invalid.json"), "{ invalid json }");

        Guid validReactionId = Guid.NewGuid();
        ReactionJson validReactionJson = CreateValidReactionJson(validReactionId);
        await File.WriteAllTextAsync(
            Path.Combine(_reactionsDirectory, "valid.json"),
            JsonSerializer.Serialize(validReactionJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo(validReactionId));
    }

    [Test]
    public async Task LoadAllReactionsAsync_WithSubdirectories_LoadsFromAllDirectories()
    {
        // Arrange
        string subDirectory = Path.Combine(_reactionsDirectory, "subfolder");
        Directory.CreateDirectory(subDirectory);

        Guid reaction1Id = Guid.NewGuid();
        Guid reaction2Id = Guid.NewGuid();

        await File.WriteAllTextAsync(
            Path.Combine(_reactionsDirectory, "reaction1.json"),
            JsonSerializer.Serialize(CreateValidReactionJson(reaction1Id)));

        await File.WriteAllTextAsync(
            Path.Combine(subDirectory, "reaction2.json"),
            JsonSerializer.Serialize(CreateValidReactionJson(reaction2Id)));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.Id), Does.Contain(reaction1Id));
        Assert.That(result.Select(r => r.Id), Does.Contain(reaction2Id));
    }

    [Test]
    public async Task LoadAllReactionsAsync_CalledMultipleTimes_UsesCaching()
    {
        // Arrange
        Guid reactionId = Guid.NewGuid();
        ReactionJson reactionJson = CreateValidReactionJson(reactionId);
        await File.WriteAllTextAsync(Path.Combine(_reactionsDirectory, "test.json"), JsonSerializer.Serialize(reactionJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result1 = await repository.LoadAllReactionsAsync();
        IReadOnlyList<ReactionDefinition> result2 = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result1, Is.SameAs(result2));
    }

    [Test]
    public async Task GetReactionByIdAsync_WhenReactionExists_ReturnsReaction()
    {
        // Arrange
        Guid reactionId = Guid.NewGuid();
        ReactionJson reactionJson = CreateValidReactionJson(reactionId);
        await File.WriteAllTextAsync(Path.Combine(_reactionsDirectory, "test.json"), JsonSerializer.Serialize(reactionJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        ReactionDefinition? result = await repository.GetReactionByIdAsync(reactionId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(reactionId));
    }

    [Test]
    public async Task GetReactionByIdAsync_WhenReactionDoesNotExist_ReturnsNull()
    {
        // Arrange
        FileSystemReactionRepository repository = CreateRepository();

        // Act
        ReactionDefinition? result = await repository.GetReactionByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetReactionsByNameAsync_WhenNameMatches_ReturnsMatchingReactions()
    {
        // Arrange
        ReactionJson reaction1Json = CreateValidReactionJson(Guid.NewGuid(), "Iron Sword");
        ReactionJson reaction2Json = CreateValidReactionJson(Guid.NewGuid(), "Iron Shield");
        ReactionJson reaction3Json = CreateValidReactionJson(Guid.NewGuid(), "Steel Sword");

        ReactionJson[] reactionsJson = [reaction1Json, reaction2Json, reaction3Json];
        await File.WriteAllTextAsync(
            Path.Combine(_reactionsDirectory, "test.json"),
            JsonSerializer.Serialize(reactionsJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.GetReactionsByNameAsync("Iron");

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.Name), Does.Contain("Iron Sword"));
        Assert.That(result.Select(r => r.Name), Does.Contain("Iron Shield"));
    }

    [Test]
    public async Task GetReactionsByNameAsync_WithNullOrEmptyName_ReturnsEmptyList()
    {
        // Arrange
        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> resultNull = await repository.GetReactionsByNameAsync(null!);
        IReadOnlyList<ReactionDefinition> resultEmpty = await repository.GetReactionsByNameAsync("");
        IReadOnlyList<ReactionDefinition> resultWhitespace = await repository.GetReactionsByNameAsync("   ");

        // Assert
        Assert.That(resultNull, Is.Empty);
        Assert.That(resultEmpty, Is.Empty);
        Assert.That(resultWhitespace, Is.Empty);
    }

    [Test]
    public async Task LoadAllReactionsAsync_WithPreconditionsAndModifiers_CallsFactories()
    {
        // Arrange
        Mock<IReactionPrecondition> mockPrecondition = new Mock<IReactionPrecondition>();
        Mock<IReactionModifier> mockModifier = new Mock<IReactionModifier>();

        _mockPreconditionFactory
            .Setup(f => f.Create("TestPrecondition", It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Returns(mockPrecondition.Object);

        _mockModifierFactory
            .Setup(f => f.Create("TestModifier", It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Returns(mockModifier.Object);

        ReactionJson reactionJson = CreateValidReactionJsonWithPreconditionsAndModifiers(Guid.NewGuid());

        await File.WriteAllTextAsync(
            Path.Combine(_reactionsDirectory, "test.json"),
            JsonSerializer.Serialize(reactionJson));

        FileSystemReactionRepository repository = CreateRepository();

        // Act
        IReadOnlyList<ReactionDefinition> result = await repository.LoadAllReactionsAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        ReactionDefinition reaction = result[0];
        Assert.That(reaction.Preconditions.Length, Is.EqualTo(1));
        Assert.That(reaction.Modifiers.Length, Is.EqualTo(1));

        _mockPreconditionFactory.Verify(
            f => f.Create("TestPrecondition", It.IsAny<IReadOnlyDictionary<string, object>>()),
            Times.Once);

        _mockModifierFactory.Verify(
            f => f.Create("TestModifier", It.IsAny<IReadOnlyDictionary<string, object>>()),
            Times.Once);
    }

    [Test]
    public void LoadAllReactionsAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        FileSystemReactionRepository repository = CreateRepository();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        Assert.That(async () => await repository.LoadAllReactionsAsync(cancellationTokenSource.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private FileSystemReactionRepository CreateRepository()
    {
        return new FileSystemReactionRepository(
            _mockPreconditionFactory.Object,
            _mockModifierFactory.Object,
            _mockLogger.Object);
    }

    private static ReactionJson CreateValidReactionJson(Guid id, string name = "Test Reaction")
    {
        return new ReactionJson
        {
            Id = id.ToString(),
            Name = name,
            Inputs = [new() { Item = "iron_ingot", Amount = 2 }],
            Outputs = [new() { Item = "iron_sword", Amount = 1 }],
            BaseDuration = "00:05:00",
            BaseSuccessChance = 0.85
        };
    }

    private static ReactionJson CreateValidReactionJsonWithPreconditionsAndModifiers(Guid id, string name = "Test Reaction")
    {
        return new ReactionJson
        {
            Id = id.ToString(),
            Name = name,
            Inputs = [new() { Item = "iron_ingot", Amount = 2 }],
            Outputs = [new() { Item = "iron_sword", Amount = 1 }],
            BaseDuration = "00:05:00",
            BaseSuccessChance = 0.85,
            Preconditions =
            [
                new()
                {
                    Type = "TestPrecondition",
                    Description = "Test precondition",
                    Parameters = new Dictionary<string, object> { { "param1", "value1" } }
                }
            ],
            Modifiers =
            [
                new()
                {
                    Type = "TestModifier",
                    Description = "Test modifier",
                    Parameters = new Dictionary<string, object> { { "param2", "value2" } }
                }
            ]
        };
    }
}
