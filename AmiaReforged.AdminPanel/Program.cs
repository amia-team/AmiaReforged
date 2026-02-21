using AmiaReforged.AdminPanel.Components;
using AmiaReforged.AdminPanel.Configuration;
using AmiaReforged.AdminPanel.Hubs;
using AmiaReforged.AdminPanel.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

namespace AmiaReforged.AdminPanel;

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

        Log.Information("Amia Admin Panel initializing...");

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            // Bind configuration
            var adminConfig = new AdminPanelConfig();
            builder.Configuration.GetSection("AdminPanel").Bind(adminConfig);

            // Override from environment variables
            adminConfig.DefaultAdminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? adminConfig.DefaultAdminUsername;
            adminConfig.DefaultAdminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? adminConfig.DefaultAdminPassword;
            adminConfig.DiscordWebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? adminConfig.DiscordWebhookUrl;
            adminConfig.DockerSocketPath = Environment.GetEnvironmentVariable("DOCKER_SOCKET") ?? adminConfig.DockerSocketPath;

            builder.Services.AddSingleton(adminConfig);

            // Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();

            // Services
            builder.Services.AddSingleton<IDockerMonitorService, DockerMonitorService>();
            builder.Services.AddSingleton<IMonitoringConfigService, MonitoringConfigService>();
            builder.Services.AddHostedService<ContainerHealthMonitor>();

            // SignalR
            builder.Services.AddSignalR();

            // Blazor
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<ContainerLogHub>("/hubs/logs");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Admin Panel terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
