using AmiaReforged.BackupService.Application;
using AmiaReforged.BackupService.Configuration;
using AmiaReforged.BackupService.Services;
using DotNetEnv;
using LightInject.Microsoft.DependencyInjection;
using Serilog;

namespace AmiaReforged.BackupService;

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
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Amia Backup Service initializing...");

        try
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new LightInjectServiceProviderFactory())
                .ConfigureServices((context, services) =>
                {
                    services.AddSerilog();

                    // Bind configuration
                    BackupConfig backupConfig = new();
                    context.Configuration.GetSection("Backup").Bind(backupConfig);
                    services.AddSingleton(backupConfig);

                    // Register HTTP clients
                    services.AddHttpClient("ServerHealth");
                    services.AddHttpClient("Discord");

                    // Register services
                    services.AddSingleton<IPostgresBackupService, PostgresBackupService>();
                    services.AddSingleton<IGitBackupService, GitBackupService>();
                    services.AddSingleton<ICharacterVaultBackupService, CharacterVaultBackupService>();
                    services.AddSingleton<IServerHealthService, ServerHealthService>();
                    services.AddSingleton<IDiscordNotificationService, DiscordNotificationService>();

                    // Register the health monitor as both singleton (for DI) and hosted service (to start it)
                    services.AddSingleton<ServerHealthMonitor>();
                    services.AddSingleton<IServerHealthMonitor>(sp => sp.GetRequiredService<ServerHealthMonitor>());
                    services.AddHostedService(sp => sp.GetRequiredService<ServerHealthMonitor>());

                    // Register the background worker
                    services.AddHostedService<BackupWorker>();
                })
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Backup Service terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
