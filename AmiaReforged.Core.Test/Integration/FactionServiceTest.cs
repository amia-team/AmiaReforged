using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.System.Services;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

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
        _factionService = new FactionService();
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

        await _factionService.AddFaction(_faction);
        await _factionService.AddToRoster(_faction, one.Id);
        await _factionService.AddToRoster(_faction, new List<Guid> { two.Id, three.Id });

        Faction? actual = await _factionService.GetFactionByName(_faction.Name);

        actual.Should().NotBeNull("Faction should be added");
        actual!.Members.Should().HaveCount(3, "Faction should have 3 characters in roster");
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

        actual.ToList().Count.Should().Be(2, "There should be 2 factions");
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