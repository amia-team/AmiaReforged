using AmiaReforged.PwEngine.Systems.WorldEngine.Models;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine
{
    [TestFixture]
    public class CharacterTests
    {
        private const string PlayerKey = "<player-key>";
        private const string DmKey = "<dm-key>";
        private const string CustomSystemTag = "<custom-tag>";
        private const string DefaultSystemTag = "engine";

        [Test]
        public void Constructor_ValidParameters_InitializesProperties()
        {
            // Arrange
            Guid id = Guid.NewGuid();
            const string name = "Test Name";
            CharacterOwner.Player owner = new CharacterOwner.Player(PlayerKey);

            // Act
            Character sut = new Character(id, name, owner);

            // Assert
            Assert.That(sut.Id, Is.EqualTo(id));
            Assert.That(sut.Name, Is.EqualTo(name));
            Assert.That(sut.IsActive, Is.True);
            Assert.That(sut.IsPlayerOwned(out string? key), Is.True);
            Assert.That(key, Is.EqualTo(PlayerKey));
        }

        [Test]
        public void Constructor_EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new Character(Guid.Empty, "Name", new CharacterOwner.Player(PlayerKey)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        public void Constructor_NullOrWhitespaceName_Throws(string? badName)
        {
            Assert.Throws<ArgumentException>(() =>
                new Character(Guid.NewGuid(), badName!, new CharacterOwner.Player(PlayerKey)));
        }

        [Test]
        public void Constructor_NullOwner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new Character(Guid.NewGuid(), "Name", owner: null!));
        }

        [Test]
        public void CreateEmpty_ReturnsDefaults()
        {
            Character sut = Character.CreateEmpty();

            Assert.That(sut.Id, Is.EqualTo(Guid.Empty));
            Assert.That(sut.Name, Is.Null);
            Assert.That(sut.IsActive, Is.False);
            Assert.That(sut.IsPlayerOwned(out string? pk), Is.False);
            Assert.That(pk, Is.Null);
            Assert.That(sut.IsDmOwned(out string? dk), Is.False);
            Assert.That(dk, Is.Null);
            Assert.That(sut.IsSystemOwned(out string? tag), Is.False);
            Assert.That(tag, Is.Null);
        }

        [Test]
        public void CreateForPlayer_SetsOwner_Name_Id_And_Active()
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "Hero");

            Assert.That(sut.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(sut.Name, Is.EqualTo("Hero"));
            Assert.That(sut.IsActive, Is.True);
            Assert.That(sut.IsPlayerOwned(out string? key), Is.True);
            Assert.That(key, Is.EqualTo(PlayerKey));
            Assert.That(sut.IsDmOwned(out _), Is.False);
            Assert.That(sut.IsSystemOwned(out _), Is.False);
        }

        [Test]
        public void CreateForDungeonMaster_SetsOwner_Name_Id_And_Active()
        {
            Character sut = Character.CreateForDungeonMaster(DmKey, "Boss");

            Assert.That(sut.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(sut.Name, Is.EqualTo("Boss"));
            Assert.That(sut.IsActive, Is.True);
            Assert.That(sut.IsDmOwned(out string? key), Is.True);
            Assert.That(key, Is.EqualTo(DmKey));
            Assert.That(sut.IsPlayerOwned(out _), Is.False);
            Assert.That(sut.IsSystemOwned(out _), Is.False);
        }

        [Test]
        public void CreateForSystem_DefaultTag_SetsEngineTag()
        {
            Character sut = Character.CreateForSystem("SystemChar");

            Assert.That(sut.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(sut.Name, Is.EqualTo("SystemChar"));
            Assert.That(sut.IsActive, Is.True);
            Assert.That(sut.IsSystemOwned(out string? tag), Is.True);
            Assert.That(tag, Is.EqualTo(DefaultSystemTag));
            Assert.That(sut.IsPlayerOwned(out _), Is.False);
            Assert.That(sut.IsDmOwned(out _), Is.False);
        }

        [Test]
        public void CreateForSystem_CustomTag_SetsProvidedTag()
        {
            Character sut = Character.CreateForSystem("SystemChar", CustomSystemTag);

            Assert.That(sut.IsSystemOwned(out string? tag), Is.True);
            Assert.That(tag, Is.EqualTo(CustomSystemTag));
        }

        [Test]
        public void AssignToPlayer_ChangesOwnerAndClearsOthers()
        {
            Character sut = Character.CreateForDungeonMaster(DmKey, "Test");
            sut.AssignToPlayer(PlayerKey);

            Assert.That(sut.IsPlayerOwned(out string? key), Is.True);
            Assert.That(key, Is.EqualTo(PlayerKey));
            Assert.That(sut.IsDmOwned(out _), Is.False);
            Assert.That(sut.IsSystemOwned(out _), Is.False);
        }

        [Test]
        public void AssignToDungeonMaster_ChangesOwnerAndClearsOthers()
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "Test");
            sut.AssignToDungeonMaster(DmKey);

            Assert.That(sut.IsDmOwned(out string? key), Is.True);
            Assert.That(key, Is.EqualTo(DmKey));
            Assert.That(sut.IsPlayerOwned(out _), Is.False);
            Assert.That(sut.IsSystemOwned(out _), Is.False);
        }

        [Test]
        public void MakeSystemOwned_DefaultAndCustomTag()
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "Test");

            sut.MakeSystemOwned();
            Assert.That(sut.IsSystemOwned(out string? defaultTag), Is.True);
            Assert.That(defaultTag, Is.EqualTo(DefaultSystemTag));
            Assert.That(sut.IsPlayerOwned(out _), Is.False);
            Assert.That(sut.IsDmOwned(out _), Is.False);

            sut.MakeSystemOwned(CustomSystemTag);
            Assert.That(sut.IsSystemOwned(out string? customTag), Is.True);
            Assert.That(customTag, Is.EqualTo(CustomSystemTag));
        }

        [Test]
        public void Rename_ValidName_UpdatesName()
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "OldName");

            sut.Rename("NewName");

            Assert.That(sut.Name, Is.EqualTo("NewName"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        public void Rename_InvalidName_Throws(string? badName)
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "OldName");

            Assert.Throws<ArgumentException>(() => sut.Rename(badName!));
        }

        [Test]
        public void Activate_And_Deactivate_TogglesIsActive()
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "Test");
            Assert.That(sut.IsActive, Is.True);

            sut.Deactivate();
            Assert.That(sut.IsActive, Is.False);

            sut.Activate();
            Assert.That(sut.IsActive, Is.True);
        }

        [Test]
        public void OwnershipChecks_ReturnFalseAndNull_WhenNotOwnedByType()
        {
            Character sut = Character.CreateForPlayer(PlayerKey, "Test");

            Assert.That(sut.IsDmOwned(out string? dmKey), Is.False);
            Assert.That(dmKey, Is.Null);

            Assert.That(sut.IsSystemOwned(out string? tag), Is.False);
            Assert.That(tag, Is.Null);
        }
    }
}
