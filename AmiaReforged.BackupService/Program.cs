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
                    // Reconfigure Serilog with full config
                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .CreateLogger();

                    services.AddSerilog();

                    // Bind configuration
                    BackupConfig backupConfig = new();
                    context.Configuration.GetSection("Backup").Bind(backupConfig);
                    services.AddSingleton(backupConfig);

                    // Register services
                    services.AddSingleton<IPostgresBackupService, PostgresBackupService>();
                    services.AddSingleton<IGitBackupService, GitBackupService>();

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
