using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using BoDi;
using Microsoft.EntityFrameworkCore;
using TechTalk.SpecFlow.Infrastructure;
using Testcontainers.PostgreSql;

namespace AmiaReforged.Core.Specs.Hooks
{
    [Binding]
    public class Hooks
    {
        private readonly SpecFlowOutputHelper _outputHelper;
        private readonly IObjectContainer _objectContainer;
        private PostgreSqlContainer _postgresContainer;

        public Hooks(IObjectContainer objectContainer, SpecFlowOutputHelper outputHelper)
        {
            
            _objectContainer = objectContainer;
            _outputHelper = outputHelper;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            Character character = new()
            {
                Id = Guid.NewGuid()
            };
            
            _postgresContainer = new PostgreSqlBuilder()
                .WithPassword(PostgresConfig.Password)
                .WithUsername(PostgresConfig.Username)
                .WithDatabase(PostgresConfig.Database)
                .Build();
            
            await _postgresContainer.StartAsync();

            PostgresConfig.Host = _postgresContainer.Hostname;
            PostgresConfig.Port = _postgresContainer.GetMappedPublicPort(5432);
            

            _objectContainer.RegisterInstanceAs(character, ObjectContainerKeys.Character);
            _objectContainer.RegisterTypeAs<NwTaskHelper, NwTaskHelper>(ObjectContainerKeys.NwTaskHelper);
            _objectContainer.RegisterTypeAs<AmiaContext, AmiaContext>(ObjectContainerKeys.AmiaContext);
            _objectContainer.RegisterTypeAs<CharacterService, CharacterService>(ObjectContainerKeys.CharacterService);
            _objectContainer.RegisterTypeAs<FactionService, FactionService>();

            await DoDatabaseSetup();
        }

        private async Task DoDatabaseSetup()
        {
            _outputHelper.WriteLine("Doing database setup");
            AmiaContext amiaContext = _objectContainer.Resolve<AmiaContext>("amiaContext");

            bool canConnectAsync = await amiaContext.Database.CanConnectAsync();
            if (!canConnectAsync)
            {
                _outputHelper.WriteLine("Can't connect to database");
            }

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
            await _postgresContainer.StopAsync();
        }
    }
}