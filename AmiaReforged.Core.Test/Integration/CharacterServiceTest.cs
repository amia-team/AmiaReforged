using System;
using AmiaReforged.Core.Entities;
using AmiaReforged.System.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AmiaReforged.Core.Test.Integration;

public class CharacterServiceTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly CharacterService _characterService;

    private readonly AmiaCharacter _amiaCharacter = new()
    {
        Id = Guid.NewGuid(),
        FirstName = "test",
        LastName = "test",
        CdKey = "abcdefg",
        IsPlayerCharacter = false
    };


    public CharacterServiceTest(ITestOutputHelper output)
    {
        _output = output;
        _characterService = new CharacterService();
    
    }

    [Fact]
    public async void ShouldAddCharacter()
    {
        await _characterService.AddCharacter(_amiaCharacter);
        
        AmiaCharacter? character = await _characterService.GetCharacterByGuid(_amiaCharacter.Id);

        character.Should().NotBeNull();
        character?.CdKey.Should().Be(_amiaCharacter.CdKey);
    }

    [Fact]
    public async void ShouldDeleteCharacter()
    {
        // change guid of character to a new guid.
        AmiaCharacter character = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "test",
            LastName = "test",
            IsPlayerCharacter = false
        };
        
        await _characterService.AddCharacter(character);
        AmiaCharacter? addedPc = await _characterService.GetCharacterByGuid(character.Id);
        
        addedPc.Should().NotBeNull("character should be added for later deletion");
        
        await _characterService.DeleteCharacter(character);
        AmiaCharacter? actual = await _characterService.GetCharacterByGuid(character.Id);

        actual.Should().BeNull("character should be deleted");
    }
    
    [Fact]
    public async void ShouldUpdateCharacter()
    {
        await _characterService.AddCharacter(_amiaCharacter);
        AmiaCharacter? addedPc = await _characterService.GetCharacterByGuid(_amiaCharacter.Id);
        
        addedPc.Should().NotBeNull("character should be added for later update");
        
        addedPc!.FirstName = "updated";
        await _characterService.UpdateCharacter(addedPc);
        
        AmiaCharacter? actual = await _characterService.GetCharacterByGuid(_amiaCharacter.Id);
        actual.Should().NotBeNull("character should be updated");
        actual!.FirstName.Should().Be("updated");
    }
    
    [Fact]
    public async void ShouldGetCharacterByGuid()
    {
        await _characterService.AddCharacter(_amiaCharacter);
        AmiaCharacter? actual = await _characterService.GetCharacterByGuid(_amiaCharacter.Id);
        
        actual.Should().NotBeNull("character should be found");
        actual!.FirstName.Should().Be(_amiaCharacter.FirstName);
    }
    
    
    public async void Dispose()
    {
        await _characterService.DeleteCharacter(_amiaCharacter);
        GC.SuppressFinalize(this);
    }
}