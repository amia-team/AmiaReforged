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
            // Register DbContextOptions if needed for factory pattern
            container.Register(factory =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<SimulationDbContext>();

                // Use dedicated WorldSimulator database connection
                string? connectionString = configuration.GetConnectionString("WorldSimulator");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException(
                        "WorldSimulator database connection string is not configured. " +
                        "Please set ConnectionStrings:WorldSimulator in appsettings.json or environment variables.");
                }

                optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "simulation");
                });

                return optionsBuilder.Options;
            }, new PerContainerLifetime());

            // Register DbContext with scoped lifetime
            container.Register<SimulationDbContext>(factory =>
            {
                var options = factory.GetInstance<DbContextOptions<SimulationDbContext>>();
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
