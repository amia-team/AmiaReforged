using System.IO;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests;

[TestFixture]
public class ResourceWatcherServiceTests
{
    private string _testDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"resource-watcher-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        Environment.SetEnvironmentVariable("RESOURCE_PATH", _testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("RESOURCE_PATH", null);
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Test]
    public void Constructor_InitializesWithExistingFiles()
    {
        // Arrange
        string file1 = Path.Combine(_testDirectory, "test1.json");
        string file2 = Path.Combine(_testDirectory, "test2.json");
        File.WriteAllText(file1, "{\"test\": 1}");
        File.WriteAllText(file2, "{\"test\": 2}");

        // Act
        ResourceWatcherService service = new ResourceWatcherService();

        // Assert - service should have initialized without throwing
        Assert.Pass("Service initialized successfully with existing files");
    }

    [Test]
    public void Constructor_HandlesEmptyDirectory()
    {
        // Act
        ResourceWatcherService service = new ResourceWatcherService();

        // Assert
        Assert.Pass("Service initialized successfully with empty directory");
    }

    [Test]
    public void FileSystemChanged_FiresOnlyOnceForContentChange()
    {
        // Arrange
        ResourceWatcherService service = new ResourceWatcherService();
        int eventCount = 0;
        service.FileSystemChanged += (sender, args) => eventCount++;

        string testFile = Path.Combine(_testDirectory, "test.json");

        // Act - Create file
        File.WriteAllText(testFile, "{\"value\": 1}");
        Thread.Sleep(600); // Wait for debounce

        int countAfterCreate = eventCount;

        // Act - Modify with different content
        File.WriteAllText(testFile, "{\"value\": 2}");
        Thread.Sleep(600); // Wait for debounce

        int countAfterChange = eventCount;

        // Assert
        Assert.That(countAfterCreate, Is.GreaterThan(0), "Event should fire after file creation");
        Assert.That(countAfterChange, Is.GreaterThan(countAfterCreate), "Event should fire after content change");
    }

    [Test]
    public void FileSystemChanged_DoesNotFireForSameContent()
    {
        // Arrange
        string testFile = Path.Combine(_testDirectory, "test.json");
        string content = "{\"value\": 123}";
        File.WriteAllText(testFile, content);

        ResourceWatcherService service = new ResourceWatcherService();
        int eventCount = 0;
        service.FileSystemChanged += (sender, args) => eventCount++;

        // Act - Touch file with same content
        File.WriteAllText(testFile, content);
        Thread.Sleep(600); // Wait for debounce

        // Assert
        Assert.That(eventCount, Is.EqualTo(0), "Event should not fire when content hasn't changed");
    }

    [Test]
    public void FileSystemChanged_DebouncesBurstOfChanges()
    {
        // Arrange
        ResourceWatcherService service = new ResourceWatcherService();
        int eventCount = 0;
        service.FileSystemChanged += (sender, args) => eventCount++;

        // Act - Create multiple files rapidly
        for (int i = 0; i < 5; i++)
        {
            string testFile = Path.Combine(_testDirectory, $"test{i}.json");
            File.WriteAllText(testFile, $"{{\"value\": {i}}}");
            Thread.Sleep(50); // Small delay between writes
        }

        Thread.Sleep(600); // Wait for debounce

        // Assert - Should only fire once despite multiple changes
        Assert.That(eventCount, Is.EqualTo(1), "Should debounce multiple rapid changes into a single event");
    }

    [Test]
    public void FileSystemChanged_IgnoresNonJsonFiles()
    {
        // Arrange
        ResourceWatcherService service = new ResourceWatcherService();
        int eventCount = 0;
        service.FileSystemChanged += (sender, args) => eventCount++;

        // Act - Create non-JSON file
        string txtFile = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(txtFile, "This is not JSON");
        Thread.Sleep(600);

        // Assert
        Assert.That(eventCount, Is.EqualTo(0), "Should ignore non-JSON files");
    }

    [Test]
    public void FileSystemChanged_HandlesDeletedFiles()
    {
        // Arrange
        string testFile = Path.Combine(_testDirectory, "test.json");
        File.WriteAllText(testFile, "{\"value\": 1}");

        ResourceWatcherService service = new ResourceWatcherService();
        int eventCount = 0;
        service.FileSystemChanged += (sender, args) => eventCount++;

        // Act - Delete file
        File.Delete(testFile);
        Thread.Sleep(600);

        // Assert
        Assert.That(eventCount, Is.GreaterThan(0), "Should fire event when file is deleted");
    }

    [Test]
    public void Constructor_HandlesNoResourcePath()
    {
        // Arrange
        Environment.SetEnvironmentVariable("RESOURCE_PATH", null);

        // Act & Assert - should not throw
        ResourceWatcherService service = new ResourceWatcherService();
        Assert.Pass("Service handles missing RESOURCE_PATH gracefully");
    }

    [Test]
    public void FileSystemChanged_TracksMultipleFileChanges()
    {
        // Arrange
        ResourceWatcherService service = new ResourceWatcherService();
        int eventCount = 0;
        service.FileSystemChanged += (sender, args) => eventCount++;

        string file1 = Path.Combine(_testDirectory, "file1.json");
        string file2 = Path.Combine(_testDirectory, "file2.json");

        // Act - Create first file
        File.WriteAllText(file1, "{\"value\": 1}");
        Thread.Sleep(600);
        int countAfterFirst = eventCount;

        // Act - Create second file
        File.WriteAllText(file2, "{\"value\": 2}");
        Thread.Sleep(600);
        int countAfterSecond = eventCount;

        // Assert
        Assert.That(countAfterFirst, Is.GreaterThan(0), "First file should trigger event");
        Assert.That(countAfterSecond, Is.GreaterThan(countAfterFirst), "Second file should trigger another event");
    }
}

