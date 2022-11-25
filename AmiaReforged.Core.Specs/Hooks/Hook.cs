using System;
using AmiaReforged.Core;
using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Specs.Steps;
using AmiaReforged.System.Helpers;
using BoDi;
using Microsoft.EntityFrameworkCore;
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
        public async Task BeforeScenario()
        {
            Character character = new()
            {
                Id = Guid.NewGuid()
            };

            _objectContainer.RegisterInstanceAs(character, ObjectContainerKeys.Character);
            _objectContainer.RegisterTypeAs<NwTaskHelper, NwTaskHelper>(ObjectContainerKeys.NwTaskHelper);
            _objectContainer.RegisterTypeAs<AmiaContext, AmiaContext>(ObjectContainerKeys.AmiaContext);
            _objectContainer.RegisterTypeAs<CharacterService, CharacterService>(ObjectContainerKeys.CharacterService);
            _objectContainer.RegisterTypeAs<FactionService, FactionService>();
            
            await DoDatabaseSetup();
        }

        private async Task DoDatabaseSetup()
        {
            AmiaContext amiaContext = _objectContainer.Resolve<AmiaContext>("amiaContext");
            
            await amiaContext.Database.MigrateAsync();
        }

        [AfterScenario]
        public async Task AfterScenario()
        {
            await DoDatabaseCleanup();
            _objectContainer.Dispose();
        }

        private async Task DoDatabaseCleanup()
        {
            AmiaContext amiaContext = _objectContainer.Resolve<AmiaContext>("amiaContext");
            await amiaContext.Database.EnsureDeletedAsync();
        }
    }
}