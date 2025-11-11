using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Core.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaGatewayTests
{
    private Mock<IPersonaRepository> _mockPersonaRepo = null!;
    private Mock<IPersistentCharacterRepository> _mockCharacterRepo = null!;
    private Mock<IPersistentPlayerPersonaRepository> _mockPlayerRepo = null!;
    private PersonaGateway _gateway = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPersonaRepo = new Mock<IPersonaRepository>();
        _mockCharacterRepo = new Mock<IPersistentCharacterRepository>();
        _mockPlayerRepo = new Mock<IPersistentPlayerPersonaRepository>();

        _gateway = new PersonaGateway(
            _mockPersonaRepo.Object,
            _mockCharacterRepo.Object,
            _mockPlayerRepo.Object);
    }

    private static PersistedCharacter CreateTestCharacter(Guid id, string cdKey, string firstName, string lastName)
    {
        return new PersistedCharacter
        {
            Id = id,
            CdKey = cdKey,
            FirstName = firstName,
            LastName = lastName,
            PersonaIdString = PersonaId.FromCharacter(CharacterId.From(id)).ToString()
        };
    }

    private static PlayerPersonaRecord CreateTestPlayer(string cdKey, string displayName)
    {
        return new PlayerPersonaRecord
        {
            CdKey = cdKey,
            DisplayName = displayName,
            PersonaIdString = PersonaId.FromPlayerCdKey(cdKey).ToString(),
            CreatedUtc = DateTime.UtcNow.AddDays(-30),
            UpdatedUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow
        };
    }

    #region Basic Persona Lookup Tests

    [Test]
    public async Task GetPersonaAsync_WhenPersonaExists_ReturnsPersonaInfo()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        CharacterPersona persona = CharacterPersona.Create(characterId, "Aldric the Brave");
        Persona? outPersona = persona;

        _mockPersonaRepo
            .Setup(x => x.TryGetPersona(persona.Id, out outPersona))
            .Returns(true);

        // Act
        PersonaInfo? result = await _gateway.GetPersonaAsync(persona.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(persona.Id));
        Assert.That(result.DisplayName, Is.EqualTo("Aldric the Brave"));
        Assert.That(result.Type, Is.EqualTo(PersonaType.Character));
    }

    [Test]
    public async Task GetPersonaAsync_WhenPersonaDoesNotExist_ReturnsNull()
    {
        // Arrange
        PersonaId nonExistentId = PersonaId.FromCharacter(CharacterId.New());
        Persona? outPersona = null;

        _mockPersonaRepo
            .Setup(x => x.TryGetPersona(nonExistentId, out outPersona))
            .Returns(false);

        // Act
        PersonaInfo? result = await _gateway.GetPersonaAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ExistsAsync_WhenPersonaExists_ReturnsTrue()
    {
        // Arrange
        PersonaId personaId = PersonaId.FromCharacter(CharacterId.New());
        _mockPersonaRepo.Setup(x => x.Exists(personaId)).Returns(true);

        // Act
        bool result = await _gateway.ExistsAsync(personaId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsAsync_WhenPersonaDoesNotExist_ReturnsFalse()
    {
        // Arrange
        PersonaId personaId = PersonaId.FromCharacter(CharacterId.New());
        _mockPersonaRepo.Setup(x => x.Exists(personaId)).Returns(false);

        // Act
        bool result = await _gateway.ExistsAsync(personaId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Player-Character Mapping Tests

    [Test]
    public async Task GetPlayerCharactersAsync_ReturnsAllCharactersForPlayer()
    {
        // Arrange
        string cdKey = "TESTCDKEY123";
        Guid char1Guid = Guid.NewGuid();
        Guid char2Guid = Guid.NewGuid();

        List<PersistedCharacter> characters = new()
        {
            CreateTestCharacter(char1Guid, cdKey, "Aldric", "Brave"),
            CreateTestCharacter(char2Guid, cdKey, "Beatrice", "Wise")
        };

        _mockCharacterRepo.Setup(x => x.GetCharactersByCdKey(cdKey)).Returns(characters);

        // Act
        IReadOnlyList<CharacterPersonaInfo> result = await _gateway.GetPlayerCharactersAsync(cdKey);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].DisplayName, Is.EqualTo("Aldric Brave"));
        Assert.That(result[0].CdKey, Is.EqualTo(cdKey));
        Assert.That(result[1].DisplayName, Is.EqualTo("Beatrice Wise"));
    }

    [Test]
    public async Task GetPlayerCharactersAsync_WhenNoCharacters_ReturnsEmptyList()
    {
        // Arrange
        string cdKey = "NEWPLAYER";
        _mockCharacterRepo.Setup(x => x.GetCharactersByCdKey(cdKey)).Returns(new List<PersistedCharacter>());

        // Act
        IReadOnlyList<CharacterPersonaInfo> result = await _gateway.GetPlayerCharactersAsync(cdKey);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetCharacterOwnerAsync_ByCharacterId_ReturnsPlayerInfo()
    {
        // Arrange
        string cdKey = "TESTCDKEY123";
        Guid characterGuid = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(characterGuid);

        PersistedCharacter character = CreateTestCharacter(characterGuid, cdKey, "Aldric", "Brave");
        PlayerPersonaRecord playerRecord = CreateTestPlayer(cdKey, "TestPlayer");

        _mockCharacterRepo.Setup(x => x.GetByGuid(characterGuid)).Returns(character);
        _mockPlayerRepo.Setup(x => x.GetByCdKey(cdKey)).Returns(playerRecord);
        _mockCharacterRepo.Setup(x => x.GetCharactersByCdKey(cdKey)).Returns(new List<PersistedCharacter> { character });

        // Act
        PlayerPersonaInfo? result = await _gateway.GetCharacterOwnerAsync(characterId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CdKey, Is.EqualTo(cdKey));
        Assert.That(result.DisplayName, Is.EqualTo("TestPlayer"));
        Assert.That(result.CharacterCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GetCharacterOwnerAsync_ByCharacterId_WhenCharacterNotFound_ReturnsNull()
    {
        // Arrange
        CharacterId characterId = CharacterId.New();
        _mockCharacterRepo.Setup(x => x.GetByGuid(characterId.Value)).Returns((PersistedCharacter?)null);

        // Act
        PlayerPersonaInfo? result = await _gateway.GetCharacterOwnerAsync(characterId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCharacterPersonaIdAsync_WhenCharacterExists_ReturnsPersonaId()
    {
        // Arrange
        Guid characterGuid = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(characterGuid);
        PersonaId expectedPersonaId = PersonaId.FromCharacter(characterId);

        PersistedCharacter character = CreateTestCharacter(characterGuid, "TEST", "Test", "Character");

        _mockCharacterRepo.Setup(x => x.GetByGuid(characterGuid)).Returns(character);

        // Act
        PersonaId? result = await _gateway.GetCharacterPersonaIdAsync(characterId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(expectedPersonaId));
    }

    [Test]
    public async Task GetPersonaCharacterIdAsync_WhenCharacterPersona_ReturnsCharacterId()
    {
        // Arrange
        Guid characterGuid = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(characterGuid);
        PersonaId personaId = PersonaId.FromCharacter(characterId);

        PersistedCharacter character = CreateTestCharacter(characterGuid, "TEST", "Test", "Character");

        _mockCharacterRepo.Setup(x => x.GetByPersonaId(personaId)).Returns(character);

        // Act
        CharacterId? result = await _gateway.GetPersonaCharacterIdAsync(personaId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(characterId));
    }

    [Test]
    public async Task GetPersonaCharacterIdAsync_WhenNotCharacterPersona_ReturnsNull()
    {
        // Arrange
        PersonaId playerPersonaId = PersonaId.FromPlayerCdKey("TESTCDKEY");

        // Act
        CharacterId? result = await _gateway.GetPersonaCharacterIdAsync(playerPersonaId);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Character Identity Tests

    [Test]
    public async Task GetCharacterIdentityAsync_WhenCharacterExists_ReturnsFullIdentity()
    {
        // Arrange
        Guid characterGuid = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(characterGuid);
        string cdKey = "TESTCDKEY";

        PersistedCharacter character = CreateTestCharacter(characterGuid, cdKey, "Aldric", "the Brave");

        _mockCharacterRepo.Setup(x => x.GetByGuid(characterGuid)).Returns(character);

        // Act
        CharacterIdentityInfo? result = await _gateway.GetCharacterIdentityAsync(characterId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FirstName, Is.EqualTo("Aldric"));
        Assert.That(result.LastName, Is.EqualTo("the Brave"));
        Assert.That(result.FullName, Is.EqualTo("Aldric the Brave"));
        Assert.That(result.CdKey, Is.EqualTo(cdKey));
    }

    [Test]
    public async Task GetCharacterIdentityAsync_WhenCharacterHasEmptyLastName_ReturnsFirstNameOnly()
    {
        // Arrange
        Guid characterGuid = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(characterGuid);

        PersistedCharacter character = new()
        {
            Id = characterGuid,
            CdKey = "TEST",
            FirstName = "Aldric",
            LastName = "",
            PersonaIdString = PersonaId.FromCharacter(characterId).ToString()
        };

        _mockCharacterRepo.Setup(x => x.GetByGuid(characterGuid)).Returns(character);

        // Act
        CharacterIdentityInfo? result = await _gateway.GetCharacterIdentityAsync(characterId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FullName, Is.EqualTo("Aldric"));
    }

    [Test]
    public async Task GetCharacterIdentityByPersonaAsync_WhenCharacterExists_ReturnsIdentity()
    {
        // Arrange
        Guid characterGuid = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(characterGuid);
        PersonaId personaId = PersonaId.FromCharacter(characterId);

        PersistedCharacter character = CreateTestCharacter(characterGuid, "TEST", "Beatrice", "Wise");

        _mockCharacterRepo.Setup(x => x.GetByPersonaId(personaId)).Returns(character);

        // Act
        CharacterIdentityInfo? result = await _gateway.GetCharacterIdentityByPersonaAsync(personaId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PersonaId, Is.EqualTo(personaId));
        Assert.That(result.FullName, Is.EqualTo("Beatrice Wise"));
    }

    #endregion

    #region Player Identity Tests

    [Test]
    public async Task GetPlayerAsync_WhenPlayerExists_ReturnsPlayerInfo()
    {
        // Arrange
        string cdKey = "TESTCDKEY123";
        DateTime createdDate = DateTime.UtcNow.AddMonths(-6);
        DateTime lastSeen = DateTime.UtcNow.AddHours(-2);

        PlayerPersonaRecord playerRecord = new()
        {
            CdKey = cdKey,
            DisplayName = "TestPlayer",
            PersonaIdString = PersonaId.FromPlayerCdKey(cdKey).ToString(),
            CreatedUtc = createdDate,
            UpdatedUtc = DateTime.UtcNow,
            LastSeenUtc = lastSeen
        };

        List<PersistedCharacter> characters = new()
        {
            CreateTestCharacter(Guid.NewGuid(), cdKey, "Char1", "One"),
            CreateTestCharacter(Guid.NewGuid(), cdKey, "Char2", "Two"),
            CreateTestCharacter(Guid.NewGuid(), cdKey, "Char3", "Three")
        };

        _mockPlayerRepo.Setup(x => x.GetByCdKey(cdKey)).Returns(playerRecord);
        _mockCharacterRepo.Setup(x => x.GetCharactersByCdKey(cdKey)).Returns(characters);

        // Act
        PlayerPersonaInfo? result = await _gateway.GetPlayerAsync(cdKey);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CdKey, Is.EqualTo(cdKey));
        Assert.That(result.DisplayName, Is.EqualTo("TestPlayer"));
        Assert.That(result.CharacterCount, Is.EqualTo(3));
        Assert.That(result.CreatedUtc, Is.EqualTo(createdDate));
        Assert.That(result.LastSeenUtc, Is.EqualTo(lastSeen));
    }

    [Test]
    public async Task GetPlayerAsync_WhenPlayerDoesNotExist_ReturnsNull()
    {
        // Arrange
        string cdKey = "NONEXISTENT";
        _mockPlayerRepo.Setup(x => x.GetByCdKey(cdKey)).Returns((PlayerPersonaRecord?)null);

        // Act
        PlayerPersonaInfo? result = await _gateway.GetPlayerAsync(cdKey);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPlayerByPersonaAsync_WhenPlayerPersona_ReturnsPlayerInfo()
    {
        // Arrange
        string cdKey = "TESTCDKEY";
        PersonaId playerPersonaId = PersonaId.FromPlayerCdKey(cdKey);

        PlayerPersonaRecord playerRecord = CreateTestPlayer(cdKey, "TestPlayer");

        _mockPlayerRepo.Setup(x => x.GetByCdKey(cdKey)).Returns(playerRecord);
        _mockCharacterRepo.Setup(x => x.GetCharactersByCdKey(cdKey)).Returns(new List<PersistedCharacter>());

        // Act
        PlayerPersonaInfo? result = await _gateway.GetPlayerByPersonaAsync(playerPersonaId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CdKey, Is.EqualTo(cdKey));
    }

    [Test]
    public async Task GetPlayerByPersonaAsync_WhenNotPlayerPersona_ReturnsNull()
    {
        // Arrange
        PersonaId characterPersonaId = PersonaId.FromCharacter(CharacterId.New());

        // Act
        PlayerPersonaInfo? result = await _gateway.GetPlayerByPersonaAsync(characterPersonaId);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Holdings Tests (Placeholder)

    [Test]
    public async Task GetPersonaHoldingsAsync_ReturnsEmptyHoldings()
    {
        // Arrange
        PersonaId personaId = PersonaId.FromCharacter(CharacterId.New());

        // Act
        PersonaHoldingsInfo result = await _gateway.GetPersonaHoldingsAsync(personaId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PersonaId, Is.EqualTo(personaId));
        Assert.That(result.OwnedProperties, Is.Empty);
        Assert.That(result.Rentals, Is.Empty);
        Assert.That(result.TotalHoldings, Is.EqualTo(0));
    }

    [Test]
    public async Task GetPlayerAggregateHoldingsAsync_ReturnsEmptyHoldings()
    {
        // Arrange
        string cdKey = "TESTCDKEY";

        // Act
        PlayerAggregateHoldingsInfo result = await _gateway.GetPlayerAggregateHoldingsAsync(cdKey);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CdKey, Is.EqualTo(cdKey));
        Assert.That(result.CharacterHoldings, Is.Empty);
        Assert.That(result.TotalOwnedProperties, Is.EqualTo(0));
        Assert.That(result.TotalRentals, Is.EqualTo(0));
    }

    #endregion
}

