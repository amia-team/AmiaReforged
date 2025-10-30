using WorldSimulator.Infrastructure.DependencyInjection;
using WorldSimulator.Application;
using DotNetEnv;
using LightInject;
using LightInject.Microsoft.DependencyInjection;

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

            // Configure Serilog early for startup logging
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Build host using LightInject
            IHost host = Host.CreateDefaultBuilder(args)
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
                        var baseUrl = context.Configuration["PwEngine:BaseUrl"]
                            ?? "http://localhost:8080/api/worldengine/";
                        var apiKey = context.Configuration["PwEngine:ApiKey"]
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

            // Ensure database is created (for development)
            using (IServiceScope scope = host.Services.CreateScope())
            {
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

            Log.Information("WorldSimulator starting...");

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
                Log.CloseAndFlush();
            }
        }
    }
}

