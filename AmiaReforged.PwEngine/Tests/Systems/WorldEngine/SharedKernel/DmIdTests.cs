using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.SharedKernel;

[TestFixture]
public class DmIdTests
{
    [Test]
    public void From_WithValidGuid_CreatesDmId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var dmId = DmId.From(guid);

        // Assert
        Assert.That(dmId.Value, Is.EqualTo(guid));
    }

    [Test]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => DmId.From(Guid.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("id"));
        Assert.That(ex.Message, Does.Contain("DmId cannot be empty"));
    }

    [Test]
    public void FromCdKey_WithValidKey_CreatesDeterministicDmId()
    {
        // Arrange
        const string cdKey = "ABCD1234";

        // Act
        var dmId1 = DmId.FromCdKey(cdKey);
        var dmId2 = DmId.FromCdKey(cdKey);

        // Assert
        Assert.That(dmId1, Is.EqualTo(dmId2), "Same CD key should produce same DmId");
        Assert.That(dmId1.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void FromCdKey_WithDifferentKeys_CreatesDifferentDmIds()
    {
        // Arrange
        const string cdKey1 = "ABCD1234";
        const string cdKey2 = "WXYZ9876";

        // Act
        var dmId1 = DmId.FromCdKey(cdKey1);
        var dmId2 = DmId.FromCdKey(cdKey2);

        // Assert
        Assert.That(dmId1, Is.Not.EqualTo(dmId2));
    }

    [Test]
    public void FromCdKey_IsCaseInsensitive()
    {
        // Arrange
        const string cdKeyLower = "abcd1234";
        const string cdKeyUpper = "ABCD1234";
        const string cdKeyMixed = "AbCd1234";

        // Act
        var dmId1 = DmId.FromCdKey(cdKeyLower);
        var dmId2 = DmId.FromCdKey(cdKeyUpper);
        var dmId3 = DmId.FromCdKey(cdKeyMixed);

        // Assert
        Assert.That(dmId1, Is.EqualTo(dmId2));
        Assert.That(dmId2, Is.EqualTo(dmId3));
    }

    [Test]
    public void FromCdKey_WithWhitespace_TrimsAndWorks()
    {
        // Arrange
        const string cdKeyWithSpaces = "  ABCD1234  ";
        const string cdKeyClean = "ABCD1234";

        // Act
        var dmId1 = DmId.FromCdKey(cdKeyWithSpaces);
        var dmId2 = DmId.FromCdKey(cdKeyClean);

        // Assert
        Assert.That(dmId1, Is.EqualTo(dmId2));
    }

    [Test]
    public void FromCdKey_WithNullKey_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => DmId.FromCdKey(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("publicCdKey"));
        Assert.That(ex.Message, Does.Contain("cannot be null or whitespace"));
    }

    [Test]
    public void FromCdKey_WithEmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => DmId.FromCdKey(""));
        Assert.That(ex!.ParamName, Is.EqualTo("publicCdKey"));
    }

    [Test]
    public void FromCdKey_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => DmId.FromCdKey("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("publicCdKey"));
    }

    [Test]
    public void FromCdKey_WithTooShortKey_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => DmId.FromCdKey("ABC123"));
        Assert.That(ex!.ParamName, Is.EqualTo("publicCdKey"));
        Assert.That(ex.Message, Does.Contain("must be exactly 8 characters"));
    }

    [Test]
    public void FromCdKey_WithTooLongKey_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => DmId.FromCdKey("ABCD12345"));
        Assert.That(ex!.ParamName, Is.EqualTo("publicCdKey"));
        Assert.That(ex.Message, Does.Contain("must be exactly 8 characters"));
    }

    [Test]
    public void FromCdKey_WithExactly8Characters_Succeeds()
    {
        // Arrange
        const string cdKey = "12345678";

        // Act
        var dmId = DmId.FromCdKey(cdKey);

        // Assert
        Assert.That(dmId.Value, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dmId1 = DmId.From(guid);
        var dmId2 = DmId.From(guid);

        // Act & Assert
        Assert.That(dmId1, Is.EqualTo(dmId2));
        Assert.That(dmId1.GetHashCode(), Is.EqualTo(dmId2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var dmId1 = DmId.From(Guid.NewGuid());
        var dmId2 = DmId.From(Guid.NewGuid());

        // Act & Assert
        Assert.That(dmId1, Is.Not.EqualTo(dmId2));
    }

    [Test]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dmId = DmId.From(guid);

        // Act
        Guid result = dmId;

        // Assert
        Assert.That(result, Is.EqualTo(guid));
    }

    [Test]
    public void ExplicitConversionFromGuid_CreatesDmId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var dmId = (DmId)guid;

        // Assert
        Assert.That(dmId.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ExplicitConversionFromEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (DmId)Guid.Empty);
    }

    [Test]
    public void ImplicitConversionToCharacterId_Works()
    {
        // Arrange
        var dmId = DmId.FromCdKey("ABCD1234");

        // Act
        CharacterId characterId = dmId;

        // Assert
        Assert.That(characterId.Value, Is.EqualTo(dmId.Value));
    }

    [Test]
    public void ExplicitConversionFromCharacterId_Works()
    {
        // Arrange
        var characterId = CharacterId.New();

        // Act
        var dmId = (DmId)characterId;

        // Assert
        Assert.That(dmId.Value, Is.EqualTo(characterId.Value));
    }

    [Test]
    public void ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dmId = DmId.From(guid);

        // Act
        var result = dmId.ToString();

        // Assert
        Assert.That(result, Is.EqualTo(guid.ToString()));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var dmId1 = DmId.FromCdKey("ABCD1234");
        var dmId2 = DmId.FromCdKey("WXYZ9876");
        var dict = new Dictionary<DmId, string>
        {
            [dmId1] = "DM Alice",
            [dmId2] = "DM Bob"
        };

        // Act & Assert
        Assert.That(dict[dmId1], Is.EqualTo("DM Alice"));
        Assert.That(dict[dmId2], Is.EqualTo("DM Bob"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var dmId1 = DmId.FromCdKey("ABCD1234");
        var dmId2 = DmId.FromCdKey("ABCD1234"); // Same key
        var dmId3 = DmId.FromCdKey("WXYZ9876");
        var set = new HashSet<DmId> { dmId1, dmId2, dmId3 };

        // Act & Assert
        Assert.That(set.Count, Is.EqualTo(2)); // dmId1 and dmId2 are equal
        Assert.That(set.Contains(dmId1), Is.True);
        Assert.That(set.Contains(dmId3), Is.True);
    }

    [Test]
    public void DeterministicBehavior_AcrossSessions()
    {
        // This test verifies that the same CD key always produces
        // the same GUID, which is crucial for DM codex persistence

        // Arrange
        const string cdKey = "TESTKEY1";

        // Act - Simulate different "sessions"
        var dmIdSession1 = DmId.FromCdKey(cdKey);
        var dmIdSession2 = DmId.FromCdKey(cdKey);
        var dmIdSession3 = DmId.FromCdKey(cdKey);

        // Assert
        Assert.That(dmIdSession1, Is.EqualTo(dmIdSession2));
        Assert.That(dmIdSession2, Is.EqualTo(dmIdSession3));
        Assert.That(dmIdSession1.Value, Is.EqualTo(dmIdSession3.Value));
    }

    [Test]
    public void DmIdCanBeUsedWhereCharacterIdIsExpected()
    {
        // This test verifies that DM codices can be used polymorphically
        // with character codices in the system

        // Arrange
        var dmId = DmId.FromCdKey("DMKEY001");

        // Act - Implicit conversion to CharacterId
        CharacterId characterId = dmId;

        // Assert
        Assert.That(characterId.Value, Is.EqualTo(dmId.Value));

        // Can be used in methods expecting CharacterId
        var dict = new Dictionary<CharacterId, string>
        {
            [characterId] = "DM Codex"
        };

        Assert.That(dict[characterId], Is.EqualTo("DM Codex"));
    }
}
