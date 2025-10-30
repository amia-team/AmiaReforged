using LightInject;
using WorldSimulator.Application;
using WorldSimulator.Application.Processors;
using WorldSimulator.Infrastructure.PwEngineClient;

namespace WorldSimulator.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Configures dependency injection container for WorldSimulator.
    /// Uses LightInject for lightweight, performant DI without over-engineering.
    /// </summary>
    public static class ContainerConfiguration
    {
        /// <summary>
        /// Register all services in the DI container
        /// </summary>
        public static IServiceContainer ConfigureServices(this IServiceContainer container,
            IConfiguration configuration)
        {
            // Database
            RegisterDatabase(container, configuration);

            // Infrastructure Services
            RegisterInfrastructureServices(container, configuration);

            // Application Services
            RegisterApplicationServices(container);

            // Domain Services
            RegisterDomainServices(container);

            return container;
        }

        private static void RegisterDatabase(IServiceContainer container, IConfiguration configuration)
        {
            container.Register(factory =>
            {
                DbContextOptionsBuilder<SimulationDbContext> optionsBuilder = new DbContextOptionsBuilder<SimulationDbContext>();

                // Resolve connection string with precedence:
                // 1) ConnectionStrings:DefaultConnection (from .env, overrides appsettings)
                // 2) POSTGRES_CONNECTION_STRING
                // 3) Compose from POSTGRES_* parts
                // 4) ConnectionStrings:WorldSimulator (appsettings default)
                string? connectionString = configuration.GetConnectionString("DefaultConnection")
                                           ?? configuration["ConnectionStrings:DefaultConnection"]
                                           ?? configuration["POSTGRES_CONNECTION_STRING"]
                                           ?? ComposeFromParts(configuration)
                                           ?? configuration.GetConnectionString("WorldSimulator");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "WorldSimulator database connection string is not configured. " +
                        "Set ConnectionStrings:WorldSimulator or ConnectionStrings:DefaultConnection, " +
                        "or provide POSTGRES_* variables.");
                }

                optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "simulation");
                });

                return optionsBuilder.Options;

                static string? ComposeFromParts(IConfiguration cfg)
                {
                    string host = cfg["POSTGRES_HOST"] ?? "postgres";
                    string port = cfg["POSTGRES_PORT"] ?? "5432";
                    string? db = cfg["POSTGRES_DB"];
                    string? user = cfg["POSTGRES_USER"];
                    string? pwd = cfg["POSTGRES_PASSWORD"];
                    if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pwd))
                        return null;
                    return $"Host={host};Port={port};Database={db};Username={user};Password={pwd}";
                }
            }, new PerContainerLifetime());

            container.Register<SimulationDbContext>(factory =>
            {
                DbContextOptions<SimulationDbContext>? options = factory.GetInstance<DbContextOptions<SimulationDbContext>>();
                return new SimulationDbContext(options);
            }, new PerScopeLifetime());
        }

        private static void RegisterInfrastructureServices(IServiceContainer container, IConfiguration configuration)
        {
            // Circuit Breaker
            container.Register<CircuitBreakerService>(new PerContainerLifetime());

            // Event Log Publisher with runtime toggle
            container.Register<IEventLogPublisher, DiscordEventLogService>(new PerContainerLifetime());

            // PwEngine API Client
            container.Register<IPwEngineClient, PwEngineHttpClient>(new PerContainerLifetime());

            // PwEngine Test Service (for Hello/Ping verification)
            container.Register<PwEngineTestService>(new PerContainerLifetime());
        }

        private static void RegisterApplicationServices(IServiceContainer container)
        {
            // Background Worker
            container.Register<SimulationWorker>(new PerContainerLifetime());

            // Processors
            container.Register<IDominionTurnProcessor, DominionTurnProcessor>(new PerScopeLifetime());

            // Factories (singleton lifetime - they're stateless)
            container.Register<IDominionTurnProcessorFactory, DominionTurnProcessorFactory>(new PerContainerLifetime());
        }

        private static void RegisterDomainServices(IServiceContainer container)
        {
            // Add domain service registrations here as needed
            // Keep this minimal - domain services should be few and focused
        }
    }
}
