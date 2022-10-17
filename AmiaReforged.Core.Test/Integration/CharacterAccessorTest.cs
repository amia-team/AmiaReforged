using AmiaReforged.Core.Accessors;
using AmiaReforged.Core.Models;
using Xunit;

namespace AmiaReforged.Core.Test.Integration;

/// <summary>
/// Integration test for the CharacterAccessor
/// </summary>
public class CharacterAccessorTest 
{
    private const string TestPcKey = "123QWERTY_000000000";
    private readonly CharacterAccessor _characterAccessor;

    public CharacterAccessorTest()
    {
        _characterAccessor = new CharacterAccessor();
    }

    [Fact]
    public void ShouldGetCharacterByPcKey()
    {
        AmiaCharacter? character = _characterAccessor.Characters.Find(TestPcKey);
        
        Assert.NotNull(character?.PcKey);
        Assert.Equal(TestPcKey, character?.PcKey);
    }
}

