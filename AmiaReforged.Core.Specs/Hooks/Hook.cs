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
        private static PostgreSqlContainer? _postgresContainer;
        private static bool _isMigrationDone = false;


        public Hooks(IObjectContainer objectContainer, SpecFlowOutputHelper outputHelper)
        {
            _objectContainer = objectContainer;
            _outputHelper = outputHelper;
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            PlayerCharacter playerCharacter = new()
            {
                Id = Guid.NewGuid()
            };
            
            if (_postgresContainer == null)
            {
                _postgresContainer = new PostgreSqlBuilder()
                    .WithPassword(PostgresConfig.Password)
                    .WithUsername(PostgresConfig.Username)
                    .WithDatabase(PostgresConfig.Database)
                    .Build();
                
                await _postgresContainer.StartAsync();

                PostgresConfig.Host = _postgresContainer.Hostname;
                PostgresConfig.Port = _postgresContainer.GetMappedPublicPort(5432);
            }

            _objectContainer.RegisterInstanceAs(playerCharacter, ObjectContainerKeys.Character);
            _objectContainer.RegisterTypeAs<NwTaskHelper, NwTaskHelper>(ObjectContainerKeys.NwTaskHelper);
            _objectContainer.RegisterTypeAs<AmiaDbContext, AmiaDbContext>(ObjectContainerKeys.AmiaContext);
            _objectContainer.RegisterTypeAs<CharacterService, CharacterService>(ObjectContainerKeys.CharacterService);
            _objectContainer.RegisterTypeAs<FactionService, FactionService>();

            await DoDatabaseSetup();
        }

        private async Task DoDatabaseSetup()
        {
            _outputHelper.WriteLine("Doing database setup");
            AmiaDbContext amiaDbContext = _objectContainer.Resolve<AmiaDbContext>("amiaContext");

            bool canConnectAsync = await amiaDbContext.Database.CanConnectAsync();

            if (!canConnectAsync)
            {
                _outputHelper.WriteLine("Can't connect to database");
            }

            if (!_isMigrationDone)
            {
                await amiaDbContext.Database.MigrateAsync();
                _isMigrationDone = true;
            }
        }

        [AfterScenario]
        public async Task AfterScenario()
        {
            await DoDatabaseCleanup();
            _objectContainer.Dispose();
        }

        private async Task DoDatabaseCleanup()
        {
            // Wipe the database without stopping the container
            AmiaDbContext amiaDbContext = _objectContainer.Resolve<AmiaDbContext>("amiaContext");
            await amiaDbContext.Database.EnsureDeletedAsync();
            await amiaDbContext.Database.EnsureCreatedAsync();
        }
        
        [AfterTestRun]
        public static async Task AfterTestRun()
        {
            if (_postgresContainer != null)
            {
                await _postgresContainer.StopAsync();
            }
        }
    }
}