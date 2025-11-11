using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.DefinitionLoaders
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class IndustryDefinitionLoadingTests
    {
        private string? _originalResourcePath;
        private string _tempRoot = null!;
        private Mock<IIndustryRepository> _repoMock = null!;

        [SetUp]
        public void SetUp()
        {
            _originalResourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
            _tempRoot = Directory.CreateTempSubdirectory("industry-loader-tests-").FullName;
            _repoMock = new Mock<IIndustryRepository>(MockBehavior.Strict);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original env var
            if (_originalResourcePath is null)
                Environment.SetEnvironmentVariable("RESOURCE_PATH", null);
            else
                Environment.SetEnvironmentVariable("RESOURCE_PATH", _originalResourcePath);

            try
            {
                if (Directory.Exists(_tempRoot))
                    Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup; ignore
            }
        }

        [Test]
        public void Load_WhenResourcePathNotSet_RecordsFailure_AndDoesNotCallRepository()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", null);

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Never);
            Assert.That(sut.Failures(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Load_WhenIndustriesDirectoryMissing_RecordsFailure_AndDoesNotCallRepository()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            // Do not create "Industries" directory

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Never);
            Assert.That(sut.Failures(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Load_WithSingleValidFile_AddsToRepository_AndNoFailures()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            string industriesDir = Path.Combine(_tempRoot, "Industries");
            Directory.CreateDirectory(industriesDir);

            string json = """
                          {
                            "Tag": "industry.sample",
                            "Name": "Sample Industry",
                            "Knowledge": []
                          }
                          """;
            File.WriteAllText(Path.Combine(industriesDir, "woodcutter.json"), json);

            _repoMock.Setup(r => r.Add(It.Is<Industry>(i => i.Tag == "industry.sample" && i.Name == "Sample Industry")));

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Once);
            Assert.That(sut.Failures(), Is.Empty);
        }

        [Test]
        public void Load_WithMalformedJson_RecordsFailure()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            string industriesDir = Path.Combine(_tempRoot, "Industries");
            Directory.CreateDirectory(industriesDir);

            string malformed = "{ \"Tag\": \"X\", \"Name\": ";
            File.WriteAllText(Path.Combine(industriesDir, "bad.json"), malformed);

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Never);
            Assert.That(sut.Failures(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Load_WhenDeserializerReturnsNull_RecordsFailure()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            string industriesDir = Path.Combine(_tempRoot, "Industries");
            Directory.CreateDirectory(industriesDir);

            // This causes System.Text.Json to return null
            File.WriteAllText(Path.Combine(industriesDir, "null.json"), "null");

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Never);
            Assert.That(sut.Failures(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Load_WhenTagEmpty_RecordsFailure_AndDoesNotAdd()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            string industriesDir = Path.Combine(_tempRoot, "Industries");
            Directory.CreateDirectory(industriesDir);

            string json = """
                          {
                            "Tag": "",
                            "Name": "SomeName"
                          }
                          """;
            File.WriteAllText(Path.Combine(industriesDir, "empty-tag.json"), json);

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Never);
            Assert.That(sut.Failures(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Load_WhenNameEmpty_RecordsFailure_AndDoesNotAdd()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            string industriesDir = Path.Combine(_tempRoot, "Industries");
            Directory.CreateDirectory(industriesDir);

            string json = """
                          {
                            "Tag": "VALID_TAG",
                            "Name": ""
                          }
                          """;
            File.WriteAllText(Path.Combine(industriesDir, "empty-name.json"), json);

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Never);
            Assert.That(sut.Failures(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Load_MultipleFiles_MixesValidAndInvalid_AddsOnlyValid_RecordsFailuresForInvalid()
        {
            Environment.SetEnvironmentVariable("RESOURCE_PATH", _tempRoot);
            string industriesDir = Path.Combine(_tempRoot, "Industries");
            Directory.CreateDirectory(industriesDir);

            string valid1 = """
                            { "Tag": "A", "Name": "Alpha", "Knowledge": [] }
                            """;
            string valid2 = """
                            { "Tag": "B", "Name": "Beta", "Knowledge": [] }
                            """;
            string invalidEmptyTag = """
                                     { "Tag": "", "Name": "NoTag" }
                                     """;
            string malformed = "{";

            File.WriteAllText(Path.Combine(industriesDir, "valid1.json"), valid1);
            File.WriteAllText(Path.Combine(industriesDir, "valid2.json"), valid2);
            File.WriteAllText(Path.Combine(industriesDir, "invalid-empty-tag.json"), invalidEmptyTag);
            File.WriteAllText(Path.Combine(industriesDir, "malformed.json"), malformed);

            _repoMock.Setup(r => r.Add(It.Is<Industry>(i => i.Tag == "A" && i.Name == "Alpha")));
            _repoMock.Setup(r => r.Add(It.Is<Industry>(i => i.Tag == "B" && i.Name == "Beta")));

            IndustryDefinitionLoadingService sut = new IndustryDefinitionLoadingService(_repoMock.Object, new InMemoryEventBus());

            sut.Load();

            _repoMock.Verify(r => r.Add(It.IsAny<Industry>()), Times.Exactly(2));
            Assert.That(sut.Failures(), Has.Count.EqualTo(2));
        }
    }
}
