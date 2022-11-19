using System;
using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.System.Helpers;
using BoDi;
using TechTalk.SpecFlow;

namespace SpecFlowProject1.Hooks
{
    [Binding]
    public class Hooks
    {
        private readonly IObjectContainer _objectContainer;

        public Hooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            Character character = new()
            {
                Id = Guid.NewGuid()
            };

            _objectContainer.RegisterInstanceAs(character, "testCharacter");
            _objectContainer.RegisterTypeAs<NwTaskHelper, NwTaskHelper>("nwTaskHelper");
            _objectContainer.RegisterTypeAs<AmiaContext, AmiaContext>("amiaContext");
            _objectContainer.RegisterTypeAs<CharacterService, CharacterService>("testCharacterService");
            _objectContainer.RegisterTypeAs<FactionService, FactionService>();
        }

        [AfterScenario]
        public async Task AfterScenario()
        {
            await DoDatabaseCleanup();

            _objectContainer.Dispose();
        }

        private async Task DoDatabaseCleanup()
        {
            CharacterService characterService = _objectContainer.Resolve<CharacterService>("testCharacterService");
            FactionService factionService = _objectContainer.Resolve<FactionService>();
            
            List<Character> character = _objectContainer.ResolveAll<Character>().ToList();
            List<Faction> faction = _objectContainer.ResolveAll<Faction>().ToList();
            await characterService.DeleteCharacters(character);
            await factionService.DeleteFactions(faction);
        }
    }
}