using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.System.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using CharacterService = AmiaReforged.System.Services.CharacterService;

namespace AmiaReforged.Core.Test.Integration;

public class FactionServiceTest : IDisposable
{
    private readonly FactionService _factionService;
    private readonly ITestOutputHelper _output;

    private readonly Faction _faction = new()
    {
        Name = "Test Faction",
        Description = "Test Description",
        Members = new List<Guid>()
    };

    public FactionServiceTest(ITestOutputHelper output)
    {
        _output = output;
        _factionService = new FactionService(new CharacterService());
    }

    [Fact]
    public async void ShouldAddFaction()
    {
        await _factionService.AddFaction(_faction);

        Faction? actual = await _factionService.GetFactionByName(_faction.Name);

        actual.Should().NotBeNull("Faction should be added");
        actual!.Description.Should().Be(_faction.Description, "Faction description should be the same");
    }

    [Fact]
    public async void ShouldGetFactionByName()
    {
        await _factionService.AddFaction(_faction);

        Faction? actual = await _factionService.GetFactionByName(_faction.Name);

        actual.Should().NotBeNull("Faction should be added");
        actual!.Description.Should().Be(_faction.Description, "Faction description should be the same");
    }

    [Fact]
    public async void ShouldDeleteFaction()
    {
        Faction f = new()
        {
            Name = "Delete me",
            Description = "Delete me"
        };

        await _factionService.AddFaction(f);

        Faction? added = await _factionService.GetFactionByName(f.Name);
        added.Should().NotBeNull("Faction should be added");

        await _factionService.DeleteFaction(f);
        Faction? actual = await _factionService.GetFactionByName(f.Name);

        actual.Should().BeNull("Faction should be deleted");
    }

    [Fact]
    public async void ShouldUpdateFaction()
    {
        Faction f = new()
        {
            Name = "Update me",
            Description = "Update me"
        };

        await _factionService.AddFaction(f);

        Faction? added = await _factionService.GetFactionByName(f.Name);
        added.Should().NotBeNull("Faction should be added");

        added!.Description = "Updated";
        await _factionService.UpdateFaction(added);

        Faction? actual = await _factionService.GetFactionByName(f.Name);

        actual.Should().NotBeNull("Faction should be updated");
        actual!.Description.Should().Be("Updated", "Faction description should be updated");
    }

    [Fact]
    public async void ShouldAddCharactersToRoster()
    {
        AmiaCharacter one = new()
        {
            FirstName = "One",
            LastName = "One",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        AmiaCharacter two = new()
        {
            FirstName = "Two",
            LastName = "Two",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        AmiaCharacter three = new()
        {
            FirstName = "Three",
            LastName = "Three",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };

        CharacterService characterService = new();

        await characterService.AddCharacter(one);
        await characterService.AddCharacter(two);
        await characterService.AddCharacter(three);

        await _factionService.AddFaction(_faction);
        await _factionService.AddToRoster(_faction, one.Id);
        await _factionService.AddToRoster(_faction, new List<Guid> { two.Id, three.Id });

        Faction? actual = await _factionService.GetFactionByName(_faction.Name);

        actual.Should().NotBeNull("Faction should be added");
        actual!.Members.Should().HaveCount(3, "Faction should have 3 characters in roster");
    }

    [Fact]
    public async void ShouldAddFactionWithPrepopulatedRoster()
    {
        AmiaCharacter one = new()
        {
            FirstName = "PrepopulatedOne",
            LastName = "PrepopulatedOne",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        AmiaCharacter two = new()
        {
            FirstName = "PrepopulatedTwo",
            LastName = "PrepopulatedTwo",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        AmiaCharacter three = new()
        {
            FirstName = "PrepopulatedThree",
            LastName = "PrepopulatedThree",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        
        CharacterService characterService = new();
        
        await characterService.AddCharacter(one);
        await characterService.AddCharacter(two);
        await characterService.AddCharacter(three);
        
        Faction f = new()
        {
            Name = "Prepopulated Faction",
            Description = "Prepopulated Description",
            Members = new List<Guid> { one.Id, two.Id, three.Id }
        };
        
        await _factionService.AddFaction(f);
        
        Faction? actual = await _factionService.GetFactionByName(f.Name);
        
        actual.Should().NotBeNull("Faction should be added");
        actual!.Members.Should().HaveCount(3, "Faction should have 3 characters in roster");
    }
    [Fact]
    public async void ShouldNotAcceptNonExistentCharacters()
    {
        AmiaCharacter fakeCharacter = new()
        {
            FirstName = "Fake",
            LastName = "Fake",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        
        Faction factionWithFake = new Faction
        {
            Name = "Faction with fake",
            Description = "Faction with fake",
            Members = new List<Guid> { fakeCharacter.Id }
        };
        
        await _factionService.AddFaction(factionWithFake);
        
        Faction? actual = await _factionService.GetFactionByName(factionWithFake.Name);
        
        actual.Should().NotBeNull("Faction should be added");
        actual!.Members.Should().HaveCount(0, "Faction should not be created with nonexistent characters");
    }

    [Fact]
    public async void ShouldGetAllFactions()
    {
        Faction factionOne = new()
        {
            Name = "Get all",
            Description = "Get all",
            Members = new List<Guid>()
        };
        Faction factionTwo = new()
        {
            Name = "Get all 2",
            Description = "Get all 2",
            Members = new List<Guid>()
        };

        await _factionService.AddFaction(factionOne);
        await _factionService.AddFaction(factionTwo);

        IEnumerable<Faction> actual = await _factionService.GetAllFactions();

        actual.ToList().ToList().Should().Contain(factionOne, "Faction one should be in list").And
            .Contain(factionTwo, "Faction two should be in list");
    }
    
    
    [Fact]
    public async void ShouldRemoveCharacterFromRoster()
    {
        AmiaCharacter one = new()
        {
            FirstName = "RemoveOne",
            LastName = "RemoveOne",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        AmiaCharacter two = new()
        {
            FirstName = "RemoveTwo",
            LastName = "RemoveTwo",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        AmiaCharacter three = new()
        {
            FirstName = "RemoveThree",
            LastName = "RemoveThree",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true
        };
        
        CharacterService characterService = new();
        
        await characterService.AddCharacter(one);
        await characterService.AddCharacter(two);
        await characterService.AddCharacter(three);
        
        Faction f = new()
        {
            Name = "Remove from roster",
            Description = "Remove from roster",
            Members = new List<Guid> { one.Id, two.Id, three.Id }
        };
        
        await _factionService.AddFaction(f);
        
        Faction? actual = await _factionService.GetFactionByName(f.Name);
        
        actual.Should().NotBeNull("Faction should be added");
        actual!.Members.Should().HaveCount(3, "Faction should have 3 characters in roster");
        
        
        await _factionService.RemoveFromRoster(f, two.Id);
        
        actual = await _factionService.GetFactionByName(f.Name);
        
        actual.Should().NotBeNull("Faction should be added");
        actual!.Members.Should().HaveCount(2, "Faction should have 2 characters in roster");
    }


    [Fact]
    public async void ShouldGetAllPlayerCharactersInRoster()
    {
        AmiaCharacter pc = new()
        {
            CdKey = "asdfkjnesruq",
            FirstName = "PC",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true,
            LastName = "PC"
        };
        AmiaCharacter npc = new()
        {
            FirstName = "NPC",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = false,
            LastName = "NPC"
        };
        
        CharacterService characterService = new();
        
        await characterService.AddCharacter(pc);
        await characterService.AddCharacter(npc);
        
        Faction f = new()
        {
            Name = "Get all player characters",
            Description = "Get all player characters",
            Members = new List<Guid> { pc.Id, npc.Id }
        };
        
        await _factionService.AddFaction(f);
        
        List<AmiaCharacter> actual = await _factionService.GetAllPlayerCharactersFrom(f);
        
        actual.Should().HaveCount(1, "Faction should have 1 player character in roster");
        actual.Should().OnlyContain(c => c.FirstName == pc.FirstName, "Faction should have only PC in roster");
    }

    [Fact]
    public async void ShouldGetAllNonPlayerCharactersFromFaction()
    {
        AmiaCharacter pc = new()
        {
            CdKey = "asdfkjnesruq",
            FirstName = "PC",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = true,
            LastName = "PC"
        };
        AmiaCharacter npc = new()
        {
            FirstName = "NPC",
            Id = Guid.NewGuid(),
            IsPlayerCharacter = false,
            LastName = "NPC"
        };
        
        CharacterService characterService = new();
        
        await characterService.AddCharacter(pc);
        await characterService.AddCharacter(npc);
        
        Faction f = new()
        {
            Name = "Get all player characters",
            Description = "Get all player characters",
            Members = new List<Guid> { pc.Id, npc.Id }
        };
        
        await _factionService.AddFaction(f);
        
        List<AmiaCharacter> actual = await _factionService.GetAllNonPlayerCharactersFrom(f);
        
        actual.Should().HaveCount(1, "Faction should have 1 player character in roster");
        actual.Should().OnlyContain(c => c.FirstName == npc.FirstName, "Faction should have only NPC in roster");
    }


    public void Dispose()
    {
        AmiaContext ctx = new();
        try
        {
            ctx.Factions.Remove(_faction);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            _output.WriteLine("Error while disposing: " + e.Message);
        }

        GC.SuppressFinalize(this);
    }
}