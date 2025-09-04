using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine
{
    [TestFixture]
    public class ResourceNodeDefinitionLoadingServiceTests
    {
        private string? _originalResourcePath;

        [SetUp]
        public void SetUp()
        {
            // Save original env var to restore later
            _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original env var
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);
        }

        [Test]
        public void Load_ReadsJsonFromTempFilesystem_AndCreatesDefinition()
        {
            // Arrange
            DirectoryInfo tempRoot = Directory.CreateTempSubdirectory("resource-node-happy-path");
            try
            {
                string nodesDir = Path.Combine(tempRoot.FullName, "Nodes");
                Directory.CreateDirectory(nodesDir);

                // Valid sample: non-empty Tag, non-null Requirement, non-empty Outputs
                File.WriteAllText(Path.Combine(nodesDir, "valid_node.json"), """
                                                                             {
                                                                               "Tag": "test-node-tag",
                                                                               "Requirement": {},
                                                                               "Outputs": [ {} ],
                                                                               "BaseHarvestRounds": 1
                                                                             }
                                                                             """);

                Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot.FullName);

                Mock<IResourceNodeDefinitionRepository> repoMock = new(Moq.MockBehavior.Strict);
                repoMock.Setup(r => r.Create(Moq.It.IsAny<ResourceNodeDefinition>()));

                ResourceNodeDefinitionLoadingService loader = new(repoMock.Object);

                // Act
                loader.Load();

                // Assert
                repoMock.Verify(r => r.Create(Moq.It.IsAny<ResourceNodeDefinition>()), Moq.Times.Once);
                Assert.That(loader.Failures(), Is.Empty);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot.FullName, recursive: true);
                }
                catch
                {
                    /* best effort */
                }
            }
        }


        [Test]
        public void Load_InvalidDefinitions_AreRejected_WithFailures()
        {
            DirectoryInfo tempRoot = Directory.CreateTempSubdirectory("resource-node-validation-test");
            try
            {
                string nodesDir = Path.Combine(tempRoot.FullName, "Nodes");
                Directory.CreateDirectory(nodesDir);

                // 1) Empty tag
                File.WriteAllText(Path.Combine(nodesDir, "empty_tag.json"), """
                                                                            {
                                                                              "Tag": "   ",
                                                                              "Requirement": {},
                                                                              "Outputs": [{}],
                                                                              "BaseHarvestRounds": 1
                                                                            }
                                                                            """);

                // 2) Null requirement
                File.WriteAllText(Path.Combine(nodesDir, "null_requirement.json"), """
                    {
                      "Tag": "node-null-req",
                      "Requirement": null,
                      "Outputs": [{}],
                      "BaseHarvestRounds": 1
                    }
                    """);

                // 3) Empty outputs
                File.WriteAllText(Path.Combine(nodesDir, "empty_outputs.json"), """
                    {
                      "Tag": "node-empty-out",
                      "Requirement": {},
                      "Outputs": [],
                      "BaseHarvestRounds": 1
                    }
                    """);

                Environment.SetEnvironmentVariable("RESOURCE_PATH", tempRoot.FullName);

                Mock<IResourceNodeDefinitionRepository> repoMock = new(MockBehavior.Strict);
                ResourceNodeDefinitionLoadingService loader = new(repoMock.Object);

                // Act
                loader.Load();

                // Assert
                repoMock.Verify(r => r.Create(It.IsAny<ResourceNodeDefinition>()), Times.Never);

                List<FileLoadResult> failures = loader.Failures();
                Assert.That(failures, Is.Not.Null);
                Assert.That(failures.Count, Is.EqualTo(3));
                Assert.That(failures, Has.Some.Matches<FileLoadResult>(f =>
                    f.FileName == "empty_tag.json" && f.Message != null &&
                    f.Message.Contains("Tag", StringComparison.OrdinalIgnoreCase)));
                Assert.That(failures, Has.Some.Matches<FileLoadResult>(f =>
                    f.FileName == "null_requirement.json" && f.Message != null &&
                    f.Message.Contains("Requirement", StringComparison.OrdinalIgnoreCase)));
                Assert.That(failures, Has.Some.Matches<FileLoadResult>(f =>
                    f.FileName == "empty_outputs.json" && f.Message != null &&
                    f.Message.Contains("Outputs", StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot.FullName, recursive: true);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
