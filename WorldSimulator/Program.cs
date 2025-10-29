using WorldSimulator.Application;
using WorldSimulator.Domain.Services;
using WorldSimulator.Infrastructure.Persistence;
using WorldSimulator.Infrastructure.Services;
using DotNetEnv;

namespace WorldSimulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // Load environment variables from .env file if it exists
        if (File.Exists(".env"))
        {
            Env.Load(".env");
        }

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Services.AddSerilog();

        // Database
        builder.Services.AddDbContext<SimulationDbContext>(options =>
        {
            string connectionString = builder.Configuration.GetConnectionString("PwEngine")
                                      ?? throw new InvalidOperationException("ConnectionString 'PwEngine' not found");
            options.UseNpgsql(connectionString);
        });

        // HTTP Clients
        builder.Services.AddHttpClient("WorldEngine", client =>
        {
            int timeout = builder.Configuration.GetValue<int>("WorldEngine:TimeoutSeconds", 30);
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });

        // Core Services
        builder.Services.AddSingleton<IEventLogPublisher, DiscordEventLogService>();
        builder.Services.AddSingleton<CircuitBreakerService>();

        // Background Services (order matters for startup)
        builder.Services.AddHostedService(sp =>
            (DiscordEventLogService)sp.GetRequiredService<IEventLogPublisher>());
        builder.Services.AddHostedService(sp =>
            sp.GetRequiredService<CircuitBreakerService>());
        builder.Services.AddHostedService<SimulationWorker>();

        IHost host = builder.Build();

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

