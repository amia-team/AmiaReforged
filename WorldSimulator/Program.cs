using WorldSimulator.Infrastructure.DependencyInjection;
using WorldSimulator.Application;
using WorldSimulator.Infrastructure.PwEngineClient;
using DotNetEnv;
using LightInject;
using LightInject.Microsoft.DependencyInjection;
using Npgsql;

namespace WorldSimulator
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load environment variables from .env file if it exists
            if (File.Exists(".env"))
            {
                Env.Load(".env");
            }

            // Configure Serilog early for startup logging (with reasonable defaults)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("WorldSimulator initializing...");

            // Build host using LightInject
            IHost host = Host.CreateDefaultBuilder(args)
                // NEW: ensure ConnectionStrings:DefaultConnection is populated from environment
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Build current configuration snapshot to read env vars
                    IConfigurationRoot built = config.Build();
                    string? composed = ComposeConnectionStringFromEnvironment(built);
                    string? existing = built.GetConnectionString("DefaultConnection")
                                       ?? built["ConnectionStrings:DefaultConnection"];

                    if (!string.IsNullOrWhiteSpace(composed) && string.IsNullOrWhiteSpace(existing))
                    {
                        // Inject an in-memory connection string so downstream DI picks it up
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = composed
                        });
                    }
                })
                .UseServiceProviderFactory(new LightInjectServiceProviderFactory())
                .ConfigureServices((context, services) =>
                {
                    // Reconfigure Serilog with full config now that we have IConfiguration
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    services.AddSerilog();

                    // HTTP Clients
                    services.AddHttpClient("WorldEngine", client =>
                    {
                        int timeout = context.Configuration.GetValue("WorldEngine:TimeoutSeconds", 30);
                        client.Timeout = TimeSpan.FromSeconds(timeout);
                    });

                    // PwEngine API Client
                    services.AddHttpClient("PwEngine", client =>
                    {
                        string baseUrl = context.Configuration["PwEngine:BaseUrl"]
                                         ?? "http://localhost:8080/api/worldengine/";
                        string apiKey = context.Configuration["PwEngine:ApiKey"]
                                        ?? "dev-api-key-change-in-production";

                        client.BaseAddress = new Uri(baseUrl);
                        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                        client.Timeout = TimeSpan.FromSeconds(
                            context.Configuration.GetValue("PwEngine:Timeout", 30));
                    });

                    // Background Services
                    services.AddHostedService(sp =>
                        (DiscordEventLogService)sp.GetRequiredService<IEventLogPublisher>());
                    services.AddHostedService(sp =>
                        sp.GetRequiredService<CircuitBreakerService>());
                    services.AddHostedService<SimulationWorker>();
                })
                .ConfigureContainer<IServiceContainer>((context, container) =>
                {
                    // Configure services using LightInject
                    container.ConfigureServices(context.Configuration);
                })
                .Build();

            // Ensure database is created (for development) - optionally skippable
            using (IServiceScope scope = host.Services.CreateScope())
            {
                IConfiguration configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                bool skipDbInit = string.Equals(
                        Environment.GetEnvironmentVariable("WORLD_SIMULATOR_SKIP_DB_INIT"),
                        "true",
                        StringComparison.OrdinalIgnoreCase)
                    || configuration.GetValue("WorldSimulator:SkipDbInit", false);

                // Prefer the configured ConnectionStrings:DefaultConnection; otherwise compose
                string? connectionString = configuration.GetConnectionString("DefaultConnection")
                                            ?? configuration["ConnectionStrings:DefaultConnection"]
                                            ?? ComposeConnectionStringFromEnvironment(configuration);

                if (skipDbInit)
                {
                    Log.Information("Skipping database initialization due to configuration (WORLD_SIMULATOR_SKIP_DB_INIT).");
                }
                else if (string.IsNullOrWhiteSpace(connectionString))
                {
                    Log.Warning("No database connection string configured. Set ConnectionStrings__DefaultConnection or POSTGRES_* variables. Skipping database initialization.");
                }
                else
                {
                    // Diagnostic: log resolved connection (without password)
                    try
                    {
                        NpgsqlConnectionStringBuilder builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                        Log.Information("Using DB connection Host={Host}, Port={Port}, Database={Database}, Username={Username}",
                            builder.Host, builder.Port, builder.Database, builder.Username);
                    }
                    catch
                    {
                        Log.Information("Using DB connection string (redacted): {Conn}", connectionString.Replace("Password=", "Pwd=***;"));
                    }

                    SimulationDbContext db = scope.ServiceProvider.GetRequiredService<SimulationDbContext>();
                    try
                    {
                        await db.Database.EnsureCreatedAsync();
                        Log.Information("Database connection verified");
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Failed to connect to database");
                        throw;
                    }
                }
            }

            Log.Information("WorldSimulator starting...");

            // Test communication with PwEngine (fire and forget - don't block startup)
            using (IServiceScope testScope = host.Services.CreateScope())
            {
                PwEngineTestService? testService = testScope.ServiceProvider.GetService<PwEngineTestService>();
                if (testService != null)
                {
                    Log.Information("Testing connectivity to PwEngine (non-blocking)...");
                    // Fire and forget - don't wait for result, don't block startup
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues
                    _ = Task.Run(async () =>
                    {
                        bool pinged = await testService.PingPwEngineAsync();
                        if (pinged)
                        {
                            await testService.SendHelloToPwEngineAsync();
                        }
                    });
#pragma warning restore CS4014
                }
            }

            try
            {
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        // Helper to compose a connection string from common POSTGRES_* env/config values
        private static string? ComposeConnectionStringFromEnvironment(IConfiguration config)
        {
            // 1) If explicit fallback variable is set, honor it
            string? cs = config["POSTGRES_CONNECTION_STRING"];
            if (!string.IsNullOrWhiteSpace(cs)) return cs;

            // 2) Assemble from discrete parts
            string host = config["POSTGRES_HOST"] ?? "postgres";
            string port = config["POSTGRES_PORT"] ?? "5432";
            string? db = config["POSTGRES_DB"];
            string? user = config["POSTGRES_USER"];
            string? pwd = config["POSTGRES_PASSWORD"];

            if (string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pwd))
                return null;

            return $"Host={host};Port={port};Database={db};Username={user};Password={pwd}";
        }
    }
}
