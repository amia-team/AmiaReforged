using System;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.System.Services;
using Xunit;

namespace AmiaReforged.Core.Test.Integration;

public class FactionCharacterRelationServiceTest
{
    private readonly FactionCharacterRelationService _characterRelationService;
    private readonly FactionService _factionService;
    private readonly CharacterService _characterService;

    public FactionCharacterRelationServiceTest()
    {
        _characterRelationService = new FactionCharacterRelationService();
        _characterService = new CharacterService();
        _factionService = new FactionService(_characterService);
    }


    [Fact]
    public async void ShouldAddFactionCharacterRelation()
    {
        Faction testFaction = new()
        {
            Name = "TestFaction",
            Description = "TestFactionDescription",
        };

        Character testCharacter = new()
        {
            FirstName = "TestCharacterRelation",
            LastName = "TestCharacterRelation",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        
        FactionCharacterRelation factionCharacterRelation = new()
        {
            FactionName = "TestFaction",
            CharacterId = testCharacter.Id,
            Relation = 0
        };
        
        await _factionService.AddFaction(testFaction);
        await _characterService.AddCharacter(testCharacter);
        await _characterRelationService.AddFactionCharacterRelation(factionCharacterRelation);
        
        FactionCharacterRelation actual = _characterRelationService.GetFactionCharacterRelation(factionCharacterRelation.FactionName, factionCharacterRelation.CharacterId);
    }
}